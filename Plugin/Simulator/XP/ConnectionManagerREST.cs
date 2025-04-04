using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.XP.WS;
using PilotsDeck.Simulator.XP.WS.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.XP
{
    public class ConnectionManagerREST : IDisposable
    {
        public static readonly int VERSIONWEBAPI = 121400;

        public virtual WebSocketXP WebSocket { get; }
        protected virtual ConnectorXP Connector { get; }
        public virtual bool DoRun { get; set; } = true;
        public virtual bool IsConnected { get; protected set; } = false;
        protected virtual MappingManager MappingManager { get; }
        public virtual int RequestID { get; protected set; } = 1;
        protected virtual ConcurrentDictionary<int, OutstandingRequest> OutstandingRequests { get; } = [];
        public virtual ConcurrentDictionary<long, bool> ActiveCommands { get; } = [];
        protected virtual ConcurrentDictionary<long, bool> ActiveSubRequests { get; } = [];
        protected virtual ConcurrentDictionary<long, bool> ActiveUnsubRequests { get; } = [];
        public virtual bool HasEnumeratedRefs => WebSocket.HasEnumeratedRefs;
        protected virtual bool IsWorking { get; set; } = false;

        public ConnectionManagerREST(ConnectorXP connector)
        {
            WebSocket = new(connector);
            Connector = connector;
            MappingManager = new(WebSocket);
        }

        public virtual bool IsSocketStateValid() => WebSocket.SocketState switch
        {
            WebSocketState.CloseReceived or WebSocketState.Closed or WebSocketState.Aborted => false,
            _ => true
        };

        public virtual async void Run()
        {
            try
            {
                do
                { 
                    if (!IsConnected || !IsSocketStateValid())
                    {
                        try
                        {
                            await WebSocket.Connect();
                            IsConnected = WebSocket.SocketState == WebSocketState.Open;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }

                        if (!IsConnected)
                        {
                            Logger.Debug($"X-Plane Web Socket not connected, Retry in {App.Configuration.XPlaneRetryDelay / 1000}s");
                            await Task.Delay(App.Configuration.XPlaneRetryDelay, App.CancellationToken);
                            continue;
                        }
                        else
                        {
                            Logger.Information($"X-Plane Wep API connected.");
                        }
                    }
                    else
                    {
                        ProcessReceived(await WebSocket.GetMessageAsString());
                    }
                }
                while (DoRun && !App.CancellationToken.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual async Task OnSessionEnter()
        {
            if (!WebSocket.HasEnumeratedRefs && IsConnected && !IsWorking)
            {
                IsWorking = true;
                try
                {
                    await WebSocket.EnumerateRefs();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                IsWorking = false;
            }
        }

        public virtual async Task OnSessionLeave()
        {
            if (IsConnected && !IsWorking)
            {
                IsWorking = true;
                try
                {
                    await UnsubscribeAllRefs();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                IsWorking = false;
            }
        }

        protected virtual void ProcessReceived(string json)
        {
            var result = ResultXP.GetResult(json);
            Logger.Verbose($"Result ID '{result.req_id}' of Type '{result.type}'");
            if (result.type == ResultType.Request)
            {
                if (OutstandingRequests.TryGetValue(result.req_id, out OutstandingRequest request))
                {
                    if (!result.success)
                    {
                        Logger.Warning($"Request with ID '{request.ID}' of Type '{request.Type}' failed: {result.error_code} - {result.error_message}");
                    }
                    else if (request.Type == RequestType.SubscribeDataRef && request.RequestData is List<IdMappingXP> subscribeList)
                    {
                        foreach (var sub in subscribeList)
                        {
                            MappingManager.AddDataRef(sub);
                            ActiveSubRequests.Remove(sub.RefId);
                        }
                    }
                    else if (request.Type == RequestType.UnsubscribeDataRef && request.RequestData is UnsubscribeAllRefMessage)
                    {
                        MappingManager.RemoveAllDataRef();
                        ActiveSubRequests.Clear();
                        ActiveUnsubRequests.Clear();
                    }
                    else if (request.Type == RequestType.UnsubscribeDataRef && request.RequestData is List<IdMappingXP> unsubscribeList)
                    {
                        foreach (var sub in unsubscribeList)
                        {
                            MappingManager.RemoveDataRef(sub);
                            ActiveUnsubRequests.Remove(sub.RefId);
                        }
                    }
                    else if (request.Type == RequestType.SetCommandActive)
                    {
                        Logger.Debug($"Command Request with Request ID '{request.ID}' succeeded");
                    }
                    else
                        Logger.Warning($"Unknown Result for Request ID '{result.req_id}' received - Type {result.type}");

                    OutstandingRequests.Remove(result.req_id);
                }
                else
                {
                    if (result.req_id != 0)
                        Logger.Warning($"Request ID '{result.req_id}' not in outstanding Requests - Type: {result.type} - json: {json}");
                    else
                        Logger.Verbose($"Request ID '{result.req_id}' not in outstanding Requests - Type: {result.type} - json: {json}");
                }
            }
            else if (result.type == ResultType.UpdateDataRef)
            {
                var updateMessage = UpdateMessage.GetUpdateMessage(json);
                foreach (var update in updateMessage.data)
                {
                    if (!long.TryParse(update.Key, out long id) || !MappingManager.IsIdSubscribed(id))
                    {
                        Logger.Warning($"Could not parse or find ID '{update.Key}'");
                        continue;
                    }
                    if (App.Configuration.LogLevel == LogLevel.Verbose)
                        Logger.Verbose($"Update for id '{id}': {update.Value}");
                    MappingManager.UpdateRef(id, (JsonElement)update.Value);
                }
            }
            else
            {
                Logger.Warning($"Unknown Message with ID '{result.req_id}' received - Type: {result.type}");
            }
                
        }

        protected virtual bool DoRefSubscribe(ManagedAddress address, out long id)
        {
            id = 0;
            var query = WebSocket.KnownDataRefs.Where(kv => kv.Value.name == address.Name);
            if (!query.Any())
                return false;
            else
                id = query.First().Key;
            
            return true;
        }

        public virtual async Task SubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!Connector.IsReadyProcess || !IsConnected || !WebSocket.HasEnumeratedRefs || !IsSocketStateValid())
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            List<IdMappingXP> subscribeList = [];
            foreach (var managedVariable in managedVariables.Where(m => m.IsValueXP()))
            {
                if (!DoRefSubscribe(managedVariable.Address, out long id))
                {
                    Logger.Warning($"DataRef is not in known List: {managedVariable.Address.Name}");
                    continue;
                }
                if (ActiveSubRequests.ContainsKey(id))
                    continue;
                Logger.Verbose($"Adding '{id}' => '{managedVariable.Address}' to Subscribe List");
                subscribeList.Add(IdMappingXP.Create(id, managedVariable));
            }

            if (subscribeList.Count == 0)
                return;

            SubscribeDataRefMessage subscribeMessage = new(RequestID++);
            foreach (var sub in subscribeList)
                subscribeMessage.AddDataRef(sub.RefId);

            Logger.Verbose($"Sending Subscribe DataRef Request '{subscribeMessage.req_id}' - Count {subscribeList.Count}");
            OutstandingRequests.Add(subscribeMessage.req_id, OutstandingRequest.Create(subscribeMessage.req_id, subscribeList, subscribeMessage.type));
            await WebSocket.SendJsonRequest(subscribeMessage);
            Logger.Verbose($"Request sent.");
            if (App.Configuration.LogLevel == LogLevel.Verbose)
                foreach (var sub in subscribeList)
                    ActiveSubRequests.Add(sub.RefId);
        }

        public virtual async Task UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!Connector.IsReadyProcess || !IsConnected || !WebSocket.HasEnumeratedRefs || !IsSocketStateValid())
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            List<IdMappingXP> unsubscribeList = [];
            foreach (var managedVariable in managedVariables.Where(m => m.IsValueXP()))
            {
                if (MappingManager.GetMapping(managedVariable, out var idMapping) && !ActiveUnsubRequests.ContainsKey(idMapping.RefId))
                    unsubscribeList.Add(idMapping);
            }

            if (unsubscribeList.Count == 0)
                return;

            UnsubscribeDataRefMessage unsubscribeMessage = new(RequestID++);
            foreach (var sub in unsubscribeList)
                unsubscribeMessage.AddDataRef(sub.RefId);

            OutstandingRequests.Add(unsubscribeMessage.req_id, OutstandingRequest.Create(unsubscribeMessage.req_id, unsubscribeList, unsubscribeMessage.type));
            await WebSocket.SendJsonRequest(unsubscribeMessage);
            foreach (var sub in unsubscribeList)
                ActiveUnsubRequests.Add(sub.RefId);
        }

        public virtual async Task UnsubscribeAllRefs()
        {
            if (!Connector.IsReadyProcess || !IsConnected || !IsSocketStateValid())
                return;

            var message = new UnsubscribeAllRefMessage(RequestID++);
            OutstandingRequests.Add(message.req_id, OutstandingRequest.Create(message.req_id, message, RequestType.UnsubscribeDataRef));
            await WebSocket.SendJsonRequest(message);
        }


        public virtual async Task<bool> WriteRef(SimCommand command)
        {
            if (MappingManager.HasDataRef(command.Address.Name, out var dataRef))
            {
                object value;
                if (command.Address.IsStringType)
                    value = command.Value;
                else
                    value = command.NumValue;

                if (command.Address.TryGetXpArrayIndex(out int index))
                    await WebSocket.SetDataRef(dataRef.id, value, index);
                else
                    await WebSocket.SetDataRef(dataRef.id, value);

                return true;
            }

            return false;
        }

        public virtual async Task<bool> SendCommand(SimCommand command)
        {
            string[] xpcmds = command.Address.Address.Split(':');

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                if (xpcmds.Length > 1)
                {
                    foreach (string xpcmd in xpcmds)
                    {
                        if (WebSocket.HasCommandRef(xpcmd, out long id) && await SendCommandRef(id, command.IsUp))
                        {
                            success++;
                            if (command.CommandDelay > 0)
                                await Task.Delay(command.CommandDelay, App.CancellationToken);
                        }
                    }
                }
                else if (xpcmds.Length == 1)
                {
                    if (WebSocket.HasCommandRef(command.Address.Address, out long id) && await SendCommandRef(id, command.IsUp))
                        success++;
                }
                else
                {
                    Logger.Error($"Command-Array has zero members! (Address: {command?.Address})");
                }
            }

            return success == xpcmds.Length * command.Ticks;
        }

        public virtual async Task<bool> SendCommandRef(long id, bool isUp)
        {
            var message = new SetCommandRefMessage(RequestID++);
            if (!isUp)
            {
                message.AddCommandRef(id, true, null);
                if (!ActiveCommands.TryAdd(id, true))
                    ActiveCommands[id] = true;
            }
            else 
            {
                if (ActiveCommands.TryGetValue(id, out bool isActive) && isActive)
                    message.AddCommandRef(id, false, null);
                else
                    message.AddCommandRef(id, true, 0);
                ActiveCommands.TryRemove(id, out _);
            }
            
            var cid = message.@params.commands.First().id;
            Logger.Debug($"Sending Command Request '{message.req_id}': {cid} -> {WebSocket.KnownCommands[cid].name} | {message.@params.commands.First().duration} | {message.@params.commands.First().is_active}");
            OutstandingRequests.Add(message.req_id, OutstandingRequest.Create(message.req_id, message, RequestType.SetCommandActive));
            await WebSocket.SendJsonRequest(message);
            Logger.Debug($"Command sent.");
            return true;
        }

        public virtual async Task Stop()
        {
            try
            {
                if (MappingManager.HasDataRefMappings)
                    await UnsubscribeAllRefs();
            }
            catch { }
            DoRun = false;
            await Task.Delay(50);
            await WebSocket.CloseSockets();
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    WebSocket?.Dispose();
                    Logger.Debug("Sockets released");
                }
                _disposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
