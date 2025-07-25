using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Plugin;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PilotsDeck.StreamDeck
{
    public class DeckController : IDisposable
    {
        public bool IsConnected { get { return Socket.SocketState == WebSocketState.Open; } }
        public StreamDeckInfoMessage DeckInfo { get; protected set; }
        protected int InitialDeviceCount { get; set; } = 0;
        public int ReceivedDevices { get; protected set; } = 0;
        public static string PluginContext { get { return App.CommandLineArgs["pluginUUID"]; } }
        protected StreamDeckSocket Socket { get; } = new();

        protected Channel<StreamDeckEvent> ChannelEventsReceived { get; } = Channel.CreateUnbounded<StreamDeckEvent>();
        public ChannelReader<StreamDeckEvent> ReceiveChannel { get { return ChannelEventsReceived.Reader; } }
        protected Channel<string> ChannelEventsSend { get; } = Channel.CreateUnbounded<string>();
        public ChannelWriter<string> SendChannel { get { return ChannelEventsSend.Writer; } }

        public async void Run()
        {
            try
            {
                Logger.Debug("Parsing 'info' Argument ...");
                DeckInfo = JsonSerializer.Deserialize<StreamDeckInfoMessage>(App.CommandLineArgs["info"]);
                InitialDeviceCount = DeckInfo.devices.Count;

                await Socket.ConnectAsync();
                await Socket.RegisterAsync();

                if (IsConnected)
                    Logger.Information("Plugin connected & Registration send.");
                else
                    throw new Exception("Connection to StreamDeck Software failed!");

                StatisticManager.AddTracker(StatisticID.SD_RECEIVE);
                StatisticManager.AddTracker(StatisticID.SD_TRANSMIT);
                await Task.WhenAll(ReceiveEvents(), SendEvents());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            Logger.Information("DeckController ended");
        }

        protected async Task SendEvents()
        {
            try
            {
                Logger.Information("SendEvents Task starting ...");
                await Task.Delay(App.Configuration.DelayStreamDeckConnect, App.CancellationToken);
                while (!App.CancellationToken.IsCancellationRequested && Socket.IsSocketStateValid())
                {
                    string json = await ChannelEventsSend.Reader.ReadAsync(App.CancellationToken);
                    StatisticManager.StartTrack(StatisticID.SD_TRANSMIT);

                    await Socket.SendJson(json);

                    StatisticManager.EndTrack(StatisticID.SD_TRANSMIT);
                }
            }
            catch (WebSocketException)
            {
                Logger.Error("Socket has thrown WebSocketException");
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    Logger.LogException(ex);
            }

            Logger.Information("SendEvents Task ended");
        }

        protected async Task ReceiveEvents()
        {
            try
            {
                Logger.Information("ReceiveEvents Task starting ...");
                await Task.Delay(App.Configuration.DelayStreamDeckConnect, App.CancellationToken);
                while (!App.CancellationToken.IsCancellationRequested && Socket.IsSocketStateValid())
                {
                    string json = await Socket.GetMessageAsString();
                    StatisticManager.StartTrack(StatisticID.SD_RECEIVE);

                    if (!string.IsNullOrEmpty(json))
                    {
                        StreamDeckEvent sdEvent = null;
                        try
                        {
                            sdEvent = JsonSerializer.Deserialize<StreamDeckEvent>(json, JsonOptions.JsonSerializerOptions);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                            Logger.Debug(json);
                            continue;
                        }

                        if (sdEvent == null)
                        {
                            Logger.Error($"Unknown Message received: '{json}'");
                            continue;
                        }
                        else if (sdEvent.Event == "deviceDidConnect")
                        {
                            AddUpdateDevice(sdEvent);
                        }
                        else if (sdEvent.Event == "deviceDidDisconnect")
                        {
                            RemoveDevice(sdEvent);
                        }
                        else
                        {
                            if (App.Configuration.LogLevel == LogLevel.Verbose)
                                Logger.Verbose(json);

                            await ChannelEventsReceived.Writer.WriteAsync(sdEvent, App.CancellationToken);
                        }
                    }

                    StatisticManager.EndTrack(StatisticID.SD_RECEIVE);
                }
            }
            catch (WebSocketException)
            {
                Logger.Error("Socket has thrown WebSocketException");
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    Logger.LogException(ex);
            }

            Logger.Information("ReceiveEvents Task ended");
            App.DoShutdown();
        }

        protected void AddUpdateDevice(StreamDeckEvent sdEvent)
        {
            var query = DeckInfo.devices.Where(d => d.id.Equals(sdEvent.device, StringComparison.InvariantCultureIgnoreCase));
            if (query.Any())
            {
                var device = query.First(); 
                device.type = sdEvent.deviceInfo.type;
                device.name = sdEvent.deviceInfo.name;
                device.size = sdEvent.deviceInfo.size;
                Logger.Information($"Updated Device Info for '{device.name}' ({device.id})");
                ReceivedDevices++;
            }
            else
            {
                var device = new StreamDeckInfoMessage.Device()
                {
                    id = sdEvent.device.ToUpperInvariant(),
                    type = sdEvent.deviceInfo.type,
                    name = sdEvent.deviceInfo.name,
                    size = sdEvent.deviceInfo.size
                };
                DeckInfo.devices.Add(device);
                Logger.Information($"Added Device Info for '{device.name}' ({device.id})");
            }
        }

        protected void RemoveDevice(StreamDeckEvent sdEvent)
        {
            var query = DeckInfo.devices.Where(d => d.id.Equals(sdEvent.device, StringComparison.InvariantCultureIgnoreCase));
            if (query.Any())
            {

                Logger.Information($"Removed Device Info for '{query.First().name}' ({query.First().id})");
                DeckInfo.devices.RemoveAll(d => d.id.Equals(sdEvent.device, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public StreamDeckType GetDeckType(string deviceUuid)
        {
            var item = DeckInfo.devices.Where(d => d.id.Equals(deviceUuid, StringComparison.InvariantCultureIgnoreCase))?.FirstOrDefault();
            if (item == null || item?.type == null)
                return (StreamDeckType)0;
            else
                return item.type;
        }

        protected async Task QueueMessageAsync(string json)
        {
            await ChannelEventsSend.Writer.WriteAsync(json, App.CancellationToken);
        }

        public async Task SendSetSettings(string context, JsonNode payload)
        {
            var args = new SetSettingsArgs()
            {
                context = context,
                payload = payload
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendGetSettings(string context)
        {
            var args = new GetSettingsArgs()
            {
                context = context
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSetGlobalSettings(JsonNode payload)
        {
            var args = new SetGlobalSettingsArgs()
            {
                context = PluginContext,
                payload = payload
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendGetGlobalSettings()
        {
            var args = new GetGlobalSettingsArgs()
            {
                context = PluginContext
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendOpenUrl(string url)
        {
            var args = new OpenUrlArgs()
            {
                payload = new OpenUrlArgs.Payload()
                {
                    url = url
                }
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendLogMessage(string logMessage)
        {
            var args = new LogMessageArgs()
            {
                payload = new LogMessageArgs.Payload()
                {
                    message = logMessage
                }
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSetTitle(string context, string newTitle)
        {
            var args = new SetTitleArgs()
            {
                context = context,
                payload = new SetTitleArgs.Payload
                {
                    title = newTitle
                }
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSetImageRaw(string context, string image64, string extension = "png")
        {
            var args = new SetImageArgs
            {
                context = context,
                payload = new SetImageArgs.Payload
                {
                    target = TargetType.HardwareAndSoftware,
                    image = $"data:image/{extension};base64, {image64}"
                }
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SetFeedbackItemImageRaw(string target, string context, string imgString, string extension = "png")
        {
            var args = new SetFeedbackItemArgs
            {
                context = context,
            };
            args.payload.Add(target, $"data:image/{extension};base64, {imgString}");
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSetFeedbackItemTextRaw(string target, string context, string title)
        {
            var args = new SetFeedbackItemArgs
            {
                context = context,
            };
            args.payload.Add(target, title);
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSetFeedbackLayout(string context, string layout)
        {
            var args = new SetFeedbackLayoutArgs
            {
                context = context,
                payload = new SetFeedbackLayoutArgs.Payload
                {
                    layout = layout
                }
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendShowAlert(string context)
        {
            var args = new ShowAlertArgs()
            {
                context = context
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendShowOk(string context)
        {
            var args = new ShowOkArgs()
            {
                context = context
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSetState(string context, int state)
        {
            var args = new SetStateArgs
            {
                context = context,
                payload = new SetStateArgs.Payload
                {
                    state = state
                }
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        public async Task SendSwitchToProfile(string context, string device, string profileName)
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
            var json = JsonSerializer.Serialize(args);
            Logger.Debug($"Sending Profile Switch Command: {json}");
            await QueueMessageAsync(json);
        }

        public async Task SendToPropertyInspector(string context, dynamic settings)
        {
            var args = new SendToPropertyInspectorArgs()
            {
                context = context,
                payload = settings
            };
            await QueueMessageAsync(JsonSerializer.Serialize(args));
        }

        protected bool disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Socket.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
