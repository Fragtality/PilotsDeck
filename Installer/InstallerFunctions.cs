using Extensions;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace Installer
{
    public static class InstallerFunctions
    {
        public static bool GetProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null && proc.ProcessName == name;
        }

        #region Install Actions
        public static bool StopStreamDeck()
        {
            try
            {
                bool isOpen = false;
                var procDeck = Process.GetProcessesByName(Parameters.sdBinary).FirstOrDefault();
                var procPlugin = Process.GetProcessesByName(Parameters.pluginBinary).FirstOrDefault();

                if (GetProcessRunning(Parameters.sdBinary))
                {
                    procDeck.Kill();
                }
                if (GetProcessRunning(Parameters.pluginBinary))
                {
                    isOpen = true;
                    procPlugin.Kill();
                }

                if (isOpen)
                {
                    do
                        Thread.Sleep(250);
                    while (GetProcessRunning(Parameters.pluginBinary));
                    Thread.Sleep(2000);
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during StopStreamDeck", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool DeleteOldFiles()
        {
            try
            {
                if (!Directory.Exists(Parameters.pluginDir))
                    return true;

                DirectoryInfo di = new DirectoryInfo(Parameters.pluginDir);
                foreach (FileInfo file in di.GetFiles())
                    file.Delete();

                return (new DirectoryInfo(Parameters.pluginDir)).GetFiles().Length == 0;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during RemoveOldFiles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                RunCommand($"powershell -WindowStyle Hidden -Command \"dir -Path {Parameters.unzipPath} -Recurse | Unblock-File\"");

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during ExtractZip", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool CreateScriptFolder()
        {
            try
            {
                Directory.CreateDirectory(Parameters.scriptDir);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CreateScriptFolder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Exception '{e.GetType()}' during ExtractZipFile", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
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
                MessageBox.Show($"Exception '{e.GetType()}' during HasCustomProfiles", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool CheckVersion(string versionInstalled, string versionRequired, bool majorEqual, bool ignoreBuild)
        {
            bool majorMatch = false;
            bool minorMatch = false;
            bool patchMatch = false;

            string[] strInst = versionInstalled.Split('.');
            string[] strReq = versionRequired.Split('.');
            int vInst;
            int vReq;
            bool prevWasEqual = false;

            for (int i = 0; i < strInst.Length; i++)
            {
                if (Regex.IsMatch(strInst[i], @"(\d+)\D"))
                    strInst[i] = strInst[i].Substring(0, strInst[i].Length - 1);
            }

            //Major
            if (int.TryParse(strInst[0], out vInst) && int.TryParse(strReq[0], out vReq))
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
                            if (matches.Count == 1 && matches[0].Groups.Count >= 2)
                                return CheckVersion(matches[0].Groups[1].Value, version, false, false);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckPackageVersion", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public static bool DownloadFile(string url, string file)
        {
            bool result = false;
            try
            {
                var webClient = new WebClient();
                webClient.DownloadFile(url, file);
                result = File.Exists(file);

            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during DownloadFile", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static bool CheckFSUIPC(string version = null)
        {
            bool result = false;
            string ipcVersion = Parameters.ipcVersion;
            if (!string.IsNullOrEmpty(version))
                ipcVersion = version;

            try
            {
                string regVersion = (string)Registry.GetValue(Parameters.ipcRegPath, Parameters.ipcRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion))
                {
                    regVersion = regVersion.Substring(1);
                    int index = regVersion.IndexOf("(beta)");
                    if (index > 0)
                        regVersion = regVersion.Substring(0, index).TrimEnd();
                    result = CheckVersion(regVersion, ipcVersion, true, false);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckFSUIPC", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Exception '{e.GetType()}' during CheckFSUIPC", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        public static string FindPackagePath(string confFile)
        {
            string[] lines = File.ReadAllLines(confFile);
            foreach (string line in lines)
            {
                if (line.StartsWith(Parameters.msStringPackage))
                {
                    return line.Replace("\"", "").Substring(Parameters.msStringPackage.Length) + "\\Community";
                }
            }

            return "";
        }

        public static bool CheckInstalledMSFS(out string packagePath)
        {
            try
            {
                if (File.Exists(Parameters.msConfigStore))
                {
                    packagePath = FindPackagePath(Parameters.msConfigStore);
                    return !string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath);
                }
                else if (File.Exists(Parameters.msConfigSteam))
                {
                    packagePath = FindPackagePath(Parameters.msConfigSteam);
                    return !string.IsNullOrWhiteSpace(packagePath) && Directory.Exists(packagePath);
                }

                packagePath = "";
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckInstalledMSFS", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            packagePath = "";
            return false;
        }

        public static bool CheckStreamDeckSW(string version)
        {
            try
            {
                string regVersion = (string)Registry.GetValue(Parameters.sdRegPath, Parameters.sdRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion) && CheckVersion(regVersion, version, true, true))
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckStreamDeckSW", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
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

        public static bool StringGreaterEqual(string input, int compare)
        {
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int numA) && numA >= compare)
                return true;
            else
                return false;
        }

        public static bool StringEqual(string input, int compare)
        {
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int numA) && numA == compare)
                return true;
            else
                return false;
        }

        public static bool StringGreater(string input, int compare)
        {
            if (int.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out int numA) && numA > compare)
                return true;
            else
                return false;
        }

        public static bool CheckDotNet()
        {
            try
            {
                bool installedDesktop = false;

                string output = RunCommand("dotnet --list-runtimes");

                var matches = Parameters.netDesktop.Matches(output);
                foreach (Match match in matches)
                {
                    if (!match.Success || match.Groups.Count != 5)
                        continue;
                    if (!StringEqual(match.Groups[2].Value, Parameters.netMajor))
                        continue;
                    if ((StringEqual(match.Groups[3].Value, Parameters.netMinor) && StringGreaterEqual(match.Groups[4].Value, Parameters.netPatch))
                        || StringGreater(match.Groups[3].Value, Parameters.netMinor))
                        installedDesktop = true;
                }

                return installedDesktop;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Exception '{e.GetType()}' during CheckDotNet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion
    }
}
