using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using Installer.Worker;
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace Installer
{
    public class Config : ConfigBase
    {
        //InstallerOptions
        public virtual bool IgnoreMsfs2020 { get; set; } = false;
        public virtual bool IgnoreMsfs2024 { get; set; } = false;
        public virtual string ProfileManagerName { get { return "Profile Manager"; } }
        public virtual string ProfileManagerExePath { get { return Path.Combine(ProductPath, $"{ProfileManagerName.Replace(" ", "")}.exe"); } }
        public virtual bool Fsuipc7UseSecondaryConfig { get; set; } = true;

        public static readonly string OptionFsuipc7UseSecondary = "Fsuipc7UseSecondary";
        public static readonly string OptionVjoyInstallUpdate = "VjoyInstallUpdate";
        public static readonly string OptionResetConfiguration = "ResetConfiguration";

        //ConfigBase
        public override string ProductName { get { return PluginBinary; } }
        public static string PluginBinary { get { return "PilotsDeck"; } }
        public override string ProductConfigFile { get { return $"PluginConfig.json"; } }
        public static readonly string AppConfigUseFsuipcForMSFS = "UseFsuipcForMSFS";
        public virtual string ProductColorFile { get { return $"ColorStore.json"; } }
        public override string ProductConfigPath { get { return Path.Combine(ProductPath, ProductConfigFile); } }
        public override string ProductExePath { get { return Path.Combine(ProductPath, ProductExe); } }
        public override string ProductPath { get { return $@"{FuncStreamDeck.DeckPluginPath}\com.extension.pilotsdeck.sdPlugin"; } }
        public virtual string ProductPathProfiles { get { return Path.Combine(ProductPath, "Profiles"); } }
        public virtual string ProductPathScripts { get { return Path.Combine(ProductPath, "Scripts"); } }

        //Worker: .NET
        public virtual bool NetRuntimeDesktop { get; set; } = true;
        public virtual string NetVersion { get; set; } = "8.0.21";
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.21/windowsdesktop-runtime-8.0.21-win-x64.exe";
        public virtual string NetInstaller { get; set; } = "windowsdesktop-runtime-8.0.21-win-x64.exe";

        //Worker: MobiFlight
        public virtual bool MobiRequired { get; set; } = true;
        public virtual string MobiVersion { get; set; } = "1.0.1";

        //Worker: FSUIPC7
        public virtual bool Fsuipc7Required { get; set; } = true;
        public virtual string Fsuipc7Version { get; set; } = "7.5.4";
        public virtual string Fsuipc7WasmVersion { get; set; } = "1.0.7";
        public virtual string Fsuipc7Url { get; set; } = "https://fsuipc.com/download/Install_FSUIPC7.5.4.zip";
        public virtual bool Fsuipc7AllowBeta { get; set; } = false;
        public virtual bool Fsuipc7CheckPumps { get; set; } = true;

        //Worker: FSUIPC6
        public virtual bool Fsuipc6Required { get; set; } = true;
        public virtual string Fsuipc6Version { get; set; } = "6.2.2";
        public virtual string Fsuipc6Url { get; set; } = "https://fsuipc.simflight.com/beta/FSUIPC6.zip";
        public virtual bool Fsuipc6AllowBeta { get; set; } = false;

        //Worker: StreamDeck
        public virtual string DeckVersionMinimum { get { return "6.9.0"; } }
        public virtual string DeckVersionTarget { get { return "7.0.3"; } }
        public virtual string DeckUrl { get { return "https://edge.elgato.com/egc/windows/sd/Stream_Deck_7.0.3.22071.msi"; } }
        public virtual string DeckInstaller { get { return "Stream_Deck_7.0.3.22071.msi"; } }

        //Prepar3D
        public virtual string P3dRegPath { get { return @"HKEY_CURRENT_USER\SOFTWARE\Lockheed Martin"; } }
        public virtual string P3dRegFolderPrefix { get { return "Prepar3D v"; } }
        public virtual string P3dRegValueInstalled { get { return "Installed"; } }

        //Worker: vJoy
        public virtual string VjoyUrl { get; set; } = "https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/vJoySetup_v2.2.2.0_Win10_Win11.exe";
        public virtual string VjoyUrlFile { get; set; } = "vJoySetup_v2.2.2.0_Win10_Win11.exe";
        public virtual string VjoyVersion { get; set; } = "2.2.2.0";


        //CheckInstallerOptions
        public override void CheckInstallerOptions()
        {
            base.CheckInstallerOptions();

            //FSUIPC7 Secondary Connector
            if (CheckExistingConfigFile())
                Fsuipc7UseSecondaryConfig = ReadFsuipcSecondaryConnector();
            SetOption(OptionFsuipc7UseSecondary, Fsuipc7UseSecondaryConfig);

            //Check vJoy Installation
            if (Mode == SetupMode.INSTALL)
                SetOption(OptionVjoyInstallUpdate, true);
            else if (Mode == SetupMode.UPDATE)
            {
                if (WorkerVjoyInstall.IsInstalled())
                    SetOption(OptionVjoyInstallUpdate, true);
                else
                    SetOption(OptionVjoyInstallUpdate, false);
            }
            else
                SetOption(OptionVjoyInstallUpdate, false);

            //ResetConfig
            SetOption(OptionResetConfiguration, false);
        }

        public virtual bool ReadFsuipcSecondaryConnector()
        {
            try
            {
                var json = JsonNode.Parse(File.ReadAllText(ProductConfigPath));
                var option = json[AppConfigUseFsuipcForMSFS];
                if (option != null)
                    return option.GetValue<bool>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return true;
        }
    }
}
