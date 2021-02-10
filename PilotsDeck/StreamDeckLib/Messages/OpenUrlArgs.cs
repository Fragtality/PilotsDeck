namespace StreamDeckLib.Messages
{
	public class OpenUrlArgs : BaseStreamDeckArgs
	{
		public override string Event => "openUrl";
		public Payload payload { get; set; }
		public class Payload
		{
			public string url { get; set; }
		}
	}
}
