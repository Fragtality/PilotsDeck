using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ProfileManager.json
{
    public class DeviceInfo
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("size")]
        public JsonNode Size { get; set; }

        [JsonPropertyName("type")]
        public StreamDeckTypeEnum Type { get; set; }
    }
}
