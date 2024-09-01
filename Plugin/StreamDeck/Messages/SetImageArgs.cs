using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SetImageArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event { get => "setImage"; }

        public Payload payload { get; set; }
        public class Payload
        {
            public string image { get; set; }

            public TargetType target { get; set; } = TargetType.HardwareAndSoftware;
        }
    }
}
