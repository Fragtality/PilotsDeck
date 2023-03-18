using System;
using System.Reflection;
using System.Text.RegularExpressions;

[assembly: AssemblyVersion("0.7.8.0")]
[assembly: AssemblyFileVersion("0.7.8.0")]

namespace Installer
{
    public static class Parameters
    {
        public static readonly string pilotsDeckVersion = "latest";

        public static readonly string sdPluginDir = @"\Elgato\StreamDeck\Plugins";
        public static readonly string pluginBinary = "PilotsDeck";
        public static readonly string pluginName = "com.extension.pilotsdeck.sdPlugin";
        public static readonly string unzipPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + sdPluginDir;
        public static readonly string pluginDir = unzipPath + "\\" + pluginName;
        public static readonly string fileName = "PilotsDeck-release.zip";
        public static readonly string importBinary = "ImportProfiles.exe";

        public static readonly string profileDir = pluginDir + @"\Profiles";
        public static readonly string defaultProfilesPattern = $"PilotsDeck - *";

        public static readonly Regex netCore = new Regex(@"Microsoft.NETCore.App ((\d+)\.(\d+)\.(\d+)).+", RegexOptions.Compiled);
        public static readonly Regex netDesktop = new Regex(@"Microsoft.WindowsDesktop.App ((\d+)\.(\d+)\.(\d+)).+", RegexOptions.Compiled);

        public static readonly int netMajor = 7;
        public static readonly int netMinor = 0;
        public static readonly int netPatch = 4;
        public static readonly string netVersion = $"{netMajor}.{netMinor}.{netPatch}";

        public static readonly string sdRegPath = @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck";
        public static readonly string sdRegValue = "last_started_streamdeck_version";
        public static readonly string sdVersion = "6.0.0";
        public static readonly string sdProfilePattern = "*.streamDeckProfile";
        public static readonly string sdBinary = "StreamDeck";

        public static readonly string ipcRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7";
        public static readonly string ipcRegValue = "DisplayVersion";
        public static readonly Regex ipcRegexVersion = new Regex(@"^v((\d+)\.(\d+)\.(\d+))$", RegexOptions.Compiled);
        public static readonly string ipcVersion = "7.3.18";

        public static readonly Regex wasmRegex = new Regex("^\\s*\"package_version\":\\s*\"([0-9\\.]+)\"\\s*,\\s*$", RegexOptions.Compiled);
        public static readonly string wasmIpcName = "fsuipc-lvar-module";
        public static readonly string wasmIpcVersion = "1.0.2";
        public static readonly string wasmMobiName = "mobiflight-event-module";
        public static readonly string wasmMobiVersion = "0.6.0";

        public static readonly string msConfigStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt";
        public static readonly string msConfigSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\UserCfg.opt";
        public static readonly string msStringPackage = "InstalledPackagesPath ";
    }
}
