using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class GetSettingsArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "getSettings";
    }
}
