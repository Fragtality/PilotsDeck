using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotsDeck
{
    public class ProfileMapping
    {
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


        public override string ToString()
        {
            return $"Mapping: Name {DeckName} | DeckID {DeckId} | Path {ProfileName}";
        }

        public static readonly string STREAMDECK_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Elgato\StreamDeck";
        public static readonly string PLUGIN_MAPPING_FILE = "ProfileMappings.json";
        public static readonly string PLUGIN_UUID = "com.extension.pilotsdeck";
        public static readonly string PLUGIN_PATH = $@"{STREAMDECK_PATH}\Plugins\{PLUGIN_UUID}.sdPlugin";
        public static readonly string PLUGIN_PROFILE_FOLDER = "Profiles";
        public static readonly string PLUGIN_PROFILE_PATH = $@"{PLUGIN_PATH}\{PLUGIN_PROFILE_FOLDER}";

        public static List<ProfileMapping> LoadProfileMappings()
        {
            List<ProfileMapping> ProfileMappings = [];

            string path = $@"{PLUGIN_PROFILE_PATH}\{PLUGIN_MAPPING_FILE}";
            if (!File.Exists(path))
            {
                Logger.Log(LogLevel.Information, "ProfileMapping:LoadProfileMappings", $"Profile Mapping File '{PLUGIN_PROFILE_FOLDER}\\{PLUGIN_MAPPING_FILE}' does not exist! ({path})");
                return ProfileMappings;
            }
            if ((new FileInfo(path)).Length <= 0)
            {
                Logger.Log(LogLevel.Warning, "ProfileMapping:LoadProfileMappings", $"Profile Mapping File '{PLUGIN_PROFILE_FOLDER}\\{PLUGIN_MAPPING_FILE}' is empty - delete");
                File.Delete(path);
                return ProfileMappings;
            }

            ProfileMappings = JsonSerializer.Deserialize<List<ProfileMapping>>(File.ReadAllText(path));
            Logger.Log(LogLevel.Information, "ProfileMapping:LoadProfileMappings", $"ProfileMappings loaded (Count {ProfileMappings.Count})");

            return ProfileMappings;
        }
    }
}
