using System;
using System.Collections.Generic;
using System.Windows;

namespace Installer
{
    public partial class App : Application
    {
        public static Dictionary<Simulator, bool> CmdLineIgnore = new Dictionary<Simulator, bool>();
        public static bool DebugMode = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logger.CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            if (e.Args.Length >= 1 && e.Args[0].ToLower() == "--ignoremsfs")
            {
                CmdLineIgnore.Add(Simulator.MSFS2020, true);
                Logger.Log(LogLevel.Information, $"Installer was started with IgnoreMSFS (2020)");
            }
            else
                CmdLineIgnore.Add(Simulator.MSFS2020, false);

            if (e.Args.Length >= 1 && e.Args[0].ToLower() == "--ignoremsfs24")
            {
                CmdLineIgnore.Add(Simulator.MSFS2024, true);
                Logger.Log(LogLevel.Information, $"Installer was started with IgnoreMSFS (2024)");
            }
            else
                CmdLineIgnore.Add(Simulator.MSFS2024, false);

            if (e.Args.Length >= 1 && e.Args[0].ToLower() == "--debug")
            {
                DebugMode = true;
                Logger.Log(LogLevel.Information, $"Installer was started in Debug Mode");
            }
        }

        public void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.LogException(args.ExceptionObject as Exception);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Logger.DestroyLogger();
        }
    }
}
