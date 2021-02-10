namespace StreamDeckLib.Messages
{
	public class SetStateArgs : BaseStreamDeckArgs
	{
		public override string Event => "setState";
		public Payload payload { get; set; }
		public class Payload
		{
			public int state { get; set; }
		}

	}

}
