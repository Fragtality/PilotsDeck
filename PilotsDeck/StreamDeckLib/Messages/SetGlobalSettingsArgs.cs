namespace StreamDeckLib.Messages
{
	public class SetGlobalSettingsArgs : BaseStreamDeckArgs
	{
		public override string Event => "setGlobalSettings";
		public dynamic payload { get; set; }
	}
}
