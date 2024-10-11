using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.XP;
using PilotsDeck.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PilotsDeck.Simulator
{
    public class ConnectorXP : ISimConnector
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

        public SimulatorType Type { get; } = SimulatorType.XP;
        public SimulatorState SimState { get; protected set; } = SimulatorState.RUNNING;
        public bool IsPrimary { get; set; } = true;
        public bool IsRunning { get { return ISimConnector.GetRunning(Type) || RemoteRunning; } }
        public bool IsLoading { get; protected set; } = true;
        public bool IsReadyProcess { get { return ReceivedAircraftString && IsRunning && !IsLoading && !SocketError; } }
        public bool IsReadyCommand { get { return IsReadyProcess && !IsPaused && !SocketError; } }
        public bool IsReadySession { get { return !IsLoading && !SocketError; } }
        public bool IsPaused { get; protected set; } = true;
        public string AircraftString { get; protected set; } = "";
        protected bool RemoteRunning { get; set; } = false;
        public bool IsCanceled { get; protected set; } = false;
        protected CancellationTokenSource ReceiveTokenSource { get; set; } = new();      
        protected bool ReceivedAircraftString { get; set; } = false;
        protected bool RefsInternalSubscribed { get; set; } = false;
        protected bool RefAircraftSubscribed { get; set; } = false;
        public int SimVersion { get; protected set; } = 0;
        protected SocketXP Socket { get; set; }
        protected bool SocketError { get; set; } = false;
        protected ConcurrentDictionary<int, IndexMappingXP> IndexMapping { get; set; } = [];
        protected int NextIndex { get; set; } = INDEX_BASE_DYNAMIC;
        protected DateTime LastMessageReceived {  get; set; } = DateTime.Now;
        protected DispatcherTimer TimerCheckLoading { get; } = new();
        public Dictionary<string, List<ISimConnector.EventRegistration>> RegisteredEvents { get; private set; } = [];
        protected ConcurrentDictionary<int, int> UnknownIndices { get; private set; } = [];

        public ConnectorXP()
        {
            Socket = new(this);
        }

        public static async Task<bool> CheckRemoteXP(SimulatorType type)
        {
            if (type != SimulatorType.XP || App.Configuration.XPlaneIP == "127.0.0.1")
                return false;

            bool result = false;
            using UdpClient udpSend = new();
            try
            {
                Logger.Verbose($"XP Remote check");
                udpSend.Connect(new IPEndPoint(App.Configuration.ParsedXPlaneIP, App.Configuration.XPlanePort));
                var ipEndPoint = (IPEndPoint)udpSend.Client.LocalEndPoint;
                using UdpClient udpReceive = new(ipEndPoint);

                var sendResult = await udpSend.SendAsync(DatagramXP.MessageRequest(ConnectorXP.TIMEPAUSED_REF, ConnectorXP.TIMEPAUSED_INDEX, (int)(1000 / (App.Configuration.IntervalSimProcess))));
                Logger.Verbose($"XP Remote check: send RREF");

                var cts = new CancellationTokenSource(App.Configuration.XPlaneRemoteCheckTimeout);
                Logger.Verbose($"XP Remote check: receive Answer");
                var response = await udpReceive.ReceiveAsync(cts.Token);
                Logger.Verbose($"XP Remote check: Answer received");
                var header = DatagramXP.GetHeader(response.Buffer);
                if (header == DatagramXP.XP_STR_RREF)
                {
                    Logger.Verbose($"XP Remote check: Header is RREF");
                    result = true;
                }
                else
                    Logger.Verbose($"XP Remote check: Header is Unkown");

                udpReceive?.Close();
                udpReceive?.Dispose();
            }
            catch { }
            finally
            {
                udpSend?.Close();
                udpSend?.Dispose();
            }

            return result;
        }

        public async void Run()
        {
            try
            {
                Socket = new(this);

                TimerCheckLoading.Interval = TimeSpan.FromMilliseconds(App.Configuration.XPlaneStateCheckInterval);
                TimerCheckLoading.Tick += CheckLoadingTask;

                if (App.Configuration.XPlaneIP != "127.0.0.1")
                    RemoteRunning = true;

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
                        while (count < App.Configuration.XPlaneRetryDelay && !IsCanceled && IsRunning);

                        SocketError = false;

                        if (IsRunning && !IsCanceled)
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
                while (IsRunning && !App.CancellationToken.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            TimerCheckLoading?.Stop();
            Logger.Information($"ConnectorXP Task ended");
        }

        protected void CheckLoadingTask(object sender, EventArgs e)
        {
            if (!IsRunning || App.CancellationTokenSource.IsCancellationRequested || ReceiveTokenSource.IsCancellationRequested)
            {
                TimerCheckLoading.Stop();
                if (App.CancellationTokenSource.IsCancellationRequested)
                    ReceiveTokenSource.Cancel();
                return;
            }

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
                RemoteRunning = false;
                ReceiveTokenSource.Cancel();
            }

            if (SimState == SimulatorState.RUNNING && Socket.IsConnected && RefsInternalSubscribed)
            {
                Logger.Debug("Changed to LOADING");
                SimState = SimulatorState.LOADING;
            }

            if (SimState == SimulatorState.LOADING && !IsLoading && !string.IsNullOrWhiteSpace(AircraftString) && !IsPaused)
            {
                Logger.Debug("Changed to SESSION");
                SimState = SimulatorState.SESSION;
            }

            if (SimState == SimulatorState.SESSION && IsLoading && string.IsNullOrWhiteSpace(AircraftString))
            {
                Logger.Debug("Changed to LOADING");
                SimState = SimulatorState.LOADING;
            }

            if (SimState >= SimulatorState.LOADING && (!IsRunning && App.Configuration.XPlaneIP == "127.0.0.1") || (!RemoteRunning && App.Configuration.XPlaneIP != "127.0.0.1"))
            {
                Logger.Debug("Changed to STOPPED");
                SimState = SimulatorState.STOPPED;
            }
        }

        public void Stop()
        {
            foreach (var variable in App.PluginController.VariableManager.VariableList.Where(v => v.IsValueXP()))
                variable.IsSubscribed = false;
            
            Socket?.Close();
            ReceiveTokenSource?.Cancel();
            IsCanceled = true;
        }

        protected void ClearState()
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

        protected async Task ReceiveAsync()
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
                    ProcessReceived(index, value);
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

                            if (SimVersion >= VERSIONXP12)
                            {
                                Logger.Information("Subscribing Aircraft DataRef for Plugin (XP 12.0)");
                                SubscribeValueInternal(AIRCRAFT_XP12_REF, AIRCRAFT_INDEX, 1, true);
                            }
                            else if (SimVersion >= VERSIONXP95)
                            {
                                Logger.Information("Subscribing Aircraft DataRef for Plugin (XP 9.5)");
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
                        TimerCheckLoading.Start();
                        IsLoading = false;
                    }

                    if (IndexMapping.TryGetValue(index, out var aircraftMapping))
                    {
                        if (index == AIRCRAFT_INDEX)
                            (aircraftMapping.ValueRef as VariableString).SetValue("");
                        (aircraftMapping.ValueRef as VariableString).SetChar(aircraftMapping.CharIndex, value);

                        if (index == INDEX_BASE_DYNAMIC - 1 && aircraftMapping.ValueRef.Value != AircraftString)
                        {
                            AircraftString = aircraftMapping.ValueRef.Value;
                            Logger.Information($"Aircraft String changed to '{AircraftString}'");
                        }
                    }
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

        public void Process()
        {

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
                VariableNumeric managedVariable = new(dataref, SimValueType.XPDREF)
                {
                    IsSubscribed = true
                };

                SubscribeMapping(IndexMappingXP.Create(index, managedVariable), rate);
            }
            else
            {
                VariableString managedVariable = new(dataref, SimValueType.XPDREF)
                {
                    IsSubscribed = true
                };

                SubscribeVariableString(managedVariable, true, index, rate);
            }
        }

        protected bool ContainsAddress(string address, out int index)
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

        public void SubscribeVariable(ManagedVariable managedVariable)
        {
            if (!managedVariable.IsValueXP())
                return;

            if (!IsReadyProcess)
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

        protected void SubscribeVariableString(VariableString managedVariable, bool isCached, int index, int rate = -1)
        {
            int baseIndex = (isCached ? index : NextIndex);
            string baseRef = managedVariable.Address.Split(':')[0];
            if (!int.TryParse(managedVariable.Address.Split(':')[1][1..], out int length))
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

        public void SubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!IsReadyProcess)
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

                dataRefs.Add(managedVariable.Address);
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

        protected void SubscribeMapping(IndexMappingXP mapping, int rate = -1)
        {
            if (rate == -1)
                rate = (int)(1000 / (App.Configuration.IntervalSimProcess));

            IndexMapping.AddOrUpdate(mapping.SimIndex, mapping, (k, v) => v = mapping);
            Socket.SendSubscribe(mapping.Address, mapping.SimIndex, rate);
            mapping.ValueRef.IsSubscribed = true;
        }

        protected void SubscribeMappings(string[] dataRefs, int[] indices, List<IndexMappingXP> mappings, int rate = -1)
        {
            if (rate == -1)
                rate = (int)(1000 / (App.Configuration.IntervalSimProcess));

            foreach (var mapping in mappings)
                IndexMapping.AddOrUpdate(mapping.SimIndex, mapping, (k, v) => v = mapping);

            Socket.SendSubscribe(dataRefs, indices, rate);
            mappings.ForEach(m => m.ValueRef.IsSubscribed = true);
        }

        protected bool HasAddress(string address, out IndexMappingXP mapping)
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

        public void UnsubscribeVariable(ManagedVariable managedVariable)
        {
            if (!IsReadyProcess)
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
            string[] dataRefs = query.Select(m => m.Value.Address).ToArray();
            int[] indices = query.Select(m => m.Value.SimIndex).ToArray();
            List<IndexMappingXP> mappings = query.Select(m => m.Value).ToList();

            UnsubscribeMappings(dataRefs, indices, mappings);
        }

        public void UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!IsReadyProcess)
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

        public void RemoveUnusedVariables(bool force)
        {

        }

        protected void UnsubscribeMapping(IndexMappingXP mapping)
        {
            Socket.SendUnsubscribe(mapping.Address, mapping.SimIndex);

            mapping.IsRequested = false;
            mapping.ValueRef.IsSubscribed = false;
        }

        protected void UnsubscribeMappings(string[] dataRefs, int[] indices, List<IndexMappingXP> mappings)
        {
            Socket.SendUnsubscribe(dataRefs, indices);

            foreach (var mapping in mappings)
            {
                mapping.IsRequested = false;
                mapping.ValueRef.IsSubscribed = false;
            }
        }

        public bool SubscribeSimEvent(string evtName, string receiverID, ISimConnector.EventCallback callbackFunction)
        {
            return false;
        }

        public bool UnsubscribeSimEvent(string evtName, string receiverID)
        {
            return false;
        }

        public static bool IsCommandXP(SimCommandType? type)
        {
            return type == SimCommandType.XPCMD || type == SimCommandType.XPWREF;
        }

        public bool CanRunCommand(SimCommand command)
        {
            return IsCommandXP(command?.Type);
        }

        public async Task<bool> RunCommand(SimCommand command)
        {
            if (!IsReadyCommand)
                return false;

            bool result = false;
            
            if (command.Type == SimCommandType.XPCMD)
            {
                result = await SendCommand(command);
            }
            else if (command.Type == SimCommandType.XPWREF)
            {
                result = await WriteRef(command);
            }

            return result;
        }

        protected async Task<bool> SendCommand(SimCommand command)
        {
            string[] xpcmds = command.Address.Split(':');

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
                    if(Socket.SendCommand(command.Address))
                        success++;
                }
                else
                {
                    Logger.Error($"Command-Array has zero members! (Address: {command?.Address})");
                }
            }

            return success == xpcmds.Length * command.Ticks;
        }

        protected async Task<bool> WriteRef(SimCommand command)
        {
            if (command.Address.Contains(":s"))
                return await WriteStringRef(command);

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = Socket.SendWriteRef(command.Address, Convert.ToSingle(command.NumValue));
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (Socket.SendWriteRef(command.Address, Convert.ToSingle(command.ResetValue)) && result)
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

        protected async Task<bool> WriteStringRef(SimCommand command)
        {
            int success = 0;
            int i;
            string[] parts = command.Address.Split(':');
            string baseAddress = parts[0];
            int size = (int)Conversion.ToDouble(parts[1].Replace("s", ""));

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
                    Socket = null;
                    Logger.Debug("Sockets released");
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
