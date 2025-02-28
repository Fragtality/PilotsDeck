using CFIT.AppLogger;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProfileManager
{
    public class PackageManifest
    {
        [JsonPropertyName("packageformat")]
        public int PackageFormat { get; set; } = 1;

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("versionpackage")]
        public string VersionPackage { get; set; } = "";

        [JsonPropertyName("aircraft")]
        public string Aircraft { get; set; } = "";

        [JsonPropertyName("author")]
        public string Author { get; set; } = "";

        [JsonPropertyName("url")]
        public string URL { get; set; } = "";

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";     
        
        [JsonPropertyName("versionplugin")]
        public string VersionPlugin { get; set; } = "";

        public static PackageManifest LoadManifest(string json)
        {
            var manifest = JsonSerializer.Deserialize<PackageManifest>(json);
            Logger.Debug($"PackageManifest was loaded: {manifest?.Title}");
            return manifest;
        }
    }
}
