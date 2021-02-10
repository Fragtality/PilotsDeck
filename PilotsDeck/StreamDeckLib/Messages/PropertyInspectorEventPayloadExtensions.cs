using System;
using System.ComponentModel;

namespace StreamDeckLib.Messages
{
	public static class PropertyInspectorEventPayloadExtensions
  {
	public static bool SettingsPayloadHasProperty(this PropertyInspectorEventPayload obj, string propertyName)
	{
	  return obj.payload.settingsModel[propertyName] != null;
	}

	public static T GetSettingsPayloadValue<T>(this PropertyInspectorEventPayload obj, string propertyName) where T: IConvertible
	{
	  if (obj.SettingsPayloadHasProperty(propertyName))
	  {
		return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.payload.settingsModel[propertyName].Value);
	  }
	  return default(T);
	}

	public static bool EventPayloadHasProperty(this PropertyInspectorEventPayload obj, string propertyName)
	{
	  return obj.payload[propertyName] != null;
	}

	public static T GetEventPayloadValue<T>(this PropertyInspectorEventPayload obj, string propertyName) where T : IConvertible
	{
	  if (obj.EventPayloadHasProperty(propertyName))
	  {
		return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.payload[propertyName].Value);
	  }
	  return default(T);
	}
  }
}
