﻿using System;
using System.Linq;
using System.Windows;

namespace Installer
{
    public partial class App : Application
    {
        public static bool CmdLineIgnoreMSFS { get; private set; } = false;
        public static bool CmdLineStreamDeck { get; private set; } = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0 && e.Args.Where(a => a.ToLowerInvariant().Contains("registerevent")).Any())
                CmdLineStreamDeck = true;

            Logger.CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            if (e.Args.Length == 1 && e.Args[0].ToLower() == "--ignoremsfs")
            {
                Logger.Log(LogLevel.Information, $"Installer was started with IgnoreMSFS");
                CmdLineIgnoreMSFS = true;
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
