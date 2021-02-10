using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace StreamDeckLib.Messages
{
  public static class StreamDeckEventPayloadExtensions
  {
	public static bool PayloadSettingsHasProperty(this StreamDeckEventPayload obj, string propertyName)
	{
	  var jObj = obj.payload.settings as JObject;

	  return jObj.Count > 0 && obj.payload.settings.settingsModel[propertyName] != null;
	}


	public static T GetPayloadSettingsValue<T>(this StreamDeckEventPayload obj, string propertyName) where T : IConvertible
	{
	  if (obj.PayloadSettingsHasProperty(propertyName))
	  {
		return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.payload.settings.settingsModel[propertyName].Value);
	  }
	  return default(T);
	}

	public static object GetPayloadSettingsValue(this StreamDeckEventPayload obj, string propertyName)
	{
	  if (obj.PayloadSettingsHasProperty(propertyName))
	  {
		return obj.payload.settings.settingsModel[propertyName].Value;
	  }
	  return null;
	}

  }
}
