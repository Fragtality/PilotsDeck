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

        public static readonly Regex versionUnclean = new(@"(\d+)\D", RegexOptions.Compiled);

        public static bool CheckVersion(string versionInstalled, string versionRequired, bool majorEqual, bool ignoreBuild, out bool wrongVersionSyntax)
        {
            bool majorMatch = false;
            bool minorMatch = false;
            bool patchMatch = false;
            bool prevWasEqual = false;

            if (string.IsNullOrWhiteSpace(versionInstalled) || string.IsNullOrWhiteSpace(versionRequired))
            {
                wrongVersionSyntax = true;
                return false;
            }

            string[] strInst = versionInstalled.Split('.');
            string[] strReq = versionRequired.Split('.');
            if (strInst.Length < 3 || strReq.Length < 3)
            {
                wrongVersionSyntax = true;
                return false;
            }
            else
                wrongVersionSyntax = false;

            for (int i = 0; i < strInst.Length; i++)
            {
                if (versionUnclean.IsMatch(strInst[i]))
                    strInst[i] = strInst[i][..^1];
            }

            //Major
            if (int.TryParse(strInst[0], out int vInst) && int.TryParse(strReq[0], out int vReq))
            {
                if (majorEqual)
                    majorMatch = vInst == vReq;
                else
                    majorMatch = vInst >= vReq;

                prevWasEqual = vInst == vReq;
            }

            //Minor
            if (int.TryParse(strInst[1], out vInst) && int.TryParse(strReq[1], out vReq))
            {
                if (prevWasEqual)
                    minorMatch = vInst >= vReq;
                else
                    minorMatch = true;

                prevWasEqual = vInst == vReq;
            }

            //Patch
            if (!ignoreBuild)
            {
                if (int.TryParse(strInst[2], out vInst) && int.TryParse(strReq[2], out vReq))
                {
                    if (prevWasEqual)
                        patchMatch = vInst >= vReq;
                    else
                        patchMatch = true;
                }
            }
            else
                patchMatch = true;

            return majorMatch && minorMatch && patchMatch;
        }
    }
}
