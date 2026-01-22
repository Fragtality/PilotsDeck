using CFIT.AppLogger;
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

        // StreamDeck format: nested Device object
        public ManifestDevice Device { get; set; }

        // StreamDock format: flat properties
        [JsonPropertyName("DeviceModel")]
        public string DeviceModel { get; set; }

        [JsonPropertyName("DeviceUUID")]
        public string DeviceUUID { get; set; }

        // Helper property to get the actual device model (supports both formats)
        [JsonIgnore]
        public string ActualDeviceModel { get { return Device?.Model ?? DeviceModel; } }

        // Helper property to get the actual device UUID (supports both formats)
        [JsonIgnore]
        public string ActualDeviceUUID { get { return Device?.UUID ?? DeviceUUID; } }

        // Helper property to check if this is a StreamDock manifest
        [JsonIgnore]
        public bool IsStreamDockFormat { get { return !string.IsNullOrEmpty(DeviceModel) && Device == null; } }

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
            return Device != null
                ? $"Manifest: Name {ProfileName} | IsPrepared {IsPreparedForSwitching} | Directory {ProfileDirectory} | DeckID {Device.Hash}"
                : $"Manifest: Name {ProfileName} | IsPrepared {IsPreparedForSwitching} | Directory {ProfileDirectory} | DeckID N/A";
        }

        public void SetDeviceInfo(DeviceInfo deviceInfo)
        {
            Device.DeckName = deviceInfo.Name;
            Device.DeckType = deviceInfo.Type;
            Logger.Verbose($"DeviceInfo was set @ {this}");
        }

        public static ProfileManifest LoadManifest(string json)
        {
            var manifest = JsonSerializer.Deserialize<ProfileManifest>(json);
            if (manifest != null)
            {
                // For StreamDeck format (nested Device object)
                if (manifest.Device != null)
                {
                    manifest.Device.Hash = Tools.CreateMD5(manifest.Device.UUID);
                }
                // For StreamDock format (flat properties) - create Device for compatibility
                else
                {
                    string deviceUUID = manifest.DeviceUUID;
                    string deviceModel = manifest.DeviceModel;

                    if (!string.IsNullOrEmpty(deviceUUID))
                    {
                        // StreamDock format: map flat properties to Device object
                        manifest.Device = new ManifestDevice
                        {
                            Model = deviceModel,
                            UUID = deviceUUID,
                            Hash = Tools.CreateMD5(deviceUUID)
                        };
                        Logger.Verbose($"StreamDock format detected - Device object created from flat properties");
                    }
                    else
                    {
                        // Incomplete manifest data - create placeholder Device to prevent null reference
                        manifest.Device = new ManifestDevice
                        {
                            Model = "Unknown",
                            UUID = Guid.NewGuid().ToString(),
                            Hash = "UNKNOWN"
                        };
                        Logger.Warning($"Incomplete manifest '{manifest.ProfileName}' - missing Device info, using placeholder");
                    }
                }
            }
            return manifest;
        }

        public static ProfileManifest LoadManifest(string path, string folder, ProfileController controller)
        {
            var manifest = LoadManifest(File.ReadAllText(path));

            manifest.ProfileDirectory = folder;
            manifest.ProfileController = controller;
            Logger.Verbose($"Manifest was loaded: {manifest.ProfileName}");

            return manifest;
        }

        public static void WriteManifest(string path, ProfileManifest manifest)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(manifest));
            manifest.IsChanged = false;
            Logger.Debug($"Manifest was saved: {manifest}");
        }
    }
}
