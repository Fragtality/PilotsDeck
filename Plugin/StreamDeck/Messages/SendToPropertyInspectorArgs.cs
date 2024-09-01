using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SendToPropertyInspectorArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "sendToPropertyInspector";
        public dynamic payload { get; set; }
    }
}
