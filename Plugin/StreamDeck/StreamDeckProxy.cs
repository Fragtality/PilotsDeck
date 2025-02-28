using CFIT.AppLogger;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PilotsDeck.StreamDeck
{
    public class StreamDeckSocket : IDisposable
    {
        private readonly ClientWebSocket _Socket = new();
        private static bool _IsSending = false;

        public WebSocketState SocketState { get { return _Socket.State; } }

        public Task ConnectAsync()
        {
            Uri uri = new($"ws://{App.Configuration.StreamDeckHost}:{App.CommandLineArgs["port"]}");
            Logger.Information($"Connecting to StreamDeck Software @ {uri} ...");
            return _Socket.ConnectAsync(uri, App.CancellationToken);
        }
        public Task RegisterAsync()
        {
            var registration = new PluginRegistration
            {
                Event = App.CommandLineArgs["registerEvent"],
                uuid = App.CommandLineArgs["pluginUUID"],
            };
            Logger.Information($"Registering Plugin with event '{registration.Event}' and uuid '{registration.uuid}' ...");
            return SendAsync(JsonSerializer.Serialize(registration));
        }

        public bool IsSocketStateValid() => SocketState switch
        {
            WebSocketState.CloseReceived or WebSocketState.Closed or WebSocketState.Aborted => false,
            _ => true
        };

        public async Task<string> GetMessageAsString()
        {
            var buffer = new byte[65536];
            var segment = new ArraySegment<byte>(buffer, 0, buffer.Length);
            var result = new WebSocketReceiveResult(0, WebSocketMessageType.Text, false);
            var totalCount = 0;

            while (!result.EndOfMessage)
            {
                result = await _Socket.ReceiveAsync(segment, App.CancellationToken);
                totalCount += result.Count;
                segment = new ArraySegment<byte>(buffer, totalCount, buffer.Length - totalCount);
            }
            segment = new ArraySegment<byte>(buffer, 0, totalCount);
            string jsonString = Encoding.UTF8.GetString(segment);
            jsonString = jsonString.Trim();

            return jsonString;
        }

        public async Task SendJson(string json)
        {
            try
            {
                while (_IsSending) { }

                _IsSending = true;
                await SendAsync(json);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _IsSending = false;
            }
        }

        private Task SendAsync(string json)
        {
            if (App.Configuration.LogLevel == LogLevel.Verbose)
                Logger.Verbose(json);
            return _Socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, App.CancellationToken);
        }

        private bool disposedValue = false;
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _Socket.Dispose();
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
