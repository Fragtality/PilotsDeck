using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ProfileManager
{
    public static partial class Tools
    {
        public async static Task WaitOnTask(InstallerTask task, string messagePattern, int waitSec)
        {
            for (int i = waitSec; i > 0; i--)
            {
                task.ReplaceLastMessage(string.Format(messagePattern, i));
                await Task.Delay(1000);
            }
        }

        public static void StartStreamDeckSoftware()
        {
            string bin = GetStreamDeckBinaryPath(out string folder);
            StartProcess(bin, folder);
        }

        public async static Task StopStreamDeckSoftware(int waitMs = 0)
        {
            Process proc = GetProcess(Parameters.SD_BINARY_NAME);
            proc.Kill();

            if (waitMs != 0)
                await Task.Delay(waitMs);
        }

        public static bool IsStreamDeckRunning()
        {
            return GetProcessRunning(Parameters.SD_BINARY_NAME);
        }

        public static string GetStreamDeckBinaryPath(out string sdFolder)
        {
            sdFolder = (string)Registry.GetValue(Parameters.SD_REG_PATH, Parameters.SD_REG_VALUE_FOLDER, null);
            if (!string.IsNullOrWhiteSpace(sdFolder))
                return $@"{sdFolder}{Parameters.SD_BINARY_NAME}\{Parameters.SD_BINARY_EXE}";
            else
            {
                Logger.Log(LogLevel.Warning, "Could not get StreamDeck Folder from Registry!");
                return $@"{Parameters.SD_DEFAULT_FOLDER}{Parameters.SD_BINARY_NAME}\{Parameters.SD_BINARY_EXE}";
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        public static void SetForegroundWindow(string title)
        {
            SetForegroundWindow(FindWindowByCaption(IntPtr.Zero, title));
        }

        public static void StartProcess(string absolutePath, string workDirectory = null, string args = null)
        {
            var pProcess = new Process();
            pProcess.StartInfo.FileName = absolutePath;
            pProcess.StartInfo.UseShellExecute = true;
            pProcess.StartInfo.WorkingDirectory = workDirectory ?? Directory.GetCurrentDirectory();
            if (args != null)
                pProcess.StartInfo.Arguments = args;
            pProcess.Start();
        }

        public static bool GetProcessRunning(string appName)
        {
            Process proc = GetProcess(appName);
            return proc != null && proc.ProcessName == appName;
        }

        public static Process GetProcess(string appName)
        {
            return Process.GetProcessesByName(appName).FirstOrDefault();
        }

        public static void SetButtonImage(Image image, string icon)
        {
            image.Source = new BitmapImage(new Uri($"pack://application:,,,/ProfileManager;component/images/{icon}.png"));
        }

        public static void OpenUriArgs(string url, string args, string directory = null)
        {
            Logger.Log(LogLevel.Debug, $"url: '{url}' | args: '{args}' | directory '{directory}'");

            StartProcess(url, directory, args);
        }

        public static void OpenUri(object sender, RequestNavigateEventArgs e, string directory = null)
        {
            Logger.Log(LogLevel.Debug, $"e: '{e}' | sender: '{sender}' | uri '{e.Uri}' | directory '{directory}'");

            if (!e.Uri.ToString().Contains(".exe"))
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
            else
            {
                StartProcess(e.Uri.AbsolutePath, directory);
            }
            e.Handled = true;
        }

        public static string CreateMD5(string input)
        {
            StringBuilder sb = new();
            try
            {
                byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
                
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return sb.ToString().ToUpper();
        }

        public static readonly Regex rxNumberMatch = new(@"\D*(\d+)\D*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string[] CleanNumbers(string[] versions)
        {
            for (int i = 0; i < versions.Length; i++)
            {
                var match = rxNumberMatch.Match(versions[i]);
                if (match?.Groups?.Count == 2 && !string.IsNullOrWhiteSpace(match?.Groups[1]?.Value))
                    versions[i] = match.Groups[1].Value;
                else
                    return null;
            }

            return versions;
        }

        public enum VersionCompare
        {
            EQUAL = 1,
            LESS,
            LESS_EQUAL,
            GREATER,
            GREATER_EQUAL
        }

        public static bool CheckVersion(string leftVersion, VersionCompare comparison, string rightVersion, out bool compareable, int digits = 3)
        {
            compareable = false;

            if (string.IsNullOrWhiteSpace(leftVersion) || string.IsNullOrWhiteSpace(rightVersion))
                return false;

            string[] leftParts = leftVersion.Split('.');
            string[] rightParts = rightVersion.Split('.');
            if (leftParts.Length < digits || rightParts.Length < digits)
                return false;

            leftParts = CleanNumbers(leftParts);
            rightParts = CleanNumbers(rightParts);
            if (leftParts == null || rightParts == null)
                return false;

            leftVersion = string.Join(".", leftParts);
            rightVersion = string.Join(".", rightParts);
            if (!Version.TryParse(leftVersion, out Version left) || !Version.TryParse(rightVersion, out Version right))
                return false;

            compareable = true;
            return comparison switch
            {
                VersionCompare.LESS => left < right,
                VersionCompare.LESS_EQUAL => left <= right,
                VersionCompare.GREATER => left > right,
                VersionCompare.GREATER_EQUAL => left >= right,
                _ => left == right,
            };
        }
    }
}
