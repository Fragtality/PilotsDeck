using System;
using System.Collections.Generic;
using System.Windows;

namespace Installer
{
    public partial class App : Application
    {
        public static Dictionary<Simulator, bool> CmdLineIgnore = new Dictionary<Simulator, bool>();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Logger.CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            if (e.Args.Length == 1 && e.Args[0].ToLower() == "--ignoremsfs")
            {
                Logger.Log(LogLevel.Information, $"Installer was started with IgnoreMSFS (2020)");
                CmdLineIgnore.Add(Simulator.MSFS2020, true);
            }
            else
                CmdLineIgnore.Add(Simulator.MSFS2020, false);

            if (e.Args.Length == 1 && e.Args[0].ToLower() == "--ignoremsfs24")
            {
                Logger.Log(LogLevel.Information, $"Installer was started with IgnoreMSFS (2024)");
                CmdLineIgnore.Add(Simulator.MSFS2024, true);
            }
            else
                CmdLineIgnore.Add(Simulator.MSFS2024, false);
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
