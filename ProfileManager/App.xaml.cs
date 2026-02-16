using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ProfileManager
{
    public partial class App : Application
    {
        public static Dictionary<string, string> CommandLineArgs { get; private set; } = [];

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Process.GetProcessesByName("ProfileManager").Length > 1)
            {
                MessageBox.Show("ProfileManager is already running!", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            if (!Directory.Exists(Parameters.PLUGIN_PATH))
            {
                var result = MessageBox.Show($"Could not find the plugin in the {Parameters.PlatformName} plugin directory.\n\nPath: {Parameters.PLUGIN_PATH}\n\nSwitch to {(Parameters.IsStreamDockMode ? "StreamDeck" : "HotSpot StreamDock")} mode?",
                    "Plugin Not Found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Parameters.ChangeHotSpotMode();
                    Logger.Information($"Switched to {Parameters.PlatformName} mode");
                    Logger.Information($"Plugin path: {Parameters.PLUGIN_PATH}");
                    if (!Directory.Exists(Parameters.PLUGIN_PATH))
                    {
                        MessageBox.Show($"Plugin path not found:\n{Parameters.PLUGIN_PATH}", "Plugin Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                        return;
                    }
                }
                else
                {
                    Application.Current.Shutdown();
                    return;
                }
            }
            else
            {
                Logger.Information($"Running in {Parameters.PlatformName} mode");
                Logger.Information($"Plugin path: {Parameters.PLUGIN_PATH}");
            }
            Directory.SetCurrentDirectory(Parameters.PLUGIN_PATH);

            Logger.CreateAppLoggerRotated(Config.Instance);
            Logger.Information($"---------------------------------------------------------------------------");
            Logger.Information($"Starting Profile Manager ...");

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Logger.Information($"Parsing Command Line ...");
            ParseCommandLine();
        }

        public void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.LogException(args.ExceptionObject as Exception);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        private static void ParseCommandLine()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 1; i < args.Length; i++)
                {
                    Logger.Debug($"CommandLine args[{i}]: {args[i]}");

                    if (args[i].StartsWith('-') && i + 1 < args.Length)
                        CommandLineArgs.Add(args[i].Replace("-", ""), args[i + 1]);
                    else if (args[i].StartsWith('-'))
                        CommandLineArgs.Add(args[i].Replace("-", ""), null);
                    else
                        CommandLineArgs.Add(args[i], null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }

}
