using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    internal class SetFeedbackItemArgs : BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public override string Event { get => "setFeedback"; }

        public Dictionary<string, string> payload { get; set; } = [];
    }
}
