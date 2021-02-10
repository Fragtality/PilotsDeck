using Newtonsoft.Json;
using static StreamDeckLib.Messages.SetTitleArgs;

namespace StreamDeckLib.Messages
{
	public class SetImageArgs : BaseStreamDeckArgs
	{
		public override string Event { get => "setImage"; }

		public Payload payload { get; set; }
		public class Payload
		{

			public string image { get; set; }

			public int target { get { return (int)TargetType; } }

			[JsonIgnore]
			public TargetType TargetType { get; set; }

		}

	}
}
