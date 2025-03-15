using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.XP.UDP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.XP
{
    public class ConnectionManagerUDP(ConnectorXP connector) : IDisposable
    {
        public static readonly string TIMEPAUSED_REF = "sim/time/paused";
        public static readonly int TIMEPAUSED_INDEX = 0;
        public static readonly string SIMVERSION_REF = "sim/version/xplane_internal_version";
        public static readonly int SIMVERSION_INDEX = TIMEPAUSED_INDEX + 1;
        public static readonly string AIRCRAFT_XP12_REF = "sim/aircraft/view/acf_relative_path:s128";
        public static readonly string AIRCRAFT_XP11_REF = "sim/aircraft/view/acf_livery_path:s128";
        public static readonly int AIRCRAFT_INDEX = SIMVERSION_INDEX + 1;
        public static readonly int INDEX_BASE_DYNAMIC = AIRCRAFT_INDEX + 128;

        public static readonly int VERSIONXP12 = 120000;
        public static readonly int VERSIONXP95 = 95000;

        protected virtual SocketXP Socket { get; } = new(connector);
        protected virtual ConnectorXP Connector { get; } = connector;
        public virtual bool IsConnected => Socket.IsConnected;
        public virtual bool SocketError { get; protected set; } = false;
        public virtual bool IsCanceled { get; protected set; } = false;
        public virtual bool IsLoading { get; protected set; } = true;
        public virtual bool IsPaused { get; protected set; } = true;
        public virtual string AircraftString { get; protected set; } = "";
        public virtual CancellationTokenSource ReceiveTokenSource { get; protected set; } = new();
        public virtual bool ReceivedAircraftString { get; protected set; } = false;
        public virtual bool RefsInternalSubscribed { get; protected set; } = false;
        public virtual bool RefAircraftSubscribed { get; protected set; } = false;
        protected virtual ConcurrentDictionary<int, IndexMappingXP> IndexMapping { get; set; } = [];
        protected virtual ConcurrentDictionary<int, int> UnknownIndices { get; private set; } = [];
        protected virtual int NextIndex { get; set; } = INDEX_BASE_DYNAMIC;
        protected virtual DateTime LastMessageReceived { get; set; } = DateTime.Now;
        public virtual int SimVersion { get; protected set; } = 0;

        public virtual void Connect()
        {
            Socket.Connect();
        }

        public virtual void Stop()
        {
            Socket?.Close();
            ReceiveTokenSource?.Cancel();
            IsCanceled = true;
        }

        public virtual async Task Run()
        {
            try
            {
                int count = 0;
                int sleep = 500;
                do
                {
                    if (SocketError)
                    {
                        Logger.Information($"Connection NOT successful - retry in {App.Configuration.XPlaneRetryDelay / 1000.0}s");
                        Socket?.Close();
                        count = 0;
                        do
                        {
                            await Task.Delay(sleep, App.CancellationToken);
                            count += sleep;
                        }
                        while (count < App.Configuration.XPlaneRetryDelay && !IsCanceled && Connector.IsRunning);

                        SocketError = false;

                        if (Connector.IsRunning && !IsCanceled)
                            continue;
                        else
                            break;
                    }

                    if (!Socket.IsConnected)
                    {
                        ClearState();
                        if (Socket.Connect())
                            SubscribeInternal();
                    }

                    await ReceiveAsync();
                }
                while (Connector.IsRunning && !App.CancellationToken.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual void ClearState()
        {
            RefsInternalSubscribed = false;
            RefAircraftSubscribed = false;
            ReceivedAircraftString = false;
            IsLoading = true;
            IsPaused = true;
            AircraftString = "";
            SimVersion = 0;
            try
            {
                IndexMapping.TryRemove(TIMEPAUSED_INDEX, out _);
                IndexMapping.TryRemove(SIMVERSION_INDEX, out _);
                IndexMapping.TryRemove(AIRCRAFT_INDEX, out _);
                IndexMapping.Values.ToList().ForEach(m => m.IsRequested = false);
                IndexMapping.Values.ToList().ForEach(m => m.ValueRef.IsSubscribed = false);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual async Task ReceiveAsync()
        {
            try
            {
                var response = await Socket.SocketReceive.ReceiveAsync(ReceiveTokenSource.Token);
                var header = DatagramXP.GetHeader(response.Buffer);

                if (header != DatagramXP.XP_STR_RREF)
                {
                    Logger.Warning($"Received unhandled Datagram Header '{header}'");
                    return;
                }

                int pos = DatagramXP.XP_STR_RREF.Length + 1;
                while (DatagramXP.ParseRREF(response.Buffer, ref pos, out int index, out float value))
                {
                    if (App.Configuration.LogLevel == LogLevel.Verbose)
                        Logger.Verbose($"Received RREF Datagram - Index '{index}' - Value '{value}'");
                    ProcessReceived(index, value);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is ObjectDisposedException || ex is SocketException || ex is OperationCanceledException))
                    Logger.LogException(ex);
                else
                {
                    Logger.Error("Receive Socket threw an Exception");
                }
                SocketError = true;
            }
        }

        public void ProcessReceived(int index, float value)
        {
            try
            {
                if (index == TIMEPAUSED_INDEX)
                {
                    if ((value != 0.0f) != IsPaused)
                    {
                        IsPaused = value != 0.0f;
                        Logger.Information($"Pause State toggled: {IsPaused}");
                    }
                    LastMessageReceived = DateTime.Now;
                }
                else if (index == SIMVERSION_INDEX)
                {
                    if (SimVersion != value)
                    {
                        SimVersion = (int)value;
                        Logger.Information($"Received Version of X-Plane: {SimVersion}");
                        if (!RefAircraftSubscribed)
                        {

                            if (SimVersion >= VERSIONXP12 && !App.Configuration.XPlaneUseLiveryRefOn12)
                            {
                                Logger.Information("Subscribing Aircraft DataRef for Plugin (XP 12.0)");
                                SubscribeValueInternal(AIRCRAFT_XP12_REF, AIRCRAFT_INDEX, 1, true);
                            }
                            else if (SimVersion >= VERSIONXP95)
                            {
                                Logger.Information("Subscribing Aircraft DataRef for Plugin (XP 9.5+)");
                                SubscribeValueInternal(AIRCRAFT_XP11_REF, AIRCRAFT_INDEX, 1, true);
                            }

                            RefAircraftSubscribed = true;
                        }
                    }
                }
                else if (index >= AIRCRAFT_INDEX && index < INDEX_BASE_DYNAMIC)
                {
                    if (!ReceivedAircraftString)
                    {
                        ReceivedAircraftString = index == INDEX_BASE_DYNAMIC - 1;
                    }

                    if (IsLoading && ReceivedAircraftString)
                    {
                        Logger.Debug($"End Loading State for X-Plane (received Aircraft String)");
                        Connector.TimerCheckLoading.Start();
                        IsLoading = false;
                    }

                    if (IndexMapping.TryGetValue(index, out var aircraftMapping) && aircraftMapping?.ValueRef is VariableString valueRef)
                    {
                        if (index == AIRCRAFT_INDEX)
                        {
                            valueRef.SetValue("");
                            if (!ReceivedAircraftString)
                                Logger.Debug($"Received first Index of AircraftString");
                        }

                        valueRef.SetChar(aircraftMapping.CharIndex, value);

                        if (index == INDEX_BASE_DYNAMIC - 1 && aircraftMapping.ValueRef.Value != AircraftString)
                        {
                            AircraftString = aircraftMapping.ValueRef.Value;
                            Logger.Information($"Aircraft String changed to '{AircraftString}'");
                        }
                    }
                    else
                        Logger.Warning($"Could not retrieve Aircraft Mapping or ValueRef is invalid");
                }
                else if (IndexMapping.TryGetValue(index, out IndexMappingXP mapping))
                {
                    if (!mapping.IsString)
                        mapping.ValueRef.SetValue(value);
                    else
                    {
                        var strvalue = mapping.ValueRef as VariableString;
                        strvalue.SetChar(mapping.CharIndex, value);
                    }
                }
                else
                {
                    if (UnknownIndices.TryAdd(index, 1))
                        Logger.Debug($"The received Index '{index}' is not mapped and was added to the unkown List");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual void CheckLoading()
        {
            if (!IsLoading && DateTime.Now - LastMessageReceived > TimeSpan.FromMilliseconds(App.Configuration.XPlaneTimeoutReceive))
            {
                Logger.Information($"Set Loading State for X-Plane (Receive Timeout {App.Configuration.XPlaneTimeoutReceive / 1000}s)");
                IsLoading = true;
                AircraftString = "";
                if (IndexMapping.TryGetValue(AIRCRAFT_INDEX, out IndexMappingXP aircraftMapping))
                    (aircraftMapping.ValueRef as VariableString)?.SetValue("");
                ReceivedAircraftString = false;
            }
            else if (IsLoading && App.Configuration.XPlaneIP != "127.0.0.1" && DateTime.Now - LastMessageReceived > TimeSpan.FromMilliseconds(App.Configuration.XPlaneRetryDelay))
            {
                Logger.Information($"Timeout to Remote X-Plane - closing Connection");
                Connector.RemoteRunning = false;
                ReceiveTokenSource.Cancel();
            }
        }

        protected void SubscribeInternal()
        {
            if (!RefsInternalSubscribed)
            {
                Logger.Information("Subscribing internal DataRefs for Plugin");
                SubscribeValueInternal(TIMEPAUSED_REF, TIMEPAUSED_INDEX, (int)(1000 / (App.Configuration.IntervalSimProcess)));
                SubscribeValueInternal(SIMVERSION_REF, SIMVERSION_INDEX, 1);
                RefsInternalSubscribed = true;
            }
        }

        protected void SubscribeValueInternal(string dataref, int index, int rate, bool isString = false)
        {
            if (!isString)
            {
                VariableNumeric managedVariable = new(new ManagedAddress(dataref))
                {
                    IsSubscribed = true
                };

                SubscribeMapping(IndexMappingXP.Create(index, managedVariable), rate);
            }
            else
            {
                VariableString managedVariable = new(new ManagedAddress(dataref))
                {
                    IsSubscribed = true
                };

                SubscribeVariableString(managedVariable, true, index, rate);
            }
        }

        protected bool ContainsAddress(ManagedAddress address, out int index)
        {
            index = -1;
            var query = IndexMapping.Where(m => m.Value.ValueRef.Address == address);
            if (query.Any())
            {
                if (query.Where(m => m.Value.IsRequested).Any())
                {
                    Logger.Error($"The Address '{address}' is already mapped and requested!");
                    return true;
                }
                else
                {
                    index = query.Where(m => !m.Value.IsRequested && m.Value.CharIndex == 0).First().Value.SimIndex;
                    return false;
                }
            }
            else
                return false;
        }

        protected virtual bool HasAddress(ManagedAddress address, out IndexMappingXP mapping)
        {
            var query = IndexMapping.Where(m => m.Value?.ValueRef?.Address == address && m.Value?.IsRequested == true);
            if (query?.Any() == true)
            {
                mapping = query.Where(m => m.Value?.CharIndex == 0).FirstOrDefault().Value;
                return mapping != null;
            }
            else
            {
                mapping = null;
                Logger.Error($"The Address '{address}' is not mapped and requested!");
                return false;
            }
        }

        public virtual void SubscribeVariable(ManagedVariable managedVariable)
        {
            if (!managedVariable.IsValueXP())
                return;

            if (!Connector.IsReadyProcess)
                return;

            if (ContainsAddress(managedVariable.Address, out int index))
                return;

            int nextIndex = NextIndex;
            bool isCached = index != -1;
            if (isCached)
            {
                Logger.Debug($"Reusing Index '{index}' for Address '{managedVariable.Address}'");
                nextIndex = index;
            }

            if (managedVariable is VariableString)
            {
                SubscribeVariableString(managedVariable as VariableString, isCached, nextIndex);
                return;
            }

            var mapping = IndexMappingXP.Create(nextIndex, managedVariable);
            SubscribeMapping(mapping);
            if (!isCached)
                NextIndex++;
        }

        protected virtual void SubscribeVariableString(VariableString managedVariable, bool isCached, int index, int rate = -1)
        {
            int baseIndex = (isCached ? index : NextIndex);
            string baseRef = managedVariable.Address.Name;
            if (!managedVariable.Address.HasParameter || !int.TryParse(managedVariable.Address.Parameter[2..], out int length))
            {
                Logger.Error($"Could not parse String Length for '{managedVariable.Address}'");
                return;
            }

            List<string> dataRefs = [];
            List<int> indices = [];
            List<IndexMappingXP> mappings = [];
            string dataRef;
            for (int c = 0; c < length; c++)
            {
                dataRef = $"{baseRef}[{c}]";
                dataRefs.Add(dataRef);
                indices.Add(baseIndex);
                mappings.Add(IndexMappingXP.CreateString(baseIndex, dataRef, c, managedVariable));
                baseIndex++;
            }

            SubscribeMappings([.. dataRefs], [.. indices], mappings, rate);
            if (!isCached)
                NextIndex = baseIndex;
        }

        public virtual void SubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!Connector.IsReadyProcess)
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            List<string> dataRefs = [];
            List<int> indices = [];
            List<IndexMappingXP> mappings = [];

            int count = 0;
            int baseIndex = NextIndex;
            int nextIndex;
            bool isCached;
            int index;
            foreach (var managedVariable in managedVariables.Where(m => m is not VariableString))
            {
                if (!managedVariable.IsValueXP())
                    continue;

                if (ContainsAddress(managedVariable.Address, out index))
                    continue;

                nextIndex = baseIndex;
                isCached = index != -1;
                if (isCached)
                {
                    Logger.Debug($"Reusing Index '{index}' for Address '{managedVariable.Address}'");
                    nextIndex = index;
                }

                dataRefs.Add(managedVariable.Address.Address);
                indices.Add(nextIndex);
                mappings.Add(IndexMappingXP.Create(nextIndex, managedVariable));
                if (!isCached)
                    baseIndex++;

                count++;
                if (count >= DatagramXP.XP_MAX_NUM_RREF)
                {
                    Logger.Warning($"XP_MAX_NUM_RREF ({DatagramXP.XP_MAX_NUM_RREF}) Limit reached");
                    break;
                }
            }

            SubscribeMappings([.. dataRefs], [.. indices], mappings);
            NextIndex = baseIndex;

            if (count >= DatagramXP.XP_MAX_NUM_RREF)
                return;

            foreach (var managedVariable in managedVariables.Where(m => m is VariableString))
            {
                if (!ContainsAddress(managedVariable.Address, out index) && managedVariable.IsValueXP())
                    SubscribeVariableString(managedVariable as VariableString, index != -1, index);
            }
        }

        protected virtual void SubscribeMapping(IndexMappingXP mapping, int rate = -1)
        {
            if (rate == -1)
                rate = (int)(1000 / (App.Configuration.IntervalSimProcess));

            IndexMapping.AddOrUpdate(mapping.SimIndex, mapping, (k, v) => v = mapping);
            Socket.SendSubscribe(mapping.Address, mapping.SimIndex, rate);
            mapping.ValueRef.IsSubscribed = true;
        }

        protected virtual void SubscribeMappings(string[] dataRefs, int[] indices, List<IndexMappingXP> mappings, int rate = -1)
        {
            if (rate == -1)
                rate = (int)(1000 / (App.Configuration.IntervalSimProcess));

            foreach (var mapping in mappings)
                IndexMapping.AddOrUpdate(mapping.SimIndex, mapping, (k, v) => v = mapping);

            Socket.SendSubscribe(dataRefs, indices, rate);
            mappings.ForEach(m => m.ValueRef.IsSubscribed = true);
        }

        public virtual void UnsubscribeVariable(ManagedVariable managedVariable)
        {
            if (!Connector.IsReadyProcess)
                return;

            if (!managedVariable.IsValueXP())
                return;

            if (!HasAddress(managedVariable.Address, out IndexMappingXP mapping))
                return;

            if (managedVariable is VariableString)
            {
                UnsubscribeValueString(managedVariable as VariableString);
                return;
            }

            UnsubscribeMapping(mapping);
        }

        protected void UnsubscribeValueString(VariableString managedVariable)
        {
            var query = IndexMapping.Where(m => m.Value.ValueRef.Address == managedVariable.Address);
            string[] dataRefs = [.. query.Select(m => m.Value.Address)];
            int[] indices = [.. query.Select(m => m.Value.SimIndex)];
            List<IndexMappingXP> mappings = [.. query.Select(m => m.Value)];

            UnsubscribeMappings(dataRefs, indices, mappings);
        }

        public virtual void UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!Connector.IsReadyProcess)
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            List<string> dataRefs = [];
            List<int> indices = [];
            List<IndexMappingXP> mappings = [];

            IndexMappingXP mapping;
            foreach (var managedVariable in managedVariables.Where(m => m is not VariableString))
            {
                if (!managedVariable.IsValueXP())
                    continue;

                if (!HasAddress(managedVariable.Address, out mapping))
                    continue;

                dataRefs.Add(mapping.Address);
                indices.Add(mapping.SimIndex);
                mappings.Add(mapping);
            }
            UnsubscribeMappings([.. dataRefs], [.. indices], mappings);

            foreach (var managedVariable in managedVariables.Where(m => m is VariableString))
            {
                if (managedVariable.IsValueXP() && HasAddress(managedVariable.Address, out mapping))
                    UnsubscribeValueString(managedVariable as VariableString);
            }
        }

        protected virtual void UnsubscribeMapping(IndexMappingXP mapping)
        {
            Socket.SendUnsubscribe(mapping.Address, mapping.SimIndex);

            mapping.IsRequested = false;
            mapping.ValueRef.IsSubscribed = false;
        }

        protected virtual void UnsubscribeMappings(string[] dataRefs, int[] indices, List<IndexMappingXP> mappings)
        {
            Socket.SendUnsubscribe(dataRefs, indices);

            foreach (var mapping in mappings)
            {
                mapping.IsRequested = false;
                mapping.ValueRef.IsSubscribed = false;
            }
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
                        if (Socket.SendCommand(xpcmd))
                        {
                            success++;
                            if (command.CommandDelay > 0)
                                await Task.Delay(command.CommandDelay, App.CancellationToken);
                        }
                    }
                }
                else if (xpcmds.Length == 1)
                {
                    if (Socket.SendCommand(command.Address.Address))
                        success++;
                }
                else
                {
                    Logger.Error($"Command-Array has zero members! (Address: {command?.Address})");
                }
            }

            return success == xpcmds.Length * command.Ticks;
        }

        public virtual async Task<bool> WriteRef(SimCommand command)
        {
            if (command.Address.Parameter.StartsWith('s'))
                return await WriteStringRef(command);

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = Socket.SendWriteRef(command.Address.Address, Convert.ToSingle(command.NumValue));
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (Socket.SendWriteRef(command.Address.Address, Convert.ToSingle(command.ResetValue)) && result)
                        success++;
                }
                else if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }

        protected virtual async Task<bool> WriteStringRef(SimCommand command)
        {
            int success = 0;
            int i;
            string baseAddress = command.Address.Name;
            int size = (int)Conversion.ToDouble(command.Address.Parameter.Replace("s", ""));

            for (i = 0; i < command.Ticks; i++)
            {
                for (int n = 0; n < size; n++)
                    if (Socket.SendWriteRef($"{baseAddress}[{n}]", command.Value[n]))
                        success++;

                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    for (int n = 0; n < size; n++)
                        if (Socket.SendWriteRef($"{baseAddress}[{n}]", command.ResetValue[n]))
                            success++;
                }
            }

            if (command.IsValueReset && command.ResetDelay > 0)
                size *= 2;
            return success == command.Ticks * size;
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Socket?.Dispose();
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
