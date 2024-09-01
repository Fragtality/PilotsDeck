using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SetStateArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "setState";
        public Payload payload { get; set; }
        public class Payload
        {
            public int state { get; set; }
        }
    }
}
