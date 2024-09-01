using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class PropertyInspectorEventPayload
    {
        public string action { get; set; }
        public string context { get; set; }
        [JsonPropertyName("event")]
        public string Event { get; set; }
        public dynamic payload { get; set; }
    }
}
