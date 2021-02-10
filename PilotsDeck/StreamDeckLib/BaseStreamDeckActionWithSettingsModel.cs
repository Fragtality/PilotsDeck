using StreamDeckLib.Messages;
using System;
using System.Threading.Tasks;

namespace StreamDeckLib
{
  public abstract class BaseStreamDeckActionWithSettingsModel<T> : BaseStreamDeckAction
  {
	public T SettingsModel { get; } = Activator.CreateInstance<T>();

	public override Task OnDidReceiveSettings(StreamDeckEventPayload args)
	{
	  SetModelProperties(args);
	  return Task.CompletedTask;
	}

	public override Task OnWillAppear(StreamDeckEventPayload args)
	{
	  SetModelProperties(args);
	  return Task.CompletedTask;
	}

	protected void SetModelProperties(StreamDeckEventPayload args)
	{
	  var properties = typeof(T).GetProperties();
	  foreach (var prop in properties)
	  {
		if (args.payload != null && args.payload.settings != null && args.payload.settings.settingsModel != null)
		{
		  if (args.PayloadSettingsHasProperty(prop.Name))
		  {
			var value = args.GetPayloadSettingsValue(prop.Name);
			var value2 = Convert.ChangeType(value, prop.PropertyType);
			prop.SetValue(SettingsModel, value2);
		  }
		}
	  }
	}

  }
}
