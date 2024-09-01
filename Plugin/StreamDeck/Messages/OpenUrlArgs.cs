using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class OpenUrlArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "openUrl";
        public Payload payload { get; set; }
        public class Payload
        {
            public string url { get; set; }
        }
    }
}
