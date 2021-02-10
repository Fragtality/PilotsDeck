using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StreamDeckLib.Messages;
using StreamDeckLib.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using PilotsDeck;
using System.Configuration;
using System.Collections.Generic;

namespace StreamDeckLib
{

	/// <summary>
	/// This class manages the connection to the StreamDeck hardware
	/// </summary>
	public partial class ConnectionManager : IDisposable
	{
		private readonly int _port;
		private readonly string _uuid;
		private readonly string _registerEvent;
		private IStreamDeckProxy _proxy;
		private IGlobalSettings _globalSettings;
		public Info Info { get; private set; }

		//CHANGED
		public ActionController ActionController { get; private set; }
		private readonly int _pollInterval;
		//private Dictionary<string, string> _imageCache;

		private ConnectionManager()
		{
			this._ActionManager = new ActionManager(_LoggerFactory?.CreateLogger<ActionManager>());
			//CHANGED
			this.ActionController = new ActionController(this);
			this._pollInterval = AppSettings.pollInterval;
		}

		public ConnectionManager(IOptions<StreamDeckToolkitOptions> options, ActionManager actionManager, ILogger<ConnectionManager> logger, IStreamDeckProxy streamDeckProxy = null)
		{
			if (options == null) throw new ArgumentNullException("options", "Options cannot be null, these should be retrieved from commandline args.");
			if (actionManager == null) throw new ArgumentNullException("actionManager", "ActionManager cannot be null, these should be retrieved from commandline args.");

			_ActionManager = actionManager;
			_logger = logger;

			if (options.Value != null)
			{
				if (!string.IsNullOrEmpty(options.Value.Info))
				{
					var myInfo = JsonConvert.DeserializeObject<Info>(options.Value.Info);
					Info = myInfo;
				}
				_port = options.Value.Port;
				_uuid = options.Value.PluginUUID;
				_registerEvent = options.Value.RegisterEvent;
			}
			_proxy = streamDeckProxy ?? new StreamDeckProxy();
		}

		private ConnectionManager(StreamDeckToolkitOptions options, ILoggerFactory loggerFactory = null, IStreamDeckProxy streamDeckProxy = null) : this()
		{
			var myInfo = JsonConvert.DeserializeObject<Info>(options.Info);
			Info = myInfo;
			_port = options.Port;
			_uuid = options.PluginUUID;
			_registerEvent = options.RegisterEvent;
			_proxy = streamDeckProxy ?? new StreamDeckProxy();

			_LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
			_logger = loggerFactory?.CreateLogger<ConnectionManager>();
		}

		public static ConnectionManager Initialize(string[] commandLineArgs, ILoggerFactory loggerFactory = null, IStreamDeckProxy streamDeckProxy = null)
		{
			try
			{
				var options = ParseCommandlineArgs(commandLineArgs);
				return new ConnectionManager(options, loggerFactory, streamDeckProxy);
			}
			catch
			{
				throw new ArgumentException($"{nameof(commandLineArgs)} must be the commandline args that the StreamDeck application calls this program with.");
			}
		}

		public ConnectionManager RegisterGlobalSettings(IGlobalSettings settings)
		{
			_globalSettings = settings;
			return this;
		}

		public async Task<ConnectionManager> StartAsync(CancellationToken token)
		{

			TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

			await Run(token);

			return this;

		}

		public async Task<ConnectionManager> StartAsync()
		{
			var source = new CancellationTokenSource();
			return await this.StartAsync(source.Token);
		}

		private static StreamDeckToolkitOptions ParseCommandlineArgs(string[] args)
		{
			using (var app = new CommandLineApplication())
			{
				app.HelpOption();

				var optionPort = app.Option<int>("-port|--port <PORT>",
																				 "The port the Elgato StreamDeck software is listening on",
																				 CommandOptionType.SingleValue);

				var optionPluginUUID = app.Option<string>("-pluginUUID <UUID>",
																					"The UUID that the Elgato StreamDeck software knows this plugin as.",
																					CommandOptionType.SingleValue);

				var optionRegisterEvent = app.Option<string>("-registerEvent <REGEVENT>", "The registration event",
																						 CommandOptionType.SingleValue);

				var optionInfo = app.Option<string>("-info <INFO>", "Some information", CommandOptionType.SingleValue);

				// var optionBreak = app.Option("-break", "Attach the debugger", CommandOptionType.NoValue);

				app.Parse(args);

				return new StreamDeckToolkitOptions
				{
					// Break = optionBreak.,
					Info = optionInfo.ParsedValue,
					PluginUUID = optionPluginUUID.ParsedValue,
					Port = optionPort.ParsedValue,
					RegisterEvent = optionRegisterEvent.ParsedValue
				};
			}
		}

		private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
		{
			_logger?.LogError(e.Exception, "Error handling StreamDeck information");
		}

		//CHANGED
		private async Task ReceiveMessage(CancellationToken token)
        {
			switch (_proxy.State)
			{
				case WebSocketState.CloseReceived:
				case WebSocketState.Closed:
				case WebSocketState.Aborted:
					return;
			}

			var jsonString = await _proxy.GetMessageAsString(token);

			if (!string.IsNullOrEmpty(jsonString) && !jsonString.StartsWith("\u0000", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					var msg = JsonConvert.DeserializeObject<StreamDeckEventPayload>(jsonString);

					if (msg == null)
					{
						_logger?.LogError($"Unknown message received: {jsonString}");

						return;
					}

					if (string.IsNullOrWhiteSpace(msg.context) && string.IsNullOrWhiteSpace(msg.action))
					{
						this.BroadcastMessage(msg);
					}
					else
					{
						var action = GetInstanceOfAction(msg.context, msg.action);
						if (action == null)
						{
							_logger?.LogWarning($"The action requested (\"{msg.action}\") was not found as being registered with the plugin");
							return;
						}


						if (!_EventDictionary.ContainsKey(msg.Event))
						{
							_logger?.LogWarning($"Plugin does not handle the event '{msg.Event}'");

							return;
						}

						await _EventDictionary[msg.Event]?.Invoke(action, msg);
					}
				}
				catch (Exception ex)
				{
					_logger?.LogError("Exception while processing payload from StreamDeck: {message}", ex.Message);
					_logger?.LogError("JSON: " + jsonString?.TrimEnd('\0'));
				}
			}
		}

		//CHANGED
		private async Task Run(CancellationToken token)
		{
			_logger?.LogTrace($"{nameof(ConnectionManager)}.{nameof(Run)} port:{_port}, event: {_registerEvent}, uuid: {_uuid}");

			await _proxy.ConnectAsync(new Uri($"ws://localhost:{_port}"), token);
			_logger?.LogTrace($"{nameof(ConnectionManager)}.{nameof(Run)} Connected");
			await _proxy.Register(_registerEvent, _uuid);
			_logger?.LogTrace($"{nameof(ConnectionManager)}.{nameof(Run)} Registered");

			var keepRunning = true;

			while (!token.IsCancellationRequested && keepRunning)
			{
                // Exit loop if the socket is closed or aborted
                switch (_proxy.State)
                {
                    case WebSocketState.CloseReceived:
                    case WebSocketState.Closed:
                    case WebSocketState.Aborted:
                        keepRunning = false;

                        break;
                }

                if (!keepRunning) break;

                Task messageTask = ReceiveMessage(token);
				while (!messageTask.IsCompleted)
				{
					ActionController.Refresh(token); //REMINDER: was await Call and async Task method
					if (!messageTask.IsCompleted)
						await Task.Delay(_pollInterval);
				}
				messageTask.Dispose();
			}

			Dispose();
		}

		#region StreamDeck Methods

		public async Task SetTitleAsync(string context, string newTitle)
		{
			var args = new SetTitleArgs()
			{
				context = context,
				payload = new SetTitleArgs.Payload
				{
					title = newTitle,
					TargetType = SetTitleArgs.TargetType.HardwareAndSoftware
				}
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		//CHANGED
		public async Task SetImageRawAsync(string context, string imgString, string extension = "png")
		{
			var args = new SetImageArgs
			{
				context = context,
				payload = new SetImageArgs.Payload
				{
					TargetType = SetTitleArgs.TargetType.HardwareAndSoftware,
					image = $"data:image/{extension};base64, {imgString}"
				}
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		//CHANGED
		public async Task SetImageAsync(string context, string imageLocation, bool cached = true)
		{
			string imgString = null;
			if (File.Exists(imageLocation))
			{
#if DEBUG
				Debug.WriteLine($"Getting Image from {new FileInfo(imageLocation).FullName} on disk");
				_logger?.LogDebug($"Getting Image from {new FileInfo(imageLocation).FullName} on disk");
#endif
				imgString = StreamDeckTools.ReadImageBase64(imageLocation);
			}
			else
            {
				_logger.LogError($"The Image File {imageLocation} does not exist!");
				return;
            }

			var args = new SetImageArgs
			{
				context = context,
				payload = new SetImageArgs.Payload
				{
					TargetType = SetTitleArgs.TargetType.HardwareAndSoftware,
					image = $"data:image/{new FileInfo(imageLocation).Extension.ToLowerInvariant().Substring(1)};base64, {imgString}"
				}
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		//		public async Task SetImageAsync(string context, string imageLocation)
		//		{

		//#if DEBUG
		//			Debug.WriteLine($"Getting Image from {new FileInfo(imageLocation).FullName} on disk");
		//			_logger?.LogDebug($"Getting Image from {new FileInfo(imageLocation).FullName} on disk");
		//#endif
		//			var imgString = Convert.ToBase64String(File.ReadAllBytes(imageLocation), Base64FormattingOptions.None);

		//			var args = new SetImageArgs
		//			{
		//				context = context,
		//				payload = new SetImageArgs.Payload
		//				{
		//					TargetType = SetTitleArgs.TargetType.HardwareAndSoftware,
		//					image = $"data:image/{new FileInfo(imageLocation).Extension.ToLowerInvariant().Substring(1)};base64, {imgString}"
		//				}
		//			};

		//			await _proxy.SendStreamDeckEvent(args);
		//		}

		public async Task ShowAlertAsync(string context)
		{
			var args = new ShowAlertArgs()
			{
				context = context
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task ShowOkAsync(string context)
		{
			var args = new ShowOkArgs()
			{
				context = context
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task SetSettingsAsync(string context, dynamic value)
		{
			var args = new SetSettingsArgs()
			{
				context = context,
				payload = new { settingsModel = value }
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task SetGlobalSettingsAsync(string context, dynamic value)
		{
			var args = new SetGlobalSettingsArgs()
			{
				context = context,
				payload = new { settingsModel = value }
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task SetStateAsync(string context, int state)
		{
			var args = new SetStateArgs
			{
				context = context,
				payload = new SetStateArgs.Payload
				{
					state = state
				}
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task SwitchToProfileAsync(string context, string device, string profileName)
		{

			var args = new SwitchToProfileArgs
			{
				context = context,
				device = device,
				payload = new SwitchToProfileArgs.Payload
				{
					profile = profileName
				}
			};

			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task OpenUrlAsync(string context, string url)
		{
			var args = new OpenUrlArgs()
			{
				context = context,
				payload = new OpenUrlArgs.Payload()
				{
					url = url
				}
			};

			await _proxy.SendStreamDeckEvent(args);
		}


		public async Task GetSettingsAsync(string context)
		{
			var args = new GetSettingsArgs() { context = context };
			await _proxy.SendStreamDeckEvent(args);
		}


		public async Task GetGlobalSettingsAsync(string context)
		{
			var args = new GetGlobalSettingsArgs() { context = context };
			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task LogMessageAsync(string context, string logMessage)
		{
			var args = new LogMessageArgs()
			{
				context = context,
				payload = new LogMessageArgs.Payload()
				{
					message = logMessage
				}
			};
			await _proxy.SendStreamDeckEvent(args);
		}

		public async Task SendToPropertyInspectorAsync(string context, dynamic settings)
		{
			var args = new SendToPropertyInspectorArgs()
			{
				context = context,
				payload = settings
			};
			await _proxy.SendStreamDeckEvent(args);
		}


		#endregion

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls
		private static ILoggerFactory _LoggerFactory;
		private readonly ILogger<ConnectionManager> _logger;

		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_proxy.Dispose();
					//ADDON
					ActionController.Dispose();
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			//GC.SuppressFinalize(this);
		}

		#endregion
	}

}
