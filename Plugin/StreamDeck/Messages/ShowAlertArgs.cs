using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class ShowAlertArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "showAlert";
    }
}
