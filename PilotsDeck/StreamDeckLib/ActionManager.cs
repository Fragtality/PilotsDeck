using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace StreamDeckLib
{
	public partial class ActionManager : IDisposable
	{
		#region Type and instance properties

		// We will use a string equality comparer for our dictionaries which ignores case and is for invariant culture.
		private static readonly IEqualityComparer<string> _StringEqualityComparer = StringComparer.Create(CultureInfo.InvariantCulture, true);

		//string is the action UUID, type is the class type of the action
		private readonly Dictionary<string, Type> _Actions;

		//string is the context, there will only ever be one action per context
		private readonly Dictionary<string, BaseStreamDeckAction> _ActionInstances;

		// The logger we will receive when being instantiated. Defaults to a NullLogger is none is specified.
		private readonly ILogger<ActionManager> _Logger;

		private readonly IServiceProvider _serviceProvider;

		#endregion


		#region Constructors

		private ActionManager()
		{
			this._Actions = new Dictionary<string, Type>(_StringEqualityComparer);
			this._ActionInstances = new Dictionary<string, BaseStreamDeckAction>(_StringEqualityComparer);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StreamDeckLib.ActionManager"/> class.
		/// </summary>
		/// <param name="logger">An instance of a logger (<see cref="ILogger"/> class.</param>
		public ActionManager(ILogger<ActionManager> logger = null, IServiceProvider serviceProvider = null) : this()
		{
			this._Logger = logger ?? NullLoggerFactory.Instance.CreateLogger<ActionManager>();
			this._serviceProvider = serviceProvider;
		}

		#endregion


		#region Action registration methods


		/// <summary>
		/// Registers an action type for a given UUID.
		/// </summary>
		/// <returns>The instance of <seealso cref="ActionManager"/>.</returns>
		/// <param name="actionUUID">The UUID for the action.</param>
		/// <typeparam name="TActionType">The type to be registered for the <paramref name="actionUUID"/>. Must inherit from <seealso cref="BaseStreamDeckAction"/>.</typeparam>
		public ActionManager RegisterAction<TActionType>(string actionUUID)
			where TActionType : BaseStreamDeckAction
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(RegisterAction)}(string)");

			this._Actions.Add(actionUUID, typeof(TActionType));
			return this;
		}


		/// <summary>
		/// Registers an action type for a given UUID.
		/// </summary>
		/// <returns>The instance of <seealso cref="ActionManager"/>.</returns>
		/// <param name="actionUUID">The UUID for the action.</param>
		/// <param name="actionType">The type to be registered for the <paramref name="actionUUID"/>. Must inherit from <seealso cref="BaseStreamDeckAction"/>.</param>
		/// <remarks>If the <paramref name="actionType" /> does not inherit from <seealso cref="BaseStreamDeckAction"/>,
		/// a <seealso cref="TypeDoesNotInheritFromBaseStreamDeckAction"/> exception is thrown.</remarks>
		public ActionManager RegisterActionType(string actionUuid, Type actionType)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(RegisterActionType)}(string, Type)");

			// Check that we've got a UUID to use for registration.
			if (string.IsNullOrWhiteSpace(actionUuid))
			{
				throw new IncompleteActionDefinitionException($"The UUID for the \"{actionType.Name}\" action was not specified.");
			}

			if (_Actions.ContainsKey(actionUuid))
			{
				throw new DuplicateActionRegistrationException(actionUuid);
			}

			// Ensure that the type we're registering inherits from BaseStreamDeckAction. 
			if (!actionType.IsSubclassOf(typeof(BaseStreamDeckAction)))
			{
				throw new TypeDoesNotInheritFromBaseStreamDeckAction(actionType.Name, actionType.FullName, actionType.Assembly.FullName);
			}

			this._Actions.Add(actionUuid, actionType);
			return this;
		}


		/// <summary>
		/// Registers all actions which are decorated with an <seealso cref="ActionUuidAttribute"/>
		/// withn a given <seealso cref="Assembly"/>.
		/// </summary>
		/// <returns>The instance of <seealso cref="ActionManager"/>.</returns>
		/// <param name="actionsAssembly">The <seealso cref="Assembly"/> from within which actions are to be registered.</param>
		public ActionManager RegisterAllActions(Assembly actionsAssembly)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(RegisterAllActions)}(assembly)");

			var actions = actionsAssembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(ActionUuidAttribute)));
			foreach (var actionType in actions)
			{
				var attr = actionType.GetCustomAttributes(typeof(ActionUuidAttribute), true).FirstOrDefault() as ActionUuidAttribute;
				this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(RegisterAllActions)}({actionType}) {attr.Uuid}");
				this.RegisterActionType(attr.Uuid, actionType);
			}
			return this;
		}

		#endregion


		#region Action instance creation and retrieval

		/// <summary>
		/// Gets an instance of an action from an action UUID.
		/// </summary>
		/// <returns>A new instance of an action.</returns>
		/// <param name="actionUUID">The UUID of the action type which is requested.</param>
		/// <typeparam name="TActionType">The specific type of action to be returned.</typeparam>
		/// <remarks>If no type is found to match the <paramref name="actionUUID"/>, an exception
		/// of type <seealso cref="ActionNotRegisteredException"/> is thrown.</remarks>
		public TActionType GetActionInstance<TActionType>(ConnectionManager connectionManager, string actionUUID)
			where TActionType : BaseStreamDeckAction
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(GetActionInstance)}(string)");

			if (this._Actions.ContainsKey(actionUUID))
			{
				var instance = ActivatorUtilities.CreateInstance(this._serviceProvider, this._Actions[actionUUID]) as TActionType;
				instance.Logger = _Logger;
				instance.Manager = connectionManager;
				return instance;
			}

			throw new ActionNotRegisteredException(actionUUID);
		}


		/// <summary>
		/// Checks whether an action is registered for a given UUID.
		/// </summary>
		/// <returns><c>true</c>, if action registered was registered, <c>false</c> otherwise.</returns>
		/// <param name="actionUUID">The UUID to check</param>
		public bool IsActionRegistered(string actionUUID)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(IsActionRegistered)}(string)");

			return this._Actions.ContainsKey(actionUUID);
		}


		/// <summary>
		/// Gets a new instance of an <seealso cref="BaseStreamDeckAction"/> registered with a UUID
		/// of <paramref name="actionUUID"/>. This instance <strong>will not</strong> be stored or managed,
		/// and cannot be reused.
		/// </summary>
		/// <returns>A new instance of the action</returns>
		/// <param name="actionUUID">The UUID of the action type which is to be instantiated</param>
		/// <remarks>
		/// Throws a <exception cref="ActionNotRegisteredException" /> exception if there is no action with
		/// a UUID of <paramref name="actionUUID"/> registered.
		/// </remarks>
		public BaseStreamDeckAction GetAction(ConnectionManager connectionManager, string actionUUID)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(GetAction)}(string)");

			return this.CreateActionInstanceByUUID(connectionManager, actionUUID);
		}


		private BaseStreamDeckAction CreateActionInstanceByUUID(ConnectionManager connectionManager, string actionUuid, bool throwIfNotRegistered = true)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(CreateActionInstanceByUUID)}(string, bool)");
			if (this._Actions.ContainsKey(actionUuid))
			{
				var instance = ActivatorUtilities.CreateInstance(this._serviceProvider, this._Actions[actionUuid]) as BaseStreamDeckAction;
				instance.Logger = _Logger;
				instance.Manager = connectionManager;
				return instance;

			}

			if (throwIfNotRegistered)
			{
				throw new ActionNotRegisteredException(actionUuid);
			}

			return null;
		}

		#endregion


		#region Specific action instance creation, registration, and retrieval (by Stream Deck context)


		/// <summary>
		/// Registers the action instance.
		/// </summary>
		/// <returns>The action instance.</returns>
		/// <param name="instanceKey">The key to be used when registering the action instance.
		/// This is typically the value of the <seealso cref="StreamDeckLib.Messages.StreamDeckEventPayload.context"/>.</param>
		/// <param name="actionInstance">Action instance.</param>
		public ActionManager RegisterActionInstance(string instanceKey, BaseStreamDeckAction actionInstance)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(RegisterAction)}(string, BaseStreamDeckAction)");
			return this.RegisterActionInstanceInternal(instanceKey, actionInstance);
		}

		private ActionManager RegisterActionInstanceInternal(string instanceKey, BaseStreamDeckAction actionInstance, bool throwIfInstanceIsNull = true)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(RegisterActionInstanceInternal)}(string, BaseStreamDeckAction, bool)");

			if (null == actionInstance && (!throwIfInstanceIsNull))
			{
				this._Logger?.LogDebug($"The instance to register with a key of {instanceKey} was null, but prevent raising an exception was specified.");
				return this;
			}

			if (null == actionInstance && throwIfInstanceIsNull)
			{
				throw new ArgumentNullException($"Could not register an action instance with a key of \"{instanceKey}\"");
			}

			this._Logger?.LogDebug($"Registering an instance of {actionInstance.GetType().FullName} with a key of \"{instanceKey}\".");
			if (this._ActionInstances.ContainsKey(instanceKey))
			{
				throw new DuplicateActionInstanceRegistrationException(instanceKey);
			}

			this._ActionInstances.Add(instanceKey, actionInstance);

			return this;
		}


		/// <summary>
		/// Gets the instance of action.
		/// </summary>
		/// <returns>The instance of action.</returns>
		/// <param name="context">Context.</param>
		/// <param name="actionUuid">Action UUID.</param>
		internal BaseStreamDeckAction GetActionForContext(ConnectionManager connectionManager, string context, string actionUuid)
		{
			this._Logger?.LogTrace($"{nameof(ActionManager)}.{nameof(GetActionForContext)}(string, string)");

			//see if context exists, if so, return the associated action
			if (this._ActionInstances.ContainsKey(context))
			{
				return this._ActionInstances[context];
			}

			//see if we have a recorded type for the action
			var actionInstance = this.CreateActionInstanceByUUID(connectionManager, actionUuid, false);
			this.RegisterActionInstanceInternal(context, actionInstance, false);

			return actionInstance;
		}

		public Dictionary<string, BaseStreamDeckAction> GetAllActions()
		{
			return _ActionInstances;
		}


		#endregion


		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ActionManager() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion

	}
}
