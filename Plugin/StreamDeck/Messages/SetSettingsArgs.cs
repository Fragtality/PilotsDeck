using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SetSettingsArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "setSettings";
        public JsonNode payload { get; set; }
    }

}
