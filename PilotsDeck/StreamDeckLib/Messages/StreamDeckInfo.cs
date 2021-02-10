namespace StreamDeckLib.Messages
{
	public class Info
	{
		public Application application { get; set; }
		public Device[] devices { get; set; }
		public int devicePixelRatio { get; set; }

		public class Application
		{
			public string language { get; set; }
			public string platform { get; set; }
			public string version { get; set; }
		}

		public class Device
		{
			public string id { get; set; }
			public Size size { get; set; }
			public int type { get; set; }
		}

		public class PluginRegistration
		{
			public string @event { get; set; }
			public string uuid { get; set; }
		}
	}
}
