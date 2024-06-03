using System;
using System.Text.RegularExpressions;

namespace Installer
{
    public static class Parameters
    {
        public static readonly string pilotsDeckVersion = "0.8.0";

        public static readonly string sdPluginDir = @"\Elgato\StreamDeck\Plugins";
        public static readonly string pluginBinary = "PilotsDeck";
        public static readonly string pluginName = "com.extension.pilotsdeck.sdPlugin";
        public static readonly string unzipPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + sdPluginDir;
        public static readonly string pluginDir = unzipPath + "\\" + pluginName;
        public static readonly string fileName = "PilotsDeck-release.zip";
        public static readonly string importBinary = "ImportProfiles.exe";

        public static readonly string profileDir = pluginDir + @"\Profiles";
        public static readonly string scriptDir = pluginDir + @"\Scripts";
        public static readonly string defaultProfilesPattern = $"PilotsDeck - *";

        //public static readonly Regex netCore = new Regex(@"Microsoft.NETCore.App ((\d+)\.(\d+)\.(\d+)).+", RegexOptions.Compiled);
        public static readonly Regex netDesktop = new Regex(@"Microsoft.WindowsDesktop.App ((\d+)\.(\d+)\.(\d+)).+", RegexOptions.Compiled);

        public static readonly int netMajor = 8;
        public static readonly int netMinor = 0;
        public static readonly int netPatch = 6;
        public static readonly string netVersion = $"{netMajor}.{netMinor}.{netPatch}";
        public static readonly string netUrl = "https://download.visualstudio.microsoft.com/download/pr/76e5dbb2-6ae3-4629-9a84-527f8feb709c/09002599b32d5d01dc3aa5dcdffcc984/windowsdesktop-runtime-8.0.6-win-x64.exe";
        public static readonly string netUrlFile = "windowsdesktop-runtime-8.0.6-win-x64.exe";

        public static readonly string sdRegPath = @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck";
        public static readonly string sdRegValue = "last_started_streamdeck_version";
        public static readonly string sdVersion = "6.5.0";
        public static readonly string sdVersionRecommended = "6.6.1";
        public static readonly string sdProfilePattern = "*.streamDeckProfile";
        public static readonly string sdBinary = "StreamDeck";

        public static readonly string ipcRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7";
        public static readonly string ipcRegValue = "DisplayVersion";
        public static readonly string ipcRegInstallDirValue = "InstallDir";
        public static readonly Regex ipcRegexVersion = new Regex(@"^v((\d+)\.(\d+)\.(\d+))$", RegexOptions.Compiled);
        public static readonly string ipcVersion = "7.4.11";

        public static readonly Regex wasmRegex = new Regex("^\\s*\"package_version\":\\s*\"([0-9\\.]+)\"\\s*,\\s*$", RegexOptions.Compiled);
        public static readonly string wasmIpcName = "fsuipc-lvar-module";
        public static readonly string wasmIpcVersion = "1.0.3";
        public static readonly string wasmMobiName = "mobiflight-event-module";
        public static readonly string wasmMobiVersion = "1.0.1";
        public static readonly string wasmUrl = "https://github.com/MobiFlight/MobiFlight-WASM-Module/releases/download/1.0.1/mobiflight-event-module.1.0.1.zip";
        public static readonly string wasmUrlFile = "mobiflight-event-module.1.0.1.zip";

        public static readonly string msConfigStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt";
        public static readonly string msConfigSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\UserCfg.opt";
        public static readonly string msStringPackage = "InstalledPackagesPath ";
    }
}
