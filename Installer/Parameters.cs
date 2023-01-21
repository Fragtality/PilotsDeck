using System;
using System.Reflection;
using System.Text.RegularExpressions;

[assembly: AssemblyVersion("0.7.8.0")]
[assembly: AssemblyFileVersion("0.7.8.0")]

namespace Installer
{
    public static class Parameters
    {
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
        public static readonly int netPatch = 2;
        public static readonly string netVersion = $"{netMajor}.{netMinor}.{netPatch}";

        public static readonly string sdRegPath = @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck";
        public static readonly string sdRegValue = "last_started_streamdeck_version";
        public static readonly string sdVersion = "6.0";
        public static readonly string sdProfilePattern = "*.streamDeckProfile";
        public static readonly string sdBinary = "StreamDeck";
    }
}
