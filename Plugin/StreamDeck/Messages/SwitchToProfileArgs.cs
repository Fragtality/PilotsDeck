using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SwitchToProfileArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "switchToProfile";

        public string device { get; set; }

        public Payload payload { get; set; }

        public class Payload
        {
            public string profile { get; set; }
        }
    }
}
