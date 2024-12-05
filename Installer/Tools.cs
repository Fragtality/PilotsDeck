using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;


namespace Installer
{
    public class Tools
    {
        public static bool GetProcessRunning(string appName)
        {
            Process proc = GetProcess(appName);
            return proc != null && proc.ProcessName == appName;
        }

        public static Process GetProcess(string appName)
        {
            return Process.GetProcessesByName(appName).FirstOrDefault();
        }

        public static void StopProcess(string name)
        {
            Process proc = GetProcess(name);
            proc.Kill();
        }

        public static void StartProcess(string absolutePath, string workDirectory = null, string args = null)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return;

            if (!absolutePath.StartsWith("http") && (!File.Exists(absolutePath) || (workDirectory != null && !Directory.Exists(workDirectory))))
            {
                Logger.Log(LogLevel.Warning, $"The Path '{absolutePath}' does not exist! (WorkDir was: '{workDirectory}')");
                return;
            }

            var pProcess = new Process();
            pProcess.StartInfo.FileName = absolutePath;
            pProcess.StartInfo.UseShellExecute = true;
            pProcess.StartInfo.WorkingDirectory = workDirectory ?? Directory.GetCurrentDirectory();
            if (args != null)
                pProcess.StartInfo.Arguments = args;
            pProcess.Start();
        }

        public static string RunCommand(string command)
        {
            var pProcess = new Process();
            pProcess.StartInfo.FileName = "cmd.exe";
            pProcess.StartInfo.Arguments = "/C" + command;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();

            return strOutput ?? "";
        }

        public static void SetButtonImage(Image image, string icon)
        {
            image.Source = new BitmapImage(new Uri($"pack://application:,,,/images/{icon}.png"));
        }

        public static void OpenUriArgs(string url, string args, string directory = null)
        {
            StartProcess(url, directory, args);
        }

        public static void OpenUri(object sender, RequestNavigateEventArgs e)
        {
            OpenUri(sender, e);
        }

        public static void OpenUri(object sender, RequestNavigateEventArgs e, string directory = null)
        {
            if (!e.Uri.ToString().Contains(".exe"))
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
            else
            {
                StartProcess(e.Uri.AbsolutePath, directory);
            }
            e.Handled = true;
        }
    }
}
