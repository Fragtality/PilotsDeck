﻿using H.NotifyIcon;
using Microsoft.FlightSimulator.SimConnect;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Plugin;
using PilotsDeck.Simulator;
using PilotsDeck.Simulator.MSFS;
using PilotsDeck.StreamDeck;
using PilotsDeck.Tools;
using PilotsDeck.UI;
using PilotsDeck.UI.DeveloperUI;
using PilotsDeck.UI.ViewModels;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;


#if Debug || DEBUG
using System.Diagnostics;
#endif

namespace PilotsDeck
{
    public partial class App : Application
    {
        public static readonly string PLUGIN_PATH = $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Elgato\StreamDeck\Plugins\{AppConfiguration.PluginUUID}.sdPlugin";

        public static Dictionary<string, string> CommandLineArgs { get; private set; } = [];
        public static AppConfiguration Configuration { get; private set; } = null;
        public static DeckController DeckController { get; } = new();
        public static PluginController PluginController { get; } = new();
        public static SimController SimController { get; } = new();
        public static CancellationTokenSource CancellationTokenSource { get; private set; } = new();
        public static CancellationToken CancellationToken { get; private set; }
        public static Window HelperWindow { get; private set; }
        public static IntPtr WindowHandle { get; private set; }
        public static TaskbarIcon NotifyIcon { get; private set; }
        public static Window DeveloperView { get; private set; }
        public static int ExitCode { get; set; } = 0;
        public static ConcurrentQueue<ActionMeta> DesignerQueue { get; private set; } = [];
        public static ConcurrentDictionary<string, bool> ActiveDesigner {  get; private set; } = [];
        public static DispatcherTimer StatisticTimer { get; private set; } = null;
        public static bool UpdateDetected { get; private set; } = false;
        public static string UpdateVersion { get; private set; } = "";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);


        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                CancellationToken = CancellationTokenSource.Token;
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
#if Debug || DEBUG
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(500);
                    Debugger.Launch();
                }
#endif
                InitStartupLog();
                Logger.Information($"Plugin started. Checking Version ...");
                await CheckVersion();

                Logger.Information($"Version checked. Loading Configuration ...");
                LoadConfiguration();
                CheckFolders();
                ColorStore.Load();

                Logger.Information($"Configuration loaded. Loading Tray Icon ...");
                InitSystray();                

                Logger.Information($"Tray Icon loaded. Parsing Command Line ...");
                if (!ParseCommandLine())
                    return;

                Logger.Information($"Command Line parsed. Creating Main Window Hook ...");
                InitMainWindow();

                Logger.Information($"Main Window Hook created. Starting DeckController Thread ...");
                Task task = new (DeckController.Run, CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                task.Start();
                while (!DeckController.IsConnected)
                    await Task.Delay(250);

                Logger.Information($"StreamDeck Software connected. Starting Application Log ...");
                Logger.CreateLogger();

                Logger.Information($"---------------------------------------------------------------------------");
                Logger.Information($"Starting Plugin Controller Thread ...");
                task = new(PluginController.Run, CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                task.Start();

                Logger.Information($"Starting Sim Controller Thread ...");
                task = new(SimController.Run, CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                task.Start();

                StatisticTimer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUnusedRessources)
                };
                StatisticTimer.Tick += StatisticManager.RefreshTimer_Tick;
                StatisticTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                ExitCode = -1;
                DoShutdown();
            }
        }

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var result = DefWindowProc(hwnd, msg, wParam, lParam).ToInt32();

            if (msg == (int)AppConfiguration.WM_PILOTSDECK_SIMCONNECT)
            {
                try
                {
                    Logger.Verbose($"Received WM_PILOTSDECK_SIMCONNECT");
                    ConnectorMSFS.SimConnectManager.SimConnect_ReceiveMessage();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                handled = true;
            }
            else if (msg == (int)AppConfiguration.WM_PILOTSDECK_REQ_SIMCONNECT)
            {
                try
                {
                    Logger.Information($"Open SimConnect ...");
                    SimConnectManager.SimConnect = new SimConnect(AppConfiguration.SC_CLIENT_NAME, WindowHandle, AppConfiguration.WM_PILOTSDECK_SIMCONNECT, null, 0);
                    Logger.Verbose($"SimConnect Object created");
                }
                catch (Exception ex)
                {
                    if (ex is not COMException)
                        Logger.LogException(ex);
                    else
                        Logger.Information($"Error while opening SimConnect - Retry in {App.Configuration.MsfsRetryDelay / 1000}s");
                }
                handled = true;
            }
            else if (msg == (int)AppConfiguration.WM_PILOTSDECK_REQ_DESIGNER && !DesignerQueue.IsEmpty)
            {
                try
                {
                    DesignerQueue.TryDequeue(out ActionMeta action);
                    if (action != null && !ActiveDesigner.ContainsKey(action.Context))
                    {
                        var window = new ActionDesigner(action)
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth,
                            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight
                        };
                        window.Show(disableEfficiencyMode: true);
                        ActiveDesigner.Add(action.Context);
                    }
                    else if (action != null)
                        Logger.Warning($"Designer already active for Context '{action.Context}'!");
                    else
                        Logger.Warning($"Queued Action was NULL!");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                handled = true;
            }

            return new IntPtr(result);
        }

        public void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.Error("---- PLUGIN CRASH ----");
            Logger.LogException(args.ExceptionObject as Exception);
            ExitCode = -1;
            DoShutdown();
        }

        private static bool _isShutDown = false;
        public static void DoShutdown(bool shutdownApp = true)
        {
            if (_isShutDown)
                return;
            _isShutDown = true;

            Logger.Information("Signal Shutdown ...");
            ColorStore.Save();
            StatisticTimer?.Stop();
            CancellationTokenSource.Cancel();
            if (shutdownApp)
            {
                Task.Run(async () => {
                    await Task.Delay(App.Configuration.DelayExit);
                    Environment.Exit(ExitCode); 
                });
                try { NotifyIcon?.Dispose(); } catch { }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Information("Exit Signal received");
            DoShutdown(false);
            base.OnExit(e);
        }

        private static void InitStartupLog()
        {
            if (File.Exists("log/startup.log"))
                File.Delete("log/startup.log");

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                        .WriteTo.File("log/startup.log", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message} {NewLine}")
                        .MinimumLevel.Verbose();
            Log.Logger = loggerConfiguration.CreateLogger();
        }

        private void InitMainWindow()
        {
            HelperWindow = new Window()
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Visibility = Visibility.Collapsed,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Top = -10000,
                Left = -10000,

            };
            App.Current.MainWindow = HelperWindow;
            HelperWindow.Show(disableEfficiencyMode: true);
            HelperWindow.Hide(enableEfficiencyMode: false);
            WindowHandle = new WindowInteropHelper(HelperWindow).Handle;
            Logger.Debug($"Window Handle is: {WindowHandle}");
            HwndSource mainWindowSrc = HwndSource.FromHwnd(WindowHandle);
            mainWindowSrc.AddHook(WndProcHook);
        }

        protected void InitSystray()
        {
            NotifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            NotifyIcon.Icon = UpdateDetected ? Sys.GetIcon("PluginIconUpdate.ico") : Sys.GetIcon("PluginIcon.ico");
            NotifyIcon.ForceCreate(false);
            DeveloperView = new DeveloperView(NotifyIcon.DataContext as NotifyIconViewModel);
        }

        private static void LoadConfiguration()
        {
            Configuration = AppConfiguration.LoadConfiguration();

            PropertyInfo[] properties = typeof(AppConfiguration).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == "LogTemplate")
                    Logger.Information($"\t\t{property.Name} = {property.GetValue(Configuration).ToString().Replace("{SourceContext}", "{{SourceContext}}")}");
                else if (property.Name == "SimBinaries")
                    foreach (var item in property.GetValue(Configuration) as Dictionary<SimulatorType, string[]>)
                        Logger.Information($"\t\tSimBinaries[{item.Key}] = [{string.Join(", ", item.Value)}]");
                else if (property.GetValue(Configuration) is Dictionary<string, string>)
                    foreach (var item in property.GetValue(Configuration) as Dictionary<string, string>)
                        Logger.Information($"\t\t{property.Name}[{item.Key}] = {item.Value}");
                else
                    Logger.Information($"\t\t{property.Name} = {property.GetValue(Configuration)}");
            }
            AppConfiguration.SaveConfiguration();
        }

        private static void CheckFolders()
        {
            if (!Directory.Exists(AppConfiguration.DirProfiles))
                Directory.CreateDirectory(AppConfiguration.DirProfiles);

            if (!Directory.Exists(AppConfiguration.DirScripts))
                Directory.CreateDirectory(AppConfiguration.DirScripts);
            if (!Directory.Exists(AppConfiguration.DirScriptsGlobal))
                Directory.CreateDirectory(AppConfiguration.DirScriptsGlobal);
            if (!Directory.Exists(AppConfiguration.DirScriptsImage))
                Directory.CreateDirectory(AppConfiguration.DirScriptsImage);
        }

        private static bool ParseCommandLine()
        {
            bool result = true;
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 9)
            {
                for (int i = 1; i < args.Length - 1; i++)
                {
                    string name = args[i].Replace("-","");
                    i++;
                    CommandLineArgs.Add(name, args[i]);
                    Logger.Information($"\t\t{name} = {args[i]}");
                }

                string[] parameter = ["port", "pluginUUID", "registerEvent", "info"];
                foreach (string param in parameter)
                {
                    if (!CommandLineArgs.ContainsKey(param))
                    {
                        Logger.Error($"Command Line Arguments '{param}' not present!");
                        result = false;
                    }
                }
            }
            else
            {
                Logger.Error($"Invalid Number of Command Line Arguments: {args.Length}");
                result = false;
            }

            if (!result)
            {
                Logger.Error("Shutdown Plugin - Parameter for StreamDeck Connection missing");
                Console.WriteLine("Shutdown Plugin - Parameter for StreamDeck Connection missing");
                ExitCode = -1;
                DoShutdown();
            }
            return result;
        }

        protected static async Task CheckVersion()
        {
            try
            {
                var appVersion = Assembly.GetExecutingAssembly().GetName().Version;

                HttpClient client = new()
                {
                    Timeout = TimeSpan.FromMilliseconds(1000)
                };
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

                string json = await client.GetStringAsync("https://api.github.com/repos/Fragtality/PilotsDeck/releases/latest");
                Logger.Verbose($"json received len: {json?.Length}");
                JsonNode node = JsonSerializer.Deserialize<JsonNode>(json);
                string tag_name = node["tag_name"].ToString();
                if (tag_name.StartsWith('v'))
                    tag_name = tag_name[1..];

                if (Version.TryParse(tag_name, out Version repoVersion) && repoVersion > appVersion)
                {
                    UpdateDetected = true;
                    UpdateVersion = repoVersion.ToString(3);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}