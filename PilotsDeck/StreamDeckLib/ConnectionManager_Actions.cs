using Microsoft.Extensions.Logging;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StreamDeckLib
{
	partial class ConnectionManager
	{
		private ActionManager _ActionManager;

		public ConnectionManager RegisterActionType(string actionUuid, Type actionType)
		{
			this._ActionManager.RegisterActionType(actionUuid, actionType);
			return this;
		}

		public BaseStreamDeckAction GetInstanceOfAction(string context, string actionUuid)
		{
			return this._ActionManager.GetActionForContext(this, context, actionUuid);
		}

		public ConnectionManager RegisterAllActions(Assembly assembly)
		{
			_logger?.LogInformation("ConnectionManager:RegisterAllActions: Started");
			_ActionManager.RegisterAllActions(assembly);
			_logger?.LogInformation("ConnectionManager:RegisterAllActions: Finished");
			return this;
		}

		public void BroadcastMessage(StreamDeckEventPayload msg)
		{
			var actions = GetAllActions();
			foreach (var entry in actions)
			{
				_EventDictionary[msg.Event]?.Invoke(entry.Value, msg);
			}
		}

		public Dictionary<string, BaseStreamDeckAction> GetAllActions()
		{
			return this._ActionManager.GetAllActions();
		}

	}
}
