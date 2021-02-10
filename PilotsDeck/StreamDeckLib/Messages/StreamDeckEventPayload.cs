using Newtonsoft.Json;

namespace StreamDeckLib.Messages
{
	public class StreamDeckEventPayload
	{
		public string action { get; set; }

		[JsonProperty(PropertyName = "event")]
		public string Event { get; set; }
		public string context { get; set; }
		public string device { get; set; }

		public Deviceinfo deviceInfo { get; set; }

		public Payload payload { get; set; }

		public class Payload
		{
			public dynamic settings { get; set; }
			public Coordinates coordinates { get; set; }
			public int state { get; set; }
			public int userDesiredState { get; set; }
			public bool isInMultiAction { get; set; }
			public string title { get; set; }
			public TitleParameters titleParameters { get; set; }
			public string application { get; set; }
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
			public int type { get; set; }
			public Size size { get; set; }
		}

	}

}
