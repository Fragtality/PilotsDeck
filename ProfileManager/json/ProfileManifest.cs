using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ProfileManager.json
{
    public class ProfileManifest
    {
        [JsonIgnore]
        public string ProfileDirectory { get; set; }

        [JsonIgnore]
        public string ProfileDirectoryCleaned { get { return ProfileDirectory.Replace("-", "").Replace(".sdProfile", "", StringComparison.InvariantCultureIgnoreCase); } }

        [JsonIgnore]
        public ProfileController ProfileController { get; protected set; }

        public class ManifestDevice
        {
            public string Model { get; set; }
            public string UUID { get; set; }

            [JsonIgnore]
            public string DeckName { get; set; }

            [JsonIgnore]
            public StreamDeckTypeEnum DeckType { get; set; } = StreamDeckTypeEnum.UNKNOWN;

            [JsonIgnore]
            public string Hash { get; set; }
        }

        public ManifestDevice Device { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string InstalledByPluginUUID { get; set; } = null;

        [JsonPropertyName("Name")]
        public string ProfileName { get; set; }

        public JsonNode Pages { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PreconfiguredName { get; set; } = null;

        public string Version { get; set; }

        [JsonIgnore]
        public bool IsPreparedForSwitching { get { return InstalledByPluginUUID != null && PreconfiguredName != null && HasMapping; } }

        [JsonIgnore]
        public bool IsChanged { get; set; } = false;

        [JsonIgnore]
        public bool DeleteFlag { get; set; } = false;

        [JsonIgnore]
        public bool HasMapping { get { return ProfileMapping != null; } }

        [JsonIgnore]
        public ProfileMapping ProfileMapping { get; set; } = null;

        public override string ToString()
        {
            return $"Manifest: Name {ProfileName} | IsPrepared {IsPreparedForSwitching} | Directory {ProfileDirectory} | DeckID {Device.Hash}";
        }

        public void SetDeviceInfo(DeviceInfo deviceInfo)
        {
            Device.DeckName = deviceInfo.Name;
            Device.DeckType = deviceInfo.Type;
            Logger.Log(LogLevel.Verbose, $"DeviceInfo was set @ {this}");
        }

        public static ProfileManifest LoadManifest(string json)
        {
            var manifest = JsonSerializer.Deserialize<ProfileManifest>(json);
            if (manifest?.Device != null)
                manifest.Device.Hash = Tools.CreateMD5(manifest.Device.UUID);
            return manifest;
        }

        public static ProfileManifest LoadManifest(string path, string folder, ProfileController controller)
        {
            var manifest = LoadManifest(File.ReadAllText(path));

            manifest.ProfileDirectory = folder;
            manifest.Device.Hash = Tools.CreateMD5(manifest.Device.UUID);
            manifest.ProfileController = controller;
            Logger.Log(LogLevel.Verbose, $"Manifest was loaded: {manifest}");

            return manifest;
        }

        public static void WriteManifest(string path, ProfileManifest manifest)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(manifest));
            manifest.IsChanged = false;
            Logger.Log(LogLevel.Debug, $"Manifest was saved: {manifest}");
        }
    }
}
