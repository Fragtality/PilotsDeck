using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StreamDeckLib
{
  partial class ConnectionManager
  {

	// Cheer ramblinggeek 100 January 21, 2019
	private static readonly Dictionary<string, Func<BaseStreamDeckAction, StreamDeckEventPayload, Task>> _EventDictionary
	= new Dictionary<string, Func<BaseStreamDeckAction, StreamDeckEventPayload, Task>>()
	{
	  ["keyDown"] = (plugin, args) => plugin.OnKeyDown(args),
	  ["keyUp"] = (plugin, args) => plugin.OnKeyUp(args),
	  ["willAppear"] = (plugin, args) => plugin.OnWillAppear(args),
	  ["willDisappear"] = (plugin, args) => plugin.OnWillDisappear(args),
	  ["titleParametersDidChange"] = (plugin, args) => plugin.OnTitleParametersDidChange(args),
	  ["deviceDidConnect"] = (plugin, args) => plugin.OnDeviceDidConnect(args),
	  ["deviceDidDisconnect"] = (plugin, args) => plugin.OnDeviceDidDisconnect(args),
	  ["applicationDidLaunch"] = (plugin, args) => plugin.OnApplicationDidLaunchAsync(args),
	  ["applicationDidTerminate"] = (plugin, args) => plugin.OnApplicationDidTerminateAsync(args),
	  ["didReceiveSettings"] = (plugin, args) => plugin.OnDidReceiveSettings(args),
	  ["didReceiveGlobalSettings"] = (plugin, args) => plugin.OnDidReceiveGlobalSettings(args),
	  ["propertyInspectorDidDisappear"] = (plugin, args) => plugin.OnPropertyInspectorDidDisappear(args),
	  ["propertyInspectorDidAppear"] = (plugin, args) => plugin.OnPropertyInspectorDidAppear(args),
	  ["sendToPlugin"] = (plugin, args) => plugin.OnSendToPlugin(args)
	};
  }

}
