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
            public int page { get; set; } = 0;
            public string profile { get; set; }
        }
    }
}
