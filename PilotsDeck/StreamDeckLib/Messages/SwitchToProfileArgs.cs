using Newtonsoft.Json;

namespace StreamDeckLib.Messages
{
	public class SwitchToProfileArgs : BaseStreamDeckArgs
	{
		public override string Event => "switchToProfile";

		public string device { get; set; }

		public Payload payload { get; set; }

		public class Payload
		{
			public string profile { get; set; }
		}

	}
}
