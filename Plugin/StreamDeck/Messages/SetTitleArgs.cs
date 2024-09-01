using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SetTitleArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "setTitle";
        public Payload payload { get; set; }
        public class Payload
        {
            public string title { get; set; }
            public TargetType target { get; set; } = TargetType.HardwareAndSoftware;

        }
    }
}
