using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class GetGlobalSettingsArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "getGlobalSettings";
    }
}
