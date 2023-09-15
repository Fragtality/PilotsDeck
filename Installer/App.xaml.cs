using System.Windows;

namespace Installer
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public static bool argIgnoreMSFS { get; private set; } = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1 && e.Args[0].ToLower() == "--ignoremsfs")
                argIgnoreMSFS = true;
        }
    }
}
