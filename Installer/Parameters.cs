using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Installer
{
    public static class Parameters
    {
        public static readonly string pilotsDeckVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        public static readonly string sdPluginDir = @"\Elgato\StreamDeck\Plugins";
        public static readonly string pluginBinary = "PilotsDeck";
        public static readonly string pluginName = "com.extension.pilotsdeck.sdPlugin";
        public static readonly string unzipPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + sdPluginDir;
        public static readonly string pluginDir = unzipPath + "\\" + pluginName;
        public static readonly string fileName = "PilotsDeck-release.zip";
        public static readonly string importBinary = "ImportProfiles.exe";
        public static readonly string pluginConfig = "PluginConfig.json";
        public static readonly string colorConfig = "ColorStore.json";

        public static readonly string profileDir = pluginDir + @"\Profiles";
        public static readonly string scriptDir = pluginDir + @"\Scripts";
        public static readonly string defaultProfilesPattern = $"PilotsDeck - *";

        public static readonly Regex netDesktop = new Regex(@"Microsoft.WindowsDesktop.App (\d+\.\d+\.\d+).+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly int netMajor = 8;
        public static readonly int netMinor = 0;
        public static readonly int netPatch = 8;
        public static readonly string netVersion = $"{netMajor}.{netMinor}.{netPatch}";
        public static readonly string netUrl = "https://download.visualstudio.microsoft.com/download/pr/907765b0-2bf8-494e-93aa-5ef9553c5d68/a9308dc010617e6716c0e6abd53b05ce/windowsdesktop-runtime-8.0.8-win-x64.exe";
        public static readonly string netUrlFile = "windowsdesktop-runtime-8.0.8-win-x64.exe";

        public static readonly string sdRegPath = @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck";
        public static readonly string sdRegValue = "last_started_streamdeck_version";
        public static readonly string sdRegFolder = "Folder";
        public static readonly string sdVersion = "6.5.0";
        public static readonly string sdVersionRecommended = "6.7.2";
        public static readonly string sdProfilePattern = "*.streamDeckProfile";
        public static readonly string sdBinary = "StreamDeck";
        public static readonly string sdBinaryExe = "StreamDeck.exe";
        public static readonly string sdDefaultFolder = @"C:\Program Files\Elgato\";
        public static readonly string sdUrl = "https://edge.elgato.com/egc/windows/sd/Stream_Deck_6.7.2.20986.msi";
        public static readonly string sdUrlFile = "Stream_Deck_6.7.2.20986.msi";

        //7
        public static readonly string ipcRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC7";
        public static readonly string ipcRegValue = "DisplayVersion";
        public static readonly string ipcRegInstallDirValue = "InstallDir";
        public static readonly Regex ipcRegexVersion = new Regex(@"^v((\d+)\.(\d+)\.(\d+))$", RegexOptions.Compiled);
        public static readonly string ipcVersion = "7.4.16";
        public static readonly string ipcUrl = "https://fsuipc.simflight.com/beta/Install_FSUIPC7.zip";
        public static readonly string ipcSetup = "Install_FSUIPC7";
        public static readonly string ipcUrlFile = $"{ipcSetup}.zip";
        public static readonly string ipcSetupFile = $"{ipcSetup}.exe";

        //6
        public static readonly string ipc6RegPath4 = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC6v4";
        public static readonly string ipc6RegPath5 = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC6v5";
        public static readonly string ipc6RegPath6 = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\FSUIPC6v6";
        public static readonly string ipc6Version = "6.2.0";
        public static readonly string ipc6Url = "https://fsuipc.simflight.com/beta/FSUIPC6.zip";
        public static readonly string ipc6Setup = "FSUIPC6";
        public static readonly string ipc6UrlFile = $"{ipc6Setup}.zip";
        public static readonly string ipc6SetupFile = $"Install_{ipc6Setup}.exe";

        public static readonly Regex wasmRegex = new Regex("^\\s*\"package_version\":\\s*\"([0-9\\.]+)\"\\s*,\\s*$", RegexOptions.Compiled);
        public static readonly string wasmIpcName = "fsuipc-lvar-module";
        public static readonly string wasmIpcVersion = "1.0.5";
        public static readonly string wasmMobiName = "mobiflight-event-module";
        public static readonly string wasmMobiVersion = "1.0.1";
        public static readonly string wasmUrl = "https://github.com/MobiFlight/MobiFlight-WASM-Module/releases/download/1.0.1/mobiflight-event-module.1.0.1.zip";
        public static readonly string wasmUrlFile = "mobiflight-event-module.1.0.1.zip";

        public static readonly string msConfigStore = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt";
        public static readonly string msConfigSteam = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft Flight Simulator\UserCfg.opt";
        public static readonly string msStringPackage = "InstalledPackagesPath ";

        public static readonly string vjoyUrl = "https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/vJoySetup_v2.2.2.0_Win10_Win11.exe";
        public static readonly string vjoyUrlFile = "vJoySetup_v2.2.2.0_Win10_Win11.exe";
        public static readonly string vjoyRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{8E31F76F-74C3-47F1-9550-E041EEDC5FBB}_is1";
        public static readonly string vjoyRegValue = "DisplayVersion";
        public static readonly string vjoyDisplayVersion = "2.2.2.0";

        public static readonly string prepRegPath = @"HKEY_CURRENT_USER\SOFTWARE\Lockheed Martin";
        public static readonly string prepRegFolderPrefix = @"Prepar3D v";
        public static readonly string prepRegValueInstalled = "Installed";
    }
}
