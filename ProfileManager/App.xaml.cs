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

            Directory.SetCurrentDirectory(Parameters.PLUGIN_PATH);

            Logger.CreateLogger();
            Logger.Log(LogLevel.Information, $"---------------------------------------------------------------------------");
            Logger.Log(LogLevel.Information, $"Starting Profile Manager ...");

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            ParseCommandLine();
        }

        public void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.LogException(args.ExceptionObject as Exception);
        }

        private static void ParseCommandLine()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 1; i < args.Length; i++)
                {
                    Logger.Log(LogLevel.Debug, $"CommandLine args[{i}]: {args[i]}");

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
