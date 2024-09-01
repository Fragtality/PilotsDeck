using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class LogMessageArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "logMessage";
        public Payload payload { get; set; }
        public class Payload
        {
            public string message { get; set; }
        }
    }
}
