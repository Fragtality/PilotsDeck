using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace PilotsDeck
{
    public class MobiSimConnect(IPCManager manager, ConnectorMSFS.EventCallback callbackFunc) : IDisposable
    {
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;

        public const uint WM_PILOTSDECK_SIMCONNECT = 0x1984;
        public const string CLIENT_NAME = "PilotsDeck";
        public const string PILOTSDECK_CLIENT_DATA_NAME_SIMVAR = $"{CLIENT_NAME}.LVars";
        public const string PILOTSDECK_CLIENT_DATA_NAME_COMMAND = $"{CLIENT_NAME}.Command";
        public const string PILOTSDECK_CLIENT_DATA_NAME_RESPONSE = $"{CLIENT_NAME}.Response";

        protected SimConnect simConnect = null;
        protected IntPtr simConnectHandle = IntPtr.Zero;
        protected Thread simConnectThread = null;
        private static bool cancelThread = false;

        protected bool isSimConnected = false;
        protected bool isMobiConnected = false;
        protected bool isReceiveRunning = false;
        protected bool eventsEnumerated = false;
        public bool IsConnected { get { return isSimConnected && isMobiConnected; } }
        public bool IsReady { get { return IsConnected && isReceiveRunning; } }
        public bool HasReceiveError { get { return !isReceiveRunning; } }

        protected uint nextID = 1;
        protected Dictionary<string, uint> addressToIndex = [];
        protected Dictionary<uint, bool> simVarUsed = [];
        protected Dictionary<uint, IPCValue> simVars = [];
        protected Dictionary<uint, bool> simVarFirstUpdateDone = [];
        protected List<uint> subscribeQueue = [];
        protected IPCManager ipcManager = manager;
        protected Dictionary<string, ulong> eventHashes = [];
        protected uint nextEventID = 1;
        protected Dictionary<uint, ulong> eventIndex = [];
        protected Dictionary<ulong, IPCValueInputEvent> eventSubscribedValues = [];
        protected Dictionary<uint, string> simEvents = [];
        protected uint nextSimEventID = 1;
        protected ConnectorMSFS.EventCallback eventCallback = callbackFunc;

        public bool Connect()
        {
            try
            {
                if (isSimConnected)
                    return true;
                
                simConnect = new SimConnect(CLIENT_NAME, simConnectHandle, WM_PILOTSDECK_SIMCONNECT, null, 0);
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnQuit);
                simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(SimConnect_OnReceiveEvent);
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnException);

                cancelThread = false;
                simConnectThread = new(new ThreadStart(SimConnect_ReceiveThread))
                {
                    IsBackground = true
                };
                simConnectHandle = new IntPtr(simConnectThread.ManagedThreadId);
                simConnectThread.Start();

                Logger.Log(LogLevel.Information, "MobiSimConnect:Connect", $"SimConnect Connection open.");
                return true;
            }
            catch (Exception ex)
            {
                simConnectThread = null;
                simConnectHandle = IntPtr.Zero;
                cancelThread = true;
                simConnect = null;

                Logger.Log(LogLevel.Critical, "MobiSimConnect:Connect", $"Exception while opening SimConnect! (Exception: {ex.GetType()})");
            }

            return false;
        }

        protected void SimConnect_OnOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            try
            {
                isSimConnected = true;
                simConnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(SimConnect_OnClientData);
                simConnect.OnRecvEnumerateInputEvents += new SimConnect.RecvEnumerateInputEventsEventHandler(SimConnect_OnRecvEnumerateInputEvents);
                simConnect.OnRecvGetInputEvent += new SimConnect.RecvGetInputEventEventHandler(SimConnect_OnRecvGetInputEvents);
                CreateDataAreaDefaultChannel();
                Logger.Log(LogLevel.Information, "MobiSimConnect:SimConnect_OnOpen", $"SimConnect OnOpen received.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SimConnect_OnOpen", $"Exception during SimConnect OnOpen! (Exception: {ex.GetType()})");
            }
        }

        protected void SimConnect_ReceiveThread()
        {
            ulong ticks = 0;
            int delay = 100;
            int repeat = 5000 / delay;
            int errors = 0;
            isReceiveRunning = true;
            while (!cancelThread && simConnect != null && isReceiveRunning)
            {
                try
                {
                    simConnect.ReceiveMessage();

                    if (isSimConnected && !isMobiConnected && ticks % (ulong)repeat == 0)
                    {
                        Logger.Log(LogLevel.Debug, "MobiSimConnect:SimConnect_ReceiveThread", $"Sending Ping to MobiFlight WASM Module.");
                        SendMobiWasmCmd("MF.DummyCmd");
                        SendMobiWasmCmd("MF.Ping");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors > 10)
                    {
                        isReceiveRunning = false;
                        Logger.Log(LogLevel.Critical, "MobiSimConnect:SimConnect_ReceiveThread", $"Maximum Errors reached, closing Receive Thread! (Exception: {ex.GetType()})");
                        return;
                    }
                }
                Thread.Sleep(delay);
                ticks++;
            }
            isReceiveRunning = false;
            return;
        }

        protected void CreateDataAreaDefaultChannel()
        {
            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD);

            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.AddToClientDataDefinition((SIMCONNECT_DEFINE_ID)0, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>((SIMCONNECT_DEFINE_ID)0);
            simConnect.RequestClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)0,
                (SIMCONNECT_DEFINE_ID)0,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        protected void CreateDataAreaClientChannel()
        {
            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_COMMAND, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_RESPONSE, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_SIMVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS);

            simConnect.AddToClientDataDefinition((SIMCONNECT_DEFINE_ID)0, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);
            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>((SIMCONNECT_DEFINE_ID)0);
            simConnect.RequestClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)0,
                (SIMCONNECT_DEFINE_ID)0,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        public void SubscribeSimConnectEvent(string evtName)
        {
            try
            {
                if (simEvents.ContainsValue(evtName))
                {
                    Logger.Log(LogLevel.Warning, "MobiSimConnect:SubscribeSimConnectEvent", $"The Event '{evtName}' is already subscribed!");
                    return;
                }

                simConnect.MapClientEventToSimEvent((SIM_EVENTS)nextSimEventID, evtName);
                simConnect.AddClientEventToNotificationGroup(NOTFIY_GROUP.DYNAMIC, (SIM_EVENTS)nextSimEventID, false);
                simEvents.Add(nextSimEventID, evtName);

                Logger.Log(LogLevel.Debug, "MobiSimConnect:SubscribeSimConnectEvent", $"Event '{evtName}' subscribed with ID '{nextSimEventID}'");
                nextSimEventID++;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:UnsubscribeSimConnectEvent", $"Exception '{ex.GetType}' while subscribing Event '{evtName}': {ex.Message}");
            }
        }

        public void UnsubscribeSimConnectEvent(string evtName)
        {
            try
            {
                if (!simEvents.ContainsValue(evtName))
                {
                    Logger.Log(LogLevel.Debug, "MobiSimConnect:UnsubscribeSimConnectEvent", $"The Event '{evtName}' is not subscribed!");
                    return;
                }
                uint id = simEvents.Where(kv => kv.Value == evtName).FirstOrDefault().Key;

                simConnect.RemoveClientEvent(NOTFIY_GROUP.DYNAMIC, (SIM_EVENTS)id);
                simEvents.Remove(id);
                Logger.Log(LogLevel.Debug, "MobiSimConnect:UnsubscribeSimConnectEvent", $"Event '{evtName}' with ID '{nextEventID}' is now unsubscribed");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:UnsubscribeSimConnectEvent", $"Exception '{ex.GetType}' while unsubscribing Event '{evtName}': {ex.Message}");
            }
        }

        public void RemoveAllSimConnectEvents()
        {
            try
            {
                foreach (var evt in simEvents)
                {
                    simConnect.RemoveClientEvent(NOTFIY_GROUP.DYNAMIC, (SIM_EVENTS)evt.Key);
                }
                simEvents.Clear();
                nextSimEventID = 1;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:RemoveAllSimConnectEvents", $"Exception '{ex.GetType}' while unsubscribing Events: {ex.Message}");
            }
        }

        protected void SimConnect_OnReceiveEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            try
            {
                if (recEvent != null && recEvent.uGroupID == (uint)NOTFIY_GROUP.DYNAMIC)
                {
                    if (simEvents.TryGetValue(recEvent.uEventID, out string evtName))
                        eventCallback(evtName, recEvent.dwData);
                    else
                        Logger.Log(LogLevel.Warning, "MobiSimConnect:SimConnect_OnReceiveEvent", $"Event ID '{recEvent.uEventID}' is not subscribed!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SimConnect_OnReceiveEvent", $"Exception '{ex.GetType}' while receiving Events: {ex.Message} (eventID: {recEvent?.uEventID})");
            }
        }

        protected void SimConnect_OnClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            try
            {
                if (data.dwRequestID == 0)
                {
                    var request = (ResponseString)data.dwData[0];
                    if (request.Data == "MF.Pong")
                    {
                        if (!isMobiConnected)
                        {
                            Logger.Log(LogLevel.Information, "MobiSimConnect:SimConnect_OnClientData", $"MobiFlight WASM Ping acknowledged - opening Client Connection.");
                            SendMobiWasmCmd($"MF.Clients.Add.{CLIENT_NAME}");
                        }
                    }
                    if (request.Data == $"MF.Clients.Add.{CLIENT_NAME}.Finished")
                    {
                        CreateDataAreaClientChannel();
                        isMobiConnected = true;
                        SendClientWasmCmd("MF.SimVars.Clear");
                        SendClientWasmCmd("MF.Config.MAX_VARS_PER_FRAME.Set.30");
                        Logger.Log(LogLevel.Information, "MobiSimConnect:SimConnect_OnClientData", $"MobiFlight WASM Client Connection opened.");
                    }
                }
                else
                {
                    var simData = (ClientDataValue)data.dwData[0];
                    if (simVars.TryGetValue(data.dwRequestID, out IPCValue ipcValue))
                    {
                        if (ipcValue != null)
                        {
                            bool updateDone = simVarFirstUpdateDone[data.dwRequestID];
                            if (updateDone || (!updateDone && simData.data != 0.0f))
                            {
                                ipcValue.SetValue(simData.data);
                                if (!updateDone)
                                    simVarFirstUpdateDone[data.dwRequestID] = true;
                            }
                            else
                            {
                                Logger.Log(LogLevel.Debug, "MobiSimConnect:SimConnect_OnClientData", $"The Value for ID '{data.dwRequestID}' was ignored - first Update (Value: {simData.data})");
                                simVarFirstUpdateDone[data.dwRequestID] = true;
                            }
                        }
                        else
                            Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnClientData", $"The Value for ID '{data.dwRequestID}' is NULL! (Value: {simData.data})");
                    }
                    else
                        Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnClientData", $"The received ID '{data.dwRequestID}' is not subscribed! (Value: {simData.data})");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SimConnect_OnClientData", $"Exception during SimConnect OnClientData! (Exception: {ex.GetType()}) (Data: {data})");
            }
        }

        public void EnumerateInputEvents()
        {
            Logger.Log(LogLevel.Debug, "MobiSimConnect:EnumerateInputEvents", $"Sending EnumerateInput Request");
            eventsEnumerated = false;
            eventHashes.Clear();
            eventIndex.Clear();
            nextEventID = 1;
            eventSubscribedValues.Clear();
            simConnect.EnumerateInputEvents(SIMCONNECT_REQUEST_ID.EnumEvents);
        }

        public void RequestInputEventValues()
        {
            if (!eventsEnumerated)
                return;

            foreach (var evt in eventSubscribedValues)
            {
                Logger.Log(LogLevel.Verbose, "MobiSimConnect:RequestInputEventValues", $"Requesting InputEvent '{evt.Value.Address}' (#{evt.Value.Hash}) on ID '{evt.Key}'");
                simConnect.GetInputEvent((SIMCONNECT_DEFINE_ID)evt.Key, evt.Value.Hash);
            }
        }

        public void SubscribeInputEvent(string name)
        {
            if (eventHashes.TryGetValue(name, out ulong hash) && ipcManager.TryGetValue(name, out IPCValue value))
            {
                if (!eventSubscribedValues.TryAdd(eventIndex.First(kv => kv.Value == hash).Key, value as IPCValueInputEvent))
                    Logger.Log(LogLevel.Warning, "MobiSimConnect:SubscribeInputEvent", $"Failed to add InputEvent '{name}' to subscribed Values!");
                else
                    (value as IPCValueInputEvent).Hash = hash;
            }
            else
            {
                Logger.Log(LogLevel.Warning, "MobiSimConnect:SubscribeInputEvent", $"No Hash available for InputEvent '{name}'");
            }
        }

        public void UnsubscribeInputEvent(string name)
        {
            if (eventSubscribedValues.Any(kv => kv.Value.Address == name))
            {
                ulong hash = eventSubscribedValues.First(kv => kv.Value.Address == name).Key;
                if (eventSubscribedValues.Remove(hash))
                    Logger.Log(LogLevel.Debug, "MobiSimConnect:UnsubscribeInputEvent", $"InputEvent '{name}' removed from subscribed Values");
                else
                    Logger.Log(LogLevel.Warning, "MobiSimConnect:UnsubscribeInputEvent", $"Could not remove InputEvent '{name}' from subscribed Values!");
            }
            else
            {
                Logger.Log(LogLevel.Warning, "MobiSimConnect:UnsubscribeInputEvent", $"InputEvent '{name}' not in subscribed Values!");
            }
        }

        public bool SendInputEvent(string name, double value)
        {
            if (eventsEnumerated && eventHashes.TryGetValue(name, out ulong hash))
            {
                simConnect.SetInputEvent(hash, value);
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Warning, "MobiSimConnect:SendInputEvent", $"No Hash available for InputEvent '{name}'");
            }

            return false;
        }

        protected void SimConnect_OnRecvEnumerateInputEvents(SimConnect sender, SIMCONNECT_RECV_ENUMERATE_INPUT_EVENTS data)
        {
            if (data.dwRequestID == (uint)SIMCONNECT_REQUEST_ID.EnumEvents)
            {
                foreach (SIMCONNECT_INPUT_EVENT_DESCRIPTOR evt in data.rgData.Cast<SIMCONNECT_INPUT_EVENT_DESCRIPTOR>())
                {
                    if (evt.eType == SIMCONNECT_INPUT_EVENT_TYPE.DOUBLE)
                    {
                        eventHashes.Add("B:" + evt.Name, evt.Hash);
                        eventIndex.Add(nextEventID, evt.Hash);
                        Logger.Log(LogLevel.Debug, "MobiSimConnect:SimConnect_OnRecvEnumerateInputEvents", $"Event '{evt.Name}' has Hash: {evt.Hash} (Type: {evt.eType}) on ID {nextEventID}");
                        nextEventID++;
                    }
                }
                eventsEnumerated = true;
                SubscribeAllAddresses(true);
            }
        }

        protected void SimConnect_OnRecvGetInputEvents(SimConnect sender, SIMCONNECT_RECV_GET_INPUT_EVENT data)
        {
            if (eventSubscribedValues.TryGetValue(data.dwRequestID, out IPCValueInputEvent value))
            {
                value.SetValue((double)data.Value[0]);
            }
        }

        protected void SimConnect_OnQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            try
            {
                if (isMobiConnected)
                    SendClientWasmCmd("MF.SimVars.Clear");

                RemoveAllSimConnectEvents();

                cancelThread = true;
                if (simConnectThread != null)
                {
                    simConnectThread.Interrupt();
                    simConnectThread.Join(500);
                    simConnectThread = null;
                }

                if (simConnect != null)
                {
                    simConnect.Dispose();
                    simConnect = null;
                    simConnectHandle = IntPtr.Zero;
                }

                isSimConnected = false;
                isMobiConnected = false;

                nextID = 1;
                simVars.Clear();
                simVarUsed.Clear();
                simVarFirstUpdateDone.Clear();
                subscribeQueue.Clear();
                addressToIndex.Clear();
                eventHashes.Clear();
                eventIndex.Clear();
                eventSubscribedValues.Clear();
                nextEventID = 1;
                eventsEnumerated = false;
                nextSimEventID = 1;
                simEvents.Clear();
                Logger.Log(LogLevel.Information, "MobiSimConnect:Disconnect", $"SimConnect Connection closed.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:Disconnect", $"Exception during disconnecting from SimConnect! (Exception: {ex.GetType()})");
            }
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        private void SendClientWasmCmd(string command)
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendClientWasmDummyCmd()
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, "MF.DummyCmd");
        }

        private void SendMobiWasmCmd(string command)
        {
            SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, (MOBIFLIGHT_CLIENT_DATA_ID)0, command);
        }

        private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }

        protected void SimConnect_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            if (data.dwException != 3 && data.dwException != 29)
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SimConnect_OnException", $"Exception received: (Exception: {data.dwException})");
        }

        public void SubscribeAddress(string address)
        {
            try
            {
                if (!IPCTools.rxAvar.IsMatch(address) && !IPCTools.rxLvar.IsMatch(address))
                {
                    Logger.Log(LogLevel.Error, "MobiSimConnect:SubscribeAddress", $"The Address '{address}' is not valid for MobiFlight!");
                    return;
                }
                if (nextID == 9999)
                {
                    Logger.Log(LogLevel.Critical, "MobiSimConnect:SubscribeAddress", $"Maximum Subscription Limit reached, can not subscribe any more SimVars!");
                    return;
                }

                if (addressToIndex.TryAdd(address, nextID))
                {
                    simVars.Add(nextID, ipcManager[address]);
                    simVarFirstUpdateDone.Add(nextID, false);
                    simVarUsed.Add(nextID, true);

                    subscribeQueue.Add(nextID);
                    nextID++;
                }
                else if (addressToIndex.TryGetValue(address, out uint index) && !simVarUsed[index])
                {
                    simVarUsed[index] = true;
                    simVars[index] = ipcManager[address];
                    simVarFirstUpdateDone[index] = true;
                }
                else
                    Logger.Log(LogLevel.Error, "MobiSimConnect:SubscribeAddress", $"The Address '{address}' is already subscribed!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SubscribeAddress", $"Exception while subscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public void Process(bool requestInputEvents)
        {
            if (requestInputEvents && IsReady)
            {
                if (!eventsEnumerated)
                {
                    Logger.Log(LogLevel.Error, "MobiSimConnect:Process", $"Sim is Ready and Events not enumerated - running Enumeration");
                    EnumerateInputEvents();
                }
                else
                    RequestInputEventValues();
            }
            else if (!requestInputEvents && IsReady && eventsEnumerated)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:Process", $"Sim is not Ready and Events are enumerated - reset Enumeration State");
                eventsEnumerated = false;
            }

            SubscribeQueue();
        }

        public void SubscribeAllAddresses(bool bVarOnly = false)
        {
            foreach (var address in ipcManager.AddressList)
            {
                if (IPCTools.rxBvar.IsMatch(address))
                    SubscribeInputEvent(address);
                if (!bVarOnly && ((IPCTools.rxLvar.IsMatch(address) && !AppSettings.Fsuipc7LegacyLvars) || IPCTools.rxAvar.IsMatch(address)) && !IPCTools.rxOffset.IsMatch(address))
                {
                    SubscribeAddress(address);
                }
            }
            Logger.Log(LogLevel.Information, "MobiSimConnect:SubscribeAllAddresses", $"Subscribed all IPCValues.");
        }

        protected void RegisterVariable(uint ID, string address)
        {
            Logger.Log(LogLevel.Debug, "MobiSimConnect:RegisterVariable", $"Register ID '{ID}' for Address '{address}'");
            simConnect.AddToClientDataDefinition(
                (SIMCONNECT_DEFINE_ID)ID,
                (ID - 1) * sizeof(float),
                sizeof(float),
                0,
                0);

            simConnect?.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataValue>((SIMCONNECT_DEFINE_ID)ID);

            simConnect?.RequestClientData(
                PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS,
                (SIMCONNECT_REQUEST_ID)ID,
                (SIMCONNECT_DEFINE_ID)ID,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0
            );

            address = IPCManager.FormatAddress(address, true);
            SendClientWasmCmd($"MF.SimVars.Add.{address}");
        }

        protected void SubscribeQueue()
        {
            int registered = 0;
            foreach (uint index in subscribeQueue)
            {
                if (simVars.TryGetValue(index, out IPCValue value))
                {
                    RegisterVariable(index, value.Address);
                    registered++;
                }
            }

            if (subscribeQueue.Count > 0)
            {
                Logger.Log(LogLevel.Information, "MobiSimConnect:SubscribeQueue", $"Subscribed {registered} / {subscribeQueue.Count} SimVars from Queue.");
                subscribeQueue.Clear();
            }
        }

        public void UnsubscribeAddress(string address)
        {
            try
            {
                if (!IPCTools.rxAvar.IsMatch(address) && !IPCTools.rxLvar.IsMatch(address))
                {
                    Logger.Log(LogLevel.Error, "MobiSimConnect:UnsubscribeAddress", $"The Address '{address}' is not valid for MobiFlight!");
                    return;
                }

                if (addressToIndex.TryGetValue(address, out uint index) && simVarUsed[index])
                {
                    simVarUsed[index] = false;
                    simVarFirstUpdateDone[index] = false;
                }
                else
                    Logger.Log(LogLevel.Error, "MobiSimConnect:UnsubscribeAddress", $"The Address '{address}' does not exist or is already set to unused!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:UnsubscribeAddress", $"Exception while unsubscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public void UnsubscribeUnusedAddresses()
        {
            ReorderRegistrations();
        }

        protected void ReorderRegistrations()
        {
            try
            {
                var unusedIndices = simVarUsed.Where(flag => !flag.Value).ToList();
                if (unusedIndices.Count == 0)
                    return;
                
                Logger.Log(LogLevel.Information, "MobiSimConnect:ReorderRegistrations", $"Reordering Registrations with MobiFlight WASM Module... (Unused: {unusedIndices.Count})");                
                foreach (var unusedIndex in unusedIndices)
                {
                    simVarUsed.Remove(unusedIndex.Key);
                    simVars.Remove(unusedIndex.Key);
                    simVarFirstUpdateDone.Remove(unusedIndex.Key);
                    string address = addressToIndex.Where(mapping => mapping.Value == unusedIndex.Key).FirstOrDefault().Key;
                    addressToIndex.Remove(address);
                }

                var usedAddresses = addressToIndex.Keys.ToList();
                simVarUsed.Clear();
                simVars.Clear();
                simVarFirstUpdateDone.Clear();
                addressToIndex.Clear();
                nextID = 1;
                SendClientWasmCmd("MF.SimVars.Clear");
                Thread.Sleep(Plugin.ActionController.Timing / 4);

                foreach(var address in usedAddresses)
                {
                    SubscribeAddress(address);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:ReorderRegistrations", $"Exception while reordering Registrations'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public void SetSimVar(string address, double value)
        {
            address = IPCManager.FormatAddress(address, true);
            if (!IPCTools.rxAvar.IsMatch(address) && !IPCTools.rxLvarMobi.IsMatch(address))
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:SetSimVar", $"The Address '{address}' is not valid for MobiFlight!");
                return;
            }

            address = address.Insert(1, ">");
            address = $"{string.Format(CultureInfo.InvariantCulture, "{0:G}", value)} {address}";
            SendClientWasmCmd($"MF.SimVars.Set.{address}");
            SendClientWasmDummyCmd();
        }

        public void ExecuteCode(string code)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{code}");
            SendClientWasmDummyCmd();
        }
    }
}
