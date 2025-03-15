using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Simulator.XP.WS.Messages;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.XP.WS
{
    public class WebSocketXP(ConnectorXP connector) : IDisposable
    {
        protected virtual ConnectorXP ConnectorXP { get; set; } = connector;
        protected virtual ClientWebSocket WebSocket { get; set; } = new();
        public virtual WebSocketState SocketState => WebSocket.State;
        protected virtual HttpClient RestClient { get; } = new();
        protected virtual bool ClientInitialized { get; set; } = false;
        public virtual string RestBaseUrl { get; } = "/api/v2/";
        public virtual ConcurrentDictionary<long, DataRef> KnownDataRefs { get; } = [];
        public virtual ConcurrentDictionary<long, CommandRef> KnownCommands { get; } = [];
        public virtual bool HasEnumeratedRefs => !KnownDataRefs.IsEmpty && !KnownCommands.IsEmpty;

        public virtual async Task Connect()
        {
            Logger.Debug($"Connecting to X-Plane Web API on {App.Configuration.XPlaneWebApiHost} ...");
            WebSocket = new();
            WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            await WebSocket.ConnectAsync(new($"ws://{App.Configuration.XPlaneWebApiHost}/api/v2"), App.CancellationToken);
            if (!ClientInitialized)
            {
                RestClient.BaseAddress = new($"http://{App.Configuration.XPlaneWebApiHost}");
                RestClient.DefaultRequestHeaders.Accept.Clear();
                RestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                RestClient.DefaultRequestHeaders.Connection.Add("Keep-Alive");
                ClientInitialized = true;
            }
            Logger.Debug($"Connection Request sent.");
        }

        public virtual async Task<string> GetMessageAsString()
        {
            var buffer = new byte[65536];
            var segment = new ArraySegment<byte>(buffer, 0, buffer.Length);
            var result = await WebSocket.ReceiveAsync(segment, App.CancellationToken);

            string jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            jsonString = jsonString.Trim();

            return jsonString;
        }

        public virtual async Task SendJsonRequest<T>(T @object) where T : class
        {
            try
            {
                await WebSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(@object)), WebSocketMessageType.Text, true, App.CancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task<bool> CheckConnected()
        {
            try
            {
                await WebSocket.SendAsync(Encoding.UTF8.GetBytes(""), WebSocketMessageType.Text, true, App.CancellationToken);
                await RestClient.GetAsync($"{RestBaseUrl}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task EnumerateRefs()
        {
            ClearKnownRefs();
            Logger.Debug($"Enumerating DataRefs via Web API ...");
            DataRefList dataRefList = await RestGet<DataRefList>("datarefs");
            foreach (var dataRef in dataRefList.data)
                KnownDataRefs.Add(dataRef.id, dataRef);
            Logger.Debug($"Received {KnownDataRefs.Count} DataRefs");

            Logger.Debug($"Enumerating CommandRefs via Web API ...");
            CommandRefList commandList = await RestGet<CommandRefList>("commands");
            foreach (var command in commandList.data)
                KnownCommands.Add(command.id, command);
            Logger.Debug($"Received {KnownCommands.Count} CommandRefs");
        }

        public virtual void ClearKnownRefs()
        {
            KnownDataRefs.Clear();
            KnownCommands.Clear();
        }

        protected virtual async Task<T> RestGet<T>(string path) where T : class
        {
            T jsonObject = null;
            HttpResponseMessage response = await RestClient.GetAsync($"{RestBaseUrl}{path}");
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    jsonObject = await response.Content.ReadFromJsonAsync<T>(App.CancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }                
            }

            return jsonObject;
        }

        public virtual async Task<bool> SetDataRef(long id, object value, int? index = null)
        {
            DataRefValue refValue = null;
            if (value is double numValue)
                refValue = DataRefValue.CreateNumber(numValue);
            else if (value is string strValue)
                refValue = DataRefValue.CreateString(strValue);

            if (refValue == null)
                return false;

            string query = "";
            if (index != null)
                query = $"?index={index}";

            var response = await RestClient.PatchAsJsonAsync($"{RestBaseUrl}datarefs/{id}/value{query}", refValue, App.CancellationToken);
            return response.IsSuccessStatusCode;
        }

        public virtual async Task<bool> ActivateCommandRef(long id, float duration = 0)
        {
            var body = new ActivateCommandRefMessage(duration);

            var response = await RestClient.PostAsJsonAsync($"{RestBaseUrl}command/{id}/activate", body, App.CancellationToken);
            return response.IsSuccessStatusCode;
        }

        public virtual bool HasCommandRef(string cmdRef, out long id)
        {
            var query = KnownCommands.Where(kv => kv.Value.name == cmdRef);
            if (query.Any())
            {
                id = query.First().Key;
                return true;
            }
            else
            {
                Logger.Warning($"No ID found for CommandRef '{cmdRef}'");
                id = 0;
                return false;
            }
        }

        public virtual async Task CloseSockets()
        {
            try { await WebSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); WebSocket?.Dispose(); } catch { }
            try { WebSocket?.Dispose(); } catch { }
            try { RestClient?.CancelPendingRequests(); RestClient?.Dispose(); } catch { }
            try { RestClient?.Dispose(); } catch { }
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _ = CloseSockets();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
