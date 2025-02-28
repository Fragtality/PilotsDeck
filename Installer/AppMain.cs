using CFIT.Installer;
using CFIT.Installer.UI.Behavior;
using System;

namespace Installer
{
    public class AppMain
    {
        public static InstallerApp<Definition, WindowBehavior, Config, WorkerManager> Instance { get; private set; }

        [STAThread]
        public static int Main(string[] args)
        {
            Instance = new InstallerApp<Definition, WindowBehavior, Config, WorkerManager>(new Definition(args));
            return Instance.Start();
        }
    }
}