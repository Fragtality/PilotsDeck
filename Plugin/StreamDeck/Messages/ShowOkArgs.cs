using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class ShowOkArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "showOk";
    }
}
