using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class PluginRegistration
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }
        public string uuid { get; set; }
    }
}
