using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProfileManager
{
    public class ProfileMapping
    {
        [JsonIgnore]
        public static readonly List<KeyValuePair<int, string>> SimulatorSelections = [
            new KeyValuePair<int, string>((int)SimulatorType.UNKNOWN + 1, "NOT SET"),
            new KeyValuePair<int, string>((int)SimulatorType.FSX + 1, "FlightSimulator X"),
            new KeyValuePair<int, string>((int)SimulatorType.P3D + 1, "Prepar3D 4/5"),
            new KeyValuePair<int, string>((int)SimulatorType.MSFS + 1, "FlightSimulator 2020"),
            new KeyValuePair<int, string>((int)SimulatorType.XP + 1, "X-Plane 11/12")
        ];

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string DeckId { get; set; } = "";

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string DeckName { get; set; } = "";
        
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public StreamDeckTypeEnum DeckType { get; set; } = StreamDeckTypeEnum.UNKNOWN;

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string ProfileName { get; set; } = "";

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string ProfileUUID { get; set; } = "";

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string ProfilePath { get; set; } = "";

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public bool DefaultProfile { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public SimulatorType DefaultSimulator { get; set; } = SimulatorType.UNKNOWN;

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public bool AircraftProfile { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public List<string> AircraftStrings { get; set; } = [];

        [JsonIgnore]
        public bool IsChanged { get; set; } = false;

        [JsonIgnore]
        public bool DeleteFlag { get; set; } = false;

        [JsonIgnore]
        public bool IsProfileNever { get { return !DefaultProfile && !AircraftProfile; } }

        [JsonIgnore]
        public bool HasManifest { get { return ProfileManifest != null; } }

        [JsonIgnore]
        public ProfileManifest ProfileManifest { get; set; } = null;


        public override string ToString()
        {
            return $"Mapping: Name {DeckName} | Manifest {HasManifest} | DeckID {DeckId} | Path {ProfileName}";
        }

        public void SetCheckPath()
        {
            string path = $"/{Parameters.PLUGIN_PROFILE_FOLDER}/{DeckId}/{ProfileManifest.ProfileDirectoryCleaned}";
            if (ProfileManifest.PreconfiguredName != path)
            {
                ProfileManifest.PreconfiguredName = path;
                ProfileManifest.IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected PreconfiguredName @ {ProfileManifest}");
            }
            if (ProfilePath != path)
            {
                ProfilePath = path;
                IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected ProfilePath  @ {this}");
            }
            if (ProfileManifest.InstalledByPluginUUID != Parameters.PLUGIN_UUID)
            {
                ProfileManifest.InstalledByPluginUUID = Parameters.PLUGIN_UUID;
                ProfileManifest.IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected PluginUUID  @ {ProfileManifest}");
            }
        }

        public void SetCheckManifest(ProfileManifest manifest)
        {
            ProfileManifest = manifest;
            manifest.ProfileMapping = this;

            if (DeckId != ProfileManifest.Device.Hash)
            {
                DeckId = ProfileManifest.Device.Hash;
                IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected DeckId @ {this}");
            }

            if (!string.IsNullOrEmpty(ProfileManifest.Device.DeckName) && DeckName != ProfileManifest.Device.DeckName)
            {
                DeckName = ProfileManifest.Device.DeckName;
                IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected DeckName @ {this}");
            }

            if (ProfileManifest.Device.DeckType != StreamDeckTypeEnum.UNKNOWN && DeckType != ProfileManifest.Device.DeckType)
            {
                DeckType = ProfileManifest.Device.DeckType;
                IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected DeckType @ {this}");
            }

            if (ProfileName != ProfileManifest.ProfileName)
            {
                ProfileName = ProfileManifest.ProfileName;
                IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected ProfileName @ {this}");
            }

            if (ProfileUUID != ProfileManifest.ProfileDirectoryCleaned)
            {
                ProfileUUID = ProfileManifest.ProfileDirectoryCleaned;
                IsChanged = true;
                Logger.Log(LogLevel.Information, $"Corrected ProfileUUID @ {this}");
            }

            SetCheckPath();

            Logger.Log(LogLevel.Debug, $"Manifest was set @ {this}");
        }
    }
}
