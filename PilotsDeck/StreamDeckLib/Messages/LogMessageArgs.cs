namespace StreamDeckLib.Messages
{
  public class LogMessageArgs : BaseStreamDeckArgs
  {
	public override string Event => "logMessage";
	public Payload payload { get; set; }
	public class Payload
	{
	  public string message { get; set; }
	}
  }
}
