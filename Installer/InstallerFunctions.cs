﻿using Extensions;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace Installer
{
    public static class InstallerFunctions
    {
        #region Install Actions
        public static bool StopStreamDeck()
        {
            try
            {
                bool isOpen = false;
                var procDeck = Process.GetProcessesByName(Parameters.sdBinary);
                var procPlugin = Process.GetProcessesByName(Parameters.pluginBinary);

                if (procDeck != null && procDeck.Length >= 1)
                {
                    procDeck[0].Kill();
                }
                if (procPlugin != null && procPlugin.Length >= 1)
                {
                    isOpen = true;
                    procPlugin[0].Kill();
                }

                if (isOpen)
                {
                    do
                        procPlugin = Process.GetProcessesByName(Parameters.pluginBinary);
                    while (procPlugin != null && procPlugin.Length >= 1);
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

        public static bool CheckStreamDeckSW()
        {
            try
            { 
                string regVersion = (string)Registry.GetValue(Parameters.sdRegPath, Parameters.sdRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion) && regVersion.StartsWith(Parameters.sdVersion))
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

        public static bool CheckDotNet()
        {
            try
            { 
                bool installedCore = false;
                bool installedDesktop = false;

                string output = RunCommand("dotnet --list-runtimes");

                var matches = Parameters.netCore.Matches(output);
                foreach (Match match in matches)
                {
                    if (!match.Success || match.Groups.Count != 5) continue;

                    if (StringGreaterEqual(match.Groups[2].Value, Parameters.netMajor) && StringGreaterEqual(match.Groups[3].Value, Parameters.netMinor) && StringGreaterEqual(match.Groups[4].Value, Parameters.netPatch))
                        installedCore = true;
                }

                matches = Parameters.netDesktop.Matches(output);
                foreach (Match match in matches)
                {
                    if (!match.Success || match.Groups.Count != 5) continue;

                    if (StringGreaterEqual(match.Groups[2].Value, Parameters.netMajor) && StringGreaterEqual(match.Groups[3].Value, Parameters.netMinor) && StringGreaterEqual(match.Groups[4].Value, Parameters.netPatch))
                        installedDesktop = true;
                }

                return installedCore && installedDesktop;
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
