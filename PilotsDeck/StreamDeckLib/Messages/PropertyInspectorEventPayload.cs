using Newtonsoft.Json;

namespace StreamDeckLib.Messages
{
	public class PropertyInspectorEventPayload
	{
		public string action { get; set; }
		public string context { get; set; }
		[JsonProperty(PropertyName = "event")]
		public string Event { get; set; }
		public dynamic payload { get; set; }
	}
}
