using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Modules.MobiFlight;
using PilotsDeck.Simulator;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotsDeck
{
    public class AppConfiguration: ISimConnectConfig, ILoggerConfig, IMobiConfig
    {
        //Constants
        [JsonIgnore]
        public static string SdEncoder { get; } = "Encoder";
        [JsonIgnore]
        public static string SdTargetImage { get; } = "canvas";
        [JsonIgnore]
        public static string SdEncoderBackground { get; } = "Plugin/Images/Encoder.png";
        [JsonIgnore]
        public static string WaitImage { get; } = @"Plugin/Images/Wait.png";
        [JsonIgnore]
        public static string WaitImageTemplate { get; } = @"Plugin/Images/Wait{0}.png";
        [JsonIgnore]
        public static string ImageSuffixHq { get; } = "@2x";
        [JsonIgnore]
        public static string ImageSuffixPlus { get; } = "@3x";
        [JsonIgnore]
        public static string ConfigFile { get; } = "PluginConfig.json";
        [JsonIgnore]
        public static string ColorFile { get; } = "ColorStore.json";
        [JsonIgnore]
        public static int BuildModelVersion { get; } = 8;
        [JsonIgnore]
        public static int BuildConfigVersion { get; } = 13;
        [JsonIgnore]
        public static string PluginUUID { get; } = "com.extension.pilotsdeck";
        [JsonIgnore]
        public static string ModelSimple { get; } = "settingsModel";
        [JsonIgnore]
        public static string ModelAdvanced { get; } = "settingsStore";
        [JsonIgnore]
        public static string IpcGroupRead { get; } = $"{SC_CLIENT_NAME}Read";
        [JsonIgnore]
        public static string IpcGroupWrite { get; } = $"{SC_CLIENT_NAME}Write";
        [JsonIgnore]
        public static string IpcInMenuFsxAddr { get; } = "0x3364:1";
        [JsonIgnore]
        public static string IpcInMenuAddr { get; } = "0x3365:1";
        [JsonIgnore]
        public static string IpcIsPausedAddr { get; } = "0x0264:2";
        [JsonIgnore]
        public static string IpcAircraftPathAddr { get; } = "0x3C00:256:s";
        [JsonIgnore]
        public static string DirProfiles { get; } = "Profiles/";
        [JsonIgnore]
        public static string DirProfilesName { get { return DirProfiles.TrimEnd('/'); } }
        [JsonIgnore]
        public static string DirImages { get; } = "Images/";
        [JsonIgnore]
        public static string DirImagesName { get { return DirImages.TrimEnd('/'); } }
        [JsonIgnore]
        public static string DirKorry { get { return $"{DirImages}korry"; } }
        [JsonIgnore]
        public static string ImageExtension { get; } = ".png";
        [JsonIgnore]
        public static string ImageExtensionFilter { get { return $"*{ImageExtension}"; } }
        [JsonIgnore]
        public static string DirScripts { get; } = @"Scripts/";
        [JsonIgnore]
        public static string DirScriptsGlobal { get; } = $"{DirScripts}global/";
        [JsonIgnore]
        public static string DirScriptsImage { get; } = $"{DirScripts}image/";
        [JsonIgnore]
        public static readonly uint WM_PILOTSDECK_SIMCONNECT = 0x1994;
        [JsonIgnore]
        public const int MOBI_PILOTSDECK_CLIENTID = 1994;
        [JsonIgnore]
        public static readonly uint WM_PILOTSDECK_REQ_SIMCONNECT = 0x1995;
        [JsonIgnore]
        public static readonly uint WM_PILOTSDECK_REQ_DESIGNER = 0x1996;
        [JsonIgnore]
        public static readonly string SC_CLIENT_NAME = "PilotsDeck";
        [JsonIgnore]
        public static readonly string FILE_LVAR = "L-Vars.txt";
        [JsonIgnore]
        public static readonly string FILE_BVAR = "InputEvents.txt";
        [JsonIgnore]
        public float SCALE_LEGACY { get; set; } = 120.0f / 144.0f;
        [JsonIgnore]
        public float SCALE_SESSION { get; set; } = 1;

        //ISimConnectConfig
        [JsonIgnore]
        public string ClientName { get { return SC_CLIENT_NAME; } }
        [JsonIgnore]
        public int RetryDelay { get { return MsfsRetryDelay; } }
        [JsonIgnore]
        public int StaleTimeout { get { return MsfsStaleTimeout; } }
        [JsonIgnore]
        public int CheckInterval { get { return MsfsStateCheckInterval; } }
        [JsonIgnore]
        public bool CreateWindow { get { return false; } }
        [JsonIgnore]
        public int MsgSimConnect { get { return (int)WM_PILOTSDECK_SIMCONNECT; } }
        [JsonIgnore]
        public int MsgConnectRequest { get { return (int)WM_PILOTSDECK_REQ_SIMCONNECT; } }

        public uint IdBase { get; set; } = 500;
        public uint SizeVariables { get; set; } = 10000;
        public uint SizeEvents { get; set; } = 10000;
        public uint SizeSimStates { get; set; } = 100;
        public uint SizeInputEvents { get; set; } = 10000;

        [JsonIgnore]
        public bool VerboseLogging { get { return LogLevel == LogLevel.Verbose; } }
        [JsonIgnore]
        public string BinaryMsfs2020 { get { return SimBinaries[SimulatorType.MSFS][0]; } }
        [JsonIgnore]
        public string BinaryMsfs2024 { get { return SimBinaries[SimulatorType.MSFS][1]; } }

        //IMobiConfig
        [JsonIgnore]
        public bool MobiWriteLvars { get { return !string.IsNullOrWhiteSpace(FILE_LVAR); } }
        [JsonIgnore]
        public string MobiLvarFile { get { return FILE_LVAR; } }
        [JsonIgnore]
        public bool MobiSetVarPerFrame { get { return MobiVarsPerFrame > 0; } }
        public uint MobiSizeVariables { get; set; } = 10000;


        //ConfigFile
        public int ConfigVersion { get; set; } = BuildConfigVersion;
        public string LogDirectory { get; set; } = "log";
        public string LogFile { get; set; } = "PilotsDeck.log";
        [JsonIgnore]
        public int SizeLimit { get; set; } = -1;
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
        public RollingInterval LogInterval { get; set; } = RollingInterval.Day;
        public int LogCount { get; set; } = 2;
        public string LogTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message} {NewLine}";
        public string StreamDeckHost { get; set; } = "localhost";
        public string FontLocaleKey { get; set; } = "en";
        public Dictionary<string, string> FontRegularName { get; set; } = new()
        {
            { "de", "Standard" },
            { "en", "Regular" },
        };
        public string GetFontRegularName()
        {
            if (FontRegularName.TryGetValue(FontLocaleKey, out string name))
                return name;
            else
                return FontRegularName["en"];
        }
        public Dictionary<string, string> FontBoldName { get; set; } = new()
        {
            { "de", "Fett" },
            { "en", "Bold" },
        };
        public string GetFontBoldName()
        {
            if (FontBoldName.TryGetValue(FontLocaleKey, out string name))
                return name;
            else
                return FontBoldName["en"];
        }
        public Dictionary<string, string> FontItalicName { get; set; } = new()
        {
            { "de", "Kursiv" },
            { "en", "Italic" },
        };
        public string GetFontItalicName()
        {
            if (FontItalicName.TryGetValue(FontLocaleKey, out string name))
                return name;
            else
                return FontItalicName["en"];
        }
        public float RenderDpi { get; set; } = 96.0f;
        public string StringReplace { get; set; } = "%s";
        public bool CleanInactiveCommands { get; set; } = false;
        public int DelayExit { get; set; } = 2000;
        public int DelayStreamDeckConnect { get; set; } = 750;
        public int IntervalDeckRefresh { get; set; } = 50;
        public int IntervalSimProcess { get; set; } = 50;
        public int IntervalSimMonitor { get; set; } = 5000;
        public int IntervalUnusedVariables { get; set; } = 60000;
        public int IntervalUnusedRessources { get; set; } = 60000;
        public int IntervalCheckScripts { get; set; } = 5000;
        public int IntervalCheckUiClose { get; set; } = 250;
        public int IntervalUiRefresh { get; set; } = 300;
        public Dictionary<SimulatorType, string[]> SimBinaries { get; set; } = new()
        {
            { SimulatorType.FSX, ["fsx", "fs9"] },
            { SimulatorType.P3D, ["Prepar3D"] },
            { SimulatorType.MSFS, ["FlightSimulator", "FlightSimulator2024"] },
            { SimulatorType.XP, ["X-Plane"] },
        };
        public string BinaryFSUIPC7 { get; set; } = "FSUIPC7";
        public bool UseFsuipcForMSFS { get; set; } = true;
        public int FsuipcRetryDelay { get; set; } = 30000;
        public int FsuipcStateCheckInterval { get; set; } = 1000;
        public int FsuipcConnectDelay { get; set; } = 1000;
        public int FsuipcScriptFlagDelay { get; set; } = 10;
        [JsonIgnore]
        public IPAddress ParsedXPlaneIP { get { return IPAddress.Parse(XPlaneIP); } }
        public string XPlaneIP { get; set; } = "127.0.0.1";
        public int XPlanePort { get; set; } = 49000;
        public int XPlaneRemoteCheckTimeout { get; set; } = 1500;
        public int XPlaneRetryDelay { get; set; } = 15000;
        public int XPlaneStateCheckInterval { get; set; } = 1000;
        public int XPlaneTimeoutReceive { get; set; } = 3000;
        public bool XPlaneUseLiveryRefOn12 { get; set; } = false;
        public bool XPlaneUseWebApi { get; set; } = true;
        public string XPlaneWebApiHost { get; set; } = "127.0.0.1:8086";
        public int XPlaneWebApiKeepAlive { get; set; } = 10000;
        public int MsfsRetryDelay { get; set; } = 30000;
        public int MsfsStaleTimeout { get; set; } = 15000;
        public int MsfsStateCheckInterval { get; set; } = 500;
        public int MobiRetryDelay { get; set; } = 10000;
        public int MobiVarsPerFrame { get; set; } = 0;
        public int MobiReorderTreshold { get; set; } = 10;
        public int CommandDelay { get; set; } = 25;
        public int InterActionDelay { get; set; } = 15;
        public int TickDelay { get; set; } = 15;
        public int VariableResetDelay { get; set; } = 100;
        public int VJoyMinimumPressed { get; set; } = 75;
        public int LongPressTime { get; set; } = 500;
        public int ApiPortNumber { get; set; } = 42042;


        public static AppConfiguration LoadConfiguration()
        {
            if (!File.Exists(ConfigFile))
                File.WriteAllText(ConfigFile, "{}", Encoding.UTF8);

            AppConfiguration config = JsonSerializer.Deserialize<AppConfiguration>(File.ReadAllText(ConfigFile), JsonOptions.JsonWriteOptions) ?? throw new NullReferenceException("The Plugin Configuration is null!");

            if (config.ConfigVersion < BuildConfigVersion)
            {
                Logger.Information($"Migrating Configuration from Version '{config.ConfigVersion}' to '{BuildConfigVersion}'");

                if (config.ConfigVersion == 4)
                {
                    Logger.Information($"Setting Mobi Variables");
                    config.MobiVarsPerFrame = 100;
                    config.MobiReorderTreshold = 10;
                    config.MobiRetryDelay = 10000;
                }

                if (config.ConfigVersion == 5)
                {
                    Logger.Information($"Setting Mobi Retry Delay");
                    config.MobiRetryDelay = 10000;
                }

                if (config.ConfigVersion < 9)
                {
                    Logger.Information($"Setting Sim Binaries");
                    config.SimBinaries = new Dictionary<SimulatorType, string[]>()
                        {
                            { SimulatorType.FSX, ["fsx", "fs9"] },
                            { SimulatorType.P3D, ["Prepar3D"] },
                            { SimulatorType.MSFS, ["FlightSimulator", "FlightSimulator2024"] },
                            { SimulatorType.XP, ["X-Plane"] },
                        };
                }

                if (config.ConfigVersion < 11)
                {
                    Logger.Information($"Setting RenderDpi");
                    config.RenderDpi = 96.0f;
                }

                if (config.ConfigVersion < 12)
                {
                    Logger.Information($"Setting LogFile");
                    config.LogFile = "PilotsDeck.log";
                    Logger.Information($"Setting MsfsStateCheckInterval");
                    config.MsfsStateCheckInterval = 500;
                }

                if (config.ConfigVersion < 13)
                {
                    Logger.Information($"Setting Mobi Variables");
                    config.MobiVarsPerFrame = 0;
                }

                config.ConfigVersion = BuildConfigVersion;
                SaveConfiguration();
            }
            else if (config.ConfigVersion > BuildConfigVersion)
            {
                Logger.Warning($"Existing Configuration Version '{config.ConfigVersion}' is higher then the Build Version!");
            }

            using Bitmap image = new(144, 144);
            config.SCALE_SESSION = (image.VerticalResolution / config.RenderDpi) * config.SCALE_LEGACY;

            return config;
        }

        public static float FontSizeConversionModern(float oldSize)
        {
            return MathF.Round(oldSize * App.Configuration.SCALE_SESSION, 0);
        }

        public static float FontSizeConversionLegacy(float oldSize)
        {
            return MathF.Round(oldSize * (96.0f / App.Configuration.RenderDpi), 0);
        }

        public static void SaveConfiguration(AppConfiguration config = null, string configFile = null)
        {
            config ??= App.Configuration;
            configFile ??= ConfigFile;
            File.WriteAllText(configFile, JsonSerializer.Serialize(config, JsonOptions.JsonWriteOptions));
        }
    }
}
