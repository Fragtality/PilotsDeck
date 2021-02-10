namespace StreamDeckLib.Messages
{
  public class SendToPropertyInspectorArgs : BaseStreamDeckArgs
  {
	public override string Event => "sendToPropertyInspector";
	public dynamic payload { get; set; }
  }
}
