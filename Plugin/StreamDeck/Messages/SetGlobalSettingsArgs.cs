using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class SetGlobalSettingsArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event => "setGlobalSettings";
        public dynamic payload { get; set; }
    }
}
