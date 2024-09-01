using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PilotsDeck.StreamDeck.Messages
{
    public class StreamDeckEvent
    {
        public string action { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; }
        public string context { get; set; }
        public string device { get; set; }

        public Deviceinfo deviceInfo { get; set; }

        public Payload payload { get; set; }

        public class Payload
        {
            public string controller { get; set; }
            public JsonNode settings { get; set; }
            public Coordinates coordinates { get; set; }
            public int[] tapPos { get; set; }
            public bool hold { get; set; }
            public int ticks { get; set; }
            public bool pressed { get; set; }
            public int state { get; set; }
            public int userDesiredState { get; set; }
            public bool isInMultiAction { get; set; }
            public string title { get; set; }
            public TitleParameters titleParameters { get; set; }
            public string application { get; set; }
            public string url { get; set; }
        }

        public class TitleParameters
        {
            public string fontFamily { get; set; }
            public int fontSize { get; set; }
            public string fontStyle { get; set; }
            public bool fontUnderline { get; set; }
            public bool showTitle { get; set; }
            public string titleAlignment { get; set; }
            public string titleColor { get; set; }
        }

        public class Deviceinfo
        {
            public string name { get; set; }
            public StreamDeckType type { get; set; }
            public Size size { get; set; }
        }
    }
}
