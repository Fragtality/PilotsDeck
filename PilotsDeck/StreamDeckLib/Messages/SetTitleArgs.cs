using Newtonsoft.Json;

namespace StreamDeckLib.Messages
{
	public class SetTitleArgs : BaseStreamDeckArgs
	{
		public override string Event => "setTitle";
		public Payload payload { get; set; }
		public class Payload
		{
			public string title { get; set; }
			public int target { get { return (int)TargetType; } }
			[JsonIgnore]
			public TargetType TargetType { get; set; }
		}

		public enum TargetType
		{
			HardwareAndSoftware = 0,
			Hardware = 1,
			Software = 2
		}
	}
}
