using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public abstract class BaseStreamDeckArgs
    {
        [JsonPropertyName("event")]
        public abstract string Event
        {
            get;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string context { get; set; }
    }

    public enum TargetType
    {
        HardwareAndSoftware = 0,
        Hardware = 1,
        Software = 2
    }
}
