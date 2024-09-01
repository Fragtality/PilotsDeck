using System.Collections.Generic;

namespace PilotsDeck.StreamDeck.Messages
{
    public class StreamDeckInfoMessage
    {
        public Application application { get; set; }
        public Plugin plugin { get; set; }
        public int devicePixelRatio { get; set; }
        public Colors colors { get; set; }
        public List<Device> devices { get; set; }


        public class Application
        {
            public string font { get; set; }
            public string language { get; set; }
            public string platform { get; set; }
            public string platformVersion { get; set; }
            public string version { get; set; }
        }

        public class Plugin
        {
            public string uuid { get; set; }
            public string version { get; set; }
        }

        public class Colors
        {
            public string buttonPressedBackgroundColor { get; set; }
            public string buttonPressedBorderColor { get; set; }
            public string buttonPressedTextColor { get; set; }
            public string disabledColor { get; set; }
            public string highlightColor { get; set; }
            public string mouseDownColor { get; set; }
        }

        public class Device
        {
            public string id { get; set; }
            public string name { get; set; }
            public Size size { get; set; }
            public StreamDeckType type { get; set; }
        }
    }
}
