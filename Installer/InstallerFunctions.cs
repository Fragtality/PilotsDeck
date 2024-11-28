using Extensions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Installer
{
    public static class InstallerFunctions
    {
        #region Install Actions
        public static bool CheckPluginInstalled()
        {
            try
            {
                return Directory.EnumerateFileSystemEntries(Parameters.pluginDir).Any() || Directory.Exists(Parameters.pluginDir);
            }
            catch { return false; }
        }

        public static void StartStreamDeckSoftware()
        {
            string bin = GetStreamDeckBinaryPath(out string folder);
            Tools.StartProcess(bin, folder);
        }

        public static bool WaitOnStreamDeckClose(int timeout = 15)
        {
            StopStreamDeckSoftware();
            for (int i = timeout; i > 0; i--)
            {
                InstallerTask.CurrentTask.ReplaceLastMessage($"Waiting for StreamDeck to close (Timeout {i}s) ...");
                Thread.Sleep(1000);
                if (!IsStreamDeckRunning())
                    break;
            }

            return !IsStreamDeckRunning();
        }

        public static bool StopStreamDeckSoftware(int sleep = 0)
        {
            Process proc = Tools.GetProcess(Parameters.sdBinary);
            proc?.Kill();
            proc = Tools.GetProcess(Parameters.pluginBinary);
            proc?.Kill();
            if (sleep > 0)
                Thread.Sleep(sleep);
            return IsStreamDeckRunning();
        }

        public static bool IsStreamDeckRunning()
        {
            return Tools.GetProcessRunning(Parameters.sdBinary) && Tools.GetProcessRunning(Parameters.pluginBinary);
        }

        public static string GetStreamDeckBinaryPath(out string sdFolder)
        {
            sdFolder = (string)Registry.GetValue(Parameters.sdRegPath, Parameters.sdRegFolder, null);
            if (!string.IsNullOrWhiteSpace(sdFolder))
                return $@"{sdFolder}{Parameters.sdBinary}\{Parameters.sdBinaryExe}";
            else
            {
                Logger.Log(LogLevel.Warning, "Could not get StreamDeck Folder from Registry!");
                return $@"{Parameters.sdDefaultFolder}{Parameters.sdBinary}\{Parameters.sdBinaryExe}";
            }
        }

        public static void DeleteDirectory(string path, bool create = false)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                if (create)
                    Directory.CreateDirectory(path);
            }
            else if (create)
                Directory.CreateDirectory(path);
        }

        public static bool DeleteOldFiles(bool resetConfig)
        {
            try
            {
                if (!Directory.Exists(Parameters.pluginDir))
                    return true;

                DirectoryInfo di = new DirectoryInfo(Parameters.pluginDir);
                foreach (FileInfo file in di.GetFiles())
                    if ((file.Name != Parameters.pluginConfig && file.Name != Parameters.colorConfig) || resetConfig)
                        file.Delete();

                int numFiles = (new DirectoryInfo(Parameters.pluginDir)).GetFiles().Length;
                bool streamDeckInstall = Directory.Exists($@"{Parameters.pluginDir}\previews") || numFiles == 0;
                bool filesDeleted = (numFiles <= 2 && !resetConfig) || (numFiles == 0 && resetConfig) || (numFiles == 0 && streamDeckInstall);

                string path = $@"{Parameters.pluginDir}\Plugin";
                DeleteDirectory(path);
                bool piDeleted = !Directory.Exists(path);

                DeleteDirectory($@"{Parameters.pluginDir}\logs");

                path = $@"{Parameters.pluginDir}\log";
                DeleteDirectory(path, true);
                bool logResetted = Directory.Exists(path);

                path = $@"{Parameters.pluginDir}\Images\Wait{{0}}.png";
                if (File.Exists(string.Format(path, "")))
                    File.Delete(string.Format(path, ""));
                if (File.Exists(string.Format(path, "@2x")))
                    File.Delete(string.Format(path, "@2x"));
                if (File.Exists(string.Format(path, "@3x")))
                    File.Delete(string.Format(path, "@3x"));

                DeleteDirectory($@"{Parameters.pluginDir}\bin");
                DeleteDirectory($@"{Parameters.pluginDir}\previews");
                DeleteDirectory($@"{Parameters.pluginDir}\ui");

                return filesDeleted && piDeleted && logResetted;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static bool ExtractZip()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Installer.{Parameters.fileName}"))
                {
                    ZipArchive archive = new ZipArchive(stream);
                    archive.ExtractToDirectory(Parameters.unzipPath, true);
                    stream.Close();
                }

                Tools.RunCommand($"powershell -WindowStyle Hidden -Command \"dir -Path {Parameters.unzipPath} -Recurse | Unblock-File\"");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static bool CreatePluginFolders()
        {
            try
            {
                Directory.CreateDirectory(Parameters.pluginDir + @"\log");
                Directory.CreateDirectory(Parameters.profileDir);
                Directory.CreateDirectory(Parameters.scriptDir);
                Directory.CreateDirectory(Parameters.scriptDir + @"\global");
                Directory.CreateDirectory(Parameters.scriptDir + @"\image");
                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static bool ExtractZipFile(string extractDir, string zipFile)
        {
            try
            {
                using (Stream stream = new FileStream(zipFile, FileMode.Open))
                {
                    ZipArchive archive = new ZipArchive(stream);
                    archive.ExtractToDirectory(extractDir);
                    stream.Close();
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static bool PlaceDesktopLink(string linkname, string description, string path)
        {
            bool result = false;
            try
            {
                IShellLink link = (IShellLink)new ShellLink();

                link.SetDescription(description);
                link.SetPath(path);

                IPersistFile file = (IPersistFile)link;
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                file.Save(Path.Combine(desktopPath, $"{linkname}.lnk"), false);
                result = true;
            }
            catch (Exception e)
            {
                InstallerTask.CurrentTask.SetError(e);
            }

            return result;
        }
        #endregion

        #region Check Requirements
        public static bool HasCustomProfiles(out bool oldDefault)
        {
            oldDefault = false;

            try
            {
                string profileDir = Parameters.profileDir;
                if (!Directory.Exists(profileDir))
                    return false;

                DirectoryInfo di = new DirectoryInfo(profileDir);
                var defaultFiles = di.GetFiles(Parameters.defaultProfilesPattern);
                var allFiles = di.GetFiles(Parameters.sdProfilePattern);

                oldDefault = defaultFiles.Length > 3;
                return defaultFiles.Length != allFiles.Length;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static readonly Regex rxNumberMatch = new Regex(@"\D*(\d+)\D*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            switch(comparison)
            {
                case VersionCompare.LESS:
                    return left < right;
                case VersionCompare.LESS_EQUAL:
                    return left <= right;
                case VersionCompare.GREATER:
                    return left > right;
                case VersionCompare.GREATER_EQUAL:
                    return left >= right;
                default:
                    return left == right;
            }
        }

        public static bool CheckPackageVersion(string packagePath, string packageName, string version)
        {
            try
            {
                string file = packagePath + "\\" + packageName + "\\manifest.json";
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (string line in lines)
                    {
                        if (Parameters.wasmRegex.IsMatch(line))
                        {
                            var matches = Parameters.wasmRegex.Matches(line);
                            if (matches?.Count == 1 && matches[0]?.Groups?.Count >= 2)
                                return CheckVersion(matches[0]?.Groups[1]?.Value, VersionCompare.GREATER_EQUAL, version, out bool compareable) && compareable;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
            }

            return false;
        }

        public static bool DownloadNetRuntime(string url, string file, out string fullpath, string workdir = "")
        {
            bool result = false;
            try
            {
                if (workdir == "")
                    workdir = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

                file = $@"{workdir}\{file}";
                fullpath = file;

                if (File.Exists(fullpath))
                    File.Delete(fullpath);

                var webClient = new WebClient();
                webClient.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                webClient.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                webClient.Headers.Add("Accept-Language", "en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                webClient.Headers.Add("Referer", "\r\nhttps://dotnet.microsoft.com/");
                webClient.Headers.Add("Sec-Ch-Ua", "\"Microsoft Edge\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                webClient.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
                webClient.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                webClient.Headers.Add("Sec-Ch-Ua-Platform-Version", "\"10.0.0\"");
                webClient.Headers.Add("Sec-Fetch-Dest", "document");
                webClient.Headers.Add("Sec-Fetch-Mode", "navigate");
                webClient.Headers.Add("Sec-Fetch-Site", "same-site");
                webClient.Headers.Add("Sec-Fetch-User", "?1");
                webClient.Headers.Add("Upgrade-Insecure-Requests", "1");
                webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0");
                webClient.DownloadFile(url, file);
                long size = (new FileInfo(file)).Length;
                result = File.Exists(file) && size > 1;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                fullpath = file;
            }

            return result;
        }


        public static bool DownloadFile(string url, string file, out string fullpath, string workdir = "")
        {
            bool result = false;
            try
            {
                if (workdir == "")
                    workdir = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

                file = $@"{workdir}\{file}";
                fullpath = file;

                if (File.Exists(fullpath))
                    File.Delete(fullpath);

                var webClient = new WebClient();
                webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0");
                webClient.DownloadFile(url, file);
                long size = (new FileInfo(file)).Length;
                result = File.Exists(file) && size > 1;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                fullpath = file;
            }

            return result;
        }

        public static bool CheckFSUIPC7(out bool isInstalled, string version = null)
        {
            bool result = false;
            isInstalled = false;
            string ipcVersion = Parameters.ipcVersion;
            if (!string.IsNullOrEmpty(version))
                ipcVersion = version;

            try
            {
                string regVersion = (string)Registry.GetValue(Parameters.ipcRegPath, Parameters.ipcRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion))
                {
                    isInstalled = true;
                    result = CheckVersion(regVersion, VersionCompare.GREATER_EQUAL, ipcVersion, out bool compareable) && compareable;
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
            }

            return result;
        }

        public static bool CheckFSUIPC6(string version = null)
        {
            return CheckFSUIPC6Variant(Parameters.ipc6RegPath4, version) || CheckFSUIPC6Variant(Parameters.ipc6RegPath5, version) || CheckFSUIPC6Variant(Parameters.ipc6RegPath6, version);
        }

        public static bool CheckFSUIPC6Variant(string regpath, string version = null)
        {
            bool result = false;
            string ipcVersion = Parameters.ipc6Version;
            if (!string.IsNullOrEmpty(version))
                ipcVersion = version;

            try
            {
                string regVersion = (string)Registry.GetValue(regpath, Parameters.ipcRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion))
                    result = CheckVersion(regVersion, VersionCompare.GREATER_EQUAL, ipcVersion, out bool compareable) && compareable;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
            }

            return result;
        }

        public static bool CheckFSUIPC7Pumps()
        {
            bool result = false;

            try
            { 
                string regPath = (string)Registry.GetValue(Parameters.ipcRegPath, Parameters.ipcRegInstallDirValue, null) + "\\" + "FSUIPC7.ini";

                if (File.Exists(regPath))
                {
                    string fileContent = File.ReadAllText(regPath);
                    if (fileContent.Contains("NumberOfPumps=0"))
                        result = true;
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
            }

            return result;
        }

        public static string FindPackagePath(string confFile)
        {
            Logger.Log(LogLevel.Debug, $"Getting PackagePath from {confFile}");
            string[] lines = File.ReadAllLines(confFile);
            foreach (string line in lines)
            {
                if (line.StartsWith(Parameters.msStringPackage))
                {
                    Logger.Log(LogLevel.Debug, $"Found Match!");
                    return line.Replace("\"", "").Substring(Parameters.msStringPackage.Length) + "\\Community";
                }
            }

            return "";
        }

        public static bool CheckInstalledMSFS(Simulator simulator, Dictionary<Simulator, string> packagePaths)
        {
            bool result = false;
            Logger.Log(LogLevel.Debug, $"Entered CheckInstalledMSFS Function with for simulator {simulator}");
            try
            {
                foreach (var storePath in Parameters.msConfigPaths[simulator])
                {
                    if (File.Exists(storePath))
                    {
                        Logger.Log(LogLevel.Debug, $"storePath exists: {storePath}");
                        string packagePath = FindPackagePath(storePath);
                        Logger.Log(LogLevel.Debug, $"returned packagePath: {packagePath}");
                        if (!string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath))
                        {
                            Logger.Log(LogLevel.Debug, $"Path exists!");
                            packagePaths.Add(simulator, packagePath);
                            result = true;
                        }
                        else
                        {
                            Logger.Log(LogLevel.Debug, $"Path does not exist!");
                            packagePaths.Add(simulator, "");
                        }
                    }
                    else
                        Logger.Log(LogLevel.Debug, $"storePath does not exist: {storePath}");
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
            }

            return result;
        }

        public static bool CheckStreamDeckSW(string version)
        {
            try
            {
                string regVersion = (string)Registry.GetValue(Parameters.sdRegPath, Parameters.sdRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion) && CheckVersion(regVersion, VersionCompare.GREATER_EQUAL, version, out bool compareable) && compareable)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static bool CheckDotNet()
        {
            try
            {
                bool installedDesktop = false;

                string[] output = Tools.RunCommand("dotnet --list-runtimes").Split(Environment.NewLine.ToCharArray());

                foreach (var line in output)
                {
                    var match = Parameters.netDesktop.Match(line);
                    if (match?.Groups?.Count == 2 && !string.IsNullOrWhiteSpace(match?.Groups[1]?.Value))
                        installedDesktop = CheckVersion(match.Groups[1].Value, VersionCompare.GREATER_EQUAL, Parameters.netVersion, out bool compareable) && compareable;
                    if (installedDesktop)
                        break;
                }

                return installedDesktop;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
                return false;
            }
        }

        public static bool CheckVjoy()
        {
            try
            {
                string regVersion = (string)Registry.GetValue(Parameters.vjoyRegPath, Parameters.vjoyRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion) && regVersion == Parameters.vjoyDisplayVersion)
                    return true;
            }
            catch (Exception e)
            {
                Logger.LogException(e);
                InstallerTask.CurrentTask.SetError(e);
            }

            return false;
        }
        #endregion
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
