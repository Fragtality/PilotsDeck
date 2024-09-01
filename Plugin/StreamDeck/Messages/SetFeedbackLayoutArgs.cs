using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    internal class SetFeedbackLayoutArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event { get => "setFeedbackLayout"; }

        public Payload payload { get; set; }
        public class Payload
        {
            public string layout { get; set; }
        }
    }
}
