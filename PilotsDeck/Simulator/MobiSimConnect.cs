using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace PilotsDeck
{
    public class MobiSimConnect : IDisposable
    {
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;
        public const int MOBIFLIGHT_STRINGVAR_SIZE = 128;
        public const int MOBIFLIGHT_STRINGVAR_MAX_AMOUNT = 64;
        public const int MOBIFLIGHT_STRINGVAR_DATAAREA_SIZE = MOBIFLIGHT_STRINGVAR_SIZE * MOBIFLIGHT_STRINGVAR_MAX_AMOUNT;

        public const uint WM_PILOTSDECK_SIMCONNECT = 0x1984;
        public const string CLIENT_NAME = "PilotsDeck";
        public const string PILOTSDECK_CLIENT_DATA_NAME_SIMVAR = $"{CLIENT_NAME}.LVars";
        public const string PILOTSDECK_CLIENT_DATA_NAME_COMMAND = $"{CLIENT_NAME}.Command";
        public const string PILOTSDECK_CLIENT_DATA_NAME_RESPONSE = $"{CLIENT_NAME}.Response";
        public const string PILOTSDECK_CLIENT_DATA_NAME_STRINGVAR = $"{CLIENT_NAME}.StringVars";

        public SimConnect simConnect { get; protected set; } = null;
        protected IntPtr simConnectHandle = IntPtr.Zero;
        protected Thread simConnectThread = null;
        private static bool cancelThread = false;

        protected bool isSimConnected = false;
        protected bool isMobiConnected = false;
        protected bool isReceiveRunning = false;
        public bool IsConnected { get { return isSimConnected && isMobiConnected; } }
        public bool IsReady { get { return IsConnected && isReceiveRunning; } }
        public bool HasReceiveError { get { return !isReceiveRunning; } }

        protected IPCManager ipcManager { get; }
        protected MobiVariables mobiVariables { get; }
        protected SimConnectInputEvents inputEvents { get; }
        protected Dictionary<uint, string> simEvents { get; } = [];
        public const uint DEFINE_ID_OFFSET_SIMEVENT = 30000;
        protected uint nextSimEventID { get; set; } = DEFINE_ID_OFFSET_SIMEVENT;
        protected ConnectorMSFS.EventCallback eventCallback { get; }

        public MobiSimConnect(IPCManager manager, ConnectorMSFS.EventCallback callbackFunc)
        {
            ipcManager = manager;
            mobiVariables = new(manager, this);
            inputEvents = new(manager, this);
            eventCallback = callbackFunc;
        }

        public bool Connect()
        {
            try
            {
                if (isSimConnected)
                    return true;

                if (simConnect == null)
                {
                    simConnect = new SimConnect(CLIENT_NAME, simConnectHandle, WM_PILOTSDECK_SIMCONNECT, null, 0);
                    simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnOpen);
                    simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnQuit);
                    simConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(SimConnect_OnReceiveEvent);
                    simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnException);
                }

                if (simConnectThread == null)
                {
                    cancelThread = false;
                    simConnectThread = new(new ThreadStart(SimConnect_ReceiveThread))
                    {
                        IsBackground = true
                    };
                    simConnectHandle = new IntPtr(simConnectThread.ManagedThreadId);
                    simConnectThread.Start();

                    Logger.Log(LogLevel.Information, "MobiSimConnect:Connect", $"SimConnect Connection open.");
                }
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
                simConnect.OnRecvEnumerateInputEvents += new SimConnect.RecvEnumerateInputEventsEventHandler(inputEvents.SimConnect_OnRecvEnumerateInputEvents);
                simConnect.OnRecvGetInputEvent += new SimConnect.RecvGetInputEventEventHandler(inputEvents.SimConnect_OnRecvGetInputEvents);
                CreateDataAreaDefaultChannel();
                Logger.Log(LogLevel.Information, "MobiSimConnect:OnOpen", $"SimConnect OnOpen received.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:OnOpen", $"Exception during SimConnect OnOpen! (Exception: {ex.GetType()})");
            }
        }

        protected void SimConnect_ReceiveThread()
        {
            ulong ticks = 0;
            int delay = 50;
            int repeat = 10000 / delay;
            int errors = 0;
            isReceiveRunning = true;
            while (!cancelThread && simConnect != null && isReceiveRunning)
            {
                try
                {
                    simConnect.ReceiveMessage();

                    if (isSimConnected && !isMobiConnected && ticks % (ulong)repeat == 0)
                    {
                        Logger.Log(LogLevel.Debug, "MobiSimConnect:ReceiveThread", $"Sending Ping to MobiFlight WASM Module.");
                        SendMobiWasmCmd("MF.DummyCmd");
                        SendMobiWasmCmd("MF.Ping");
                    }
                    else if (IsReady)
                        mobiVariables.SubscribeQueue();
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors > 10)
                    {
                        isReceiveRunning = false;
                        Logger.Log(LogLevel.Critical, "MobiSimConnect:ReceiveThread", $"Maximum Errors reached, closing Receive Thread! (Exception: {ex.GetType()})");
                        return;
                    }
                }
                Thread.Sleep(delay);
                ticks++;
            }
            isReceiveRunning = false;
            return;
        }

        public static void RegisterClientData(SimConnect simConnect, uint ID, Enum DataId, SIMCONNECT_CLIENT_DATA_PERIOD period)
        {
            simConnect.RequestClientData(
                DataId,
                (SIMCONNECT_REQUEST_ID)ID,
                (SIMCONNECT_DEFINE_ID)ID,
                period,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0
            );
        }

        protected void CreateDataAreaDefaultChannel()
        {
            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD);
            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            simConnect.AddToClientDataDefinition(SIMCONNECT_DEFINE_ID.CLIENT_MOBI, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);

            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>(SIMCONNECT_DEFINE_ID.CLIENT_MOBI);

            simConnect.RequestClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)SIMCONNECT_DEFINE_ID.CLIENT_MOBI,
                SIMCONNECT_DEFINE_ID.CLIENT_MOBI,
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
            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_STRINGVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_STRINGVARS);

            simConnect.AddToClientDataDefinition(SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);

            simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>(SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK);

            simConnect.RequestClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK,
                SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK,
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
                Logger.Log(LogLevel.Debug, "MobiSimConnect:UnsubscribeSimConnectEvent", $"Event '{evtName}' with ID '{id}' is now unsubscribed");
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
                nextSimEventID = DEFINE_ID_OFFSET_SIMEVENT;
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
                        Logger.Log(LogLevel.Warning, "MobiSimConnect:OnReceiveEvent", $"Event ID '{recEvent.uEventID}' is not subscribed!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:OnReceiveEvent", $"Exception '{ex.GetType}' while receiving Events: {ex.Message} (eventID: {recEvent?.uEventID})");
            }
        }

        protected void SimConnect_OnClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            try
            {
                Logger.Log(LogLevel.Verbose, "MobiSimConnect:OnClientData", $"dwRequestID {data.dwRequestID} => '{mobiVariables.GetAddress(data.dwRequestID)}'");
                if (data.dwRequestID == (uint)SIMCONNECT_DEFINE_ID.CLIENT_MOBI)
                {
                    var request = (ResponseString)data.dwData[0];
                    if (request.Data == "MF.Pong")
                    {
                        if (!isMobiConnected)
                        {
                            Logger.Log(LogLevel.Information, "MobiSimConnect:OnClientData", $"MobiFlight WASM Ping acknowledged - opening Client Connection.");
                            SendMobiWasmCmd("MF.DummyCmd");
                            SendMobiWasmCmd($"MF.Clients.Add.{CLIENT_NAME}");
                            SendMobiWasmCmd("MF.DummyCmd");
                        }
                        else
                            Logger.Log(LogLevel.Debug, "MobiSimConnect:OnClientData", $"MF.Pong received although already connected.");
                    }
                    else if (request.Data == $"MF.Clients.Add.{CLIENT_NAME}.Finished")
                    {
                        CreateDataAreaClientChannel();
                        isMobiConnected = true;
                        SendClientWasmDummyCmd();
                        SendClientWasmCmd("MF.SimVars.Clear");
                        SendClientWasmCmd("MF.Config.MAX_VARS_PER_FRAME.Set.30");
                        Logger.Log(LogLevel.Information, "MobiSimConnect:OnClientData", $"MobiFlight WASM Client Connection opened.");
                    }
                    else if (!request.Data.StartsWith("MF.Clients.Add."))
                        Logger.Log(LogLevel.Information, "MobiSimConnect:OnClientData", $"Unhandled MobiFlight Messages received: '{request.Data}'");
                }
                else if (data.dwRequestID == (uint)SIMCONNECT_DEFINE_ID.CLIENT_MOBI)
                {
                    var simData = (ResponseString)data.dwData[0];
                    Logger.Log(LogLevel.Debug, "MobiSimConnect:OnClientData", $"Received Client Callback: {simData.Data}");
                }
                else
                {
                    mobiVariables.UpdateId(data.dwRequestID, data.dwData);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:OnClientData", $"Exception during SimConnect OnClientData! (Exception: {ex.GetType()}) (Data: {data})");
            }
        }

        public void EnumerateInputEvents()
        {
            inputEvents.EnumerateInputEvents();
        }

        public void SubscribeInputEvent(string name)
        {
            inputEvents.SubscribeInputEvent(name);
        }

        public void UnsubscribeInputEvent(string name)
        {
            inputEvents.UnsubscribeInputEvent(name);
        }

        public bool SendInputEvent(string name, double value)
        {
            return inputEvents.SendInputEvent(name, value);
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
                {
                    mobiVariables.RemoveAll();
                    SendClientWasmCmd("MF.SimVars.Clear");
                }

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

                inputEvents.RemoveAll();
                simEvents.Clear();
                nextSimEventID = DEFINE_ID_OFFSET_SIMEVENT;
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

        public void SendClientWasmCmd(string command, bool includeDummy = true)
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK, command);
            if (includeDummy)
                SendClientWasmDummyCmd();
        }

        private void SendClientWasmDummyCmd()
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK, "MF.DummyCmd");
        }

        private void SendMobiWasmCmd(string command)
        {
            SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_DEFINE_ID.CLIENT_MOBI, command);
        }

        private void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            simConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }

        protected void SimConnect_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            Logger.Log(LogLevel.Critical, "MobiSimConnect:OnException", $"Exception '{((SIMCONNECT_EXCEPTION)data.dwException) as Enum}' received: (dwException {data.dwException} | dwID {data.dwID} | dwSendID {data.dwSendID} | dwIndex {data.dwIndex})");
        }

        public void SubscribeAddress(string address, IPCValue value)
        {
            mobiVariables.SubscribeAddress(address, value);
        }

        public void Process(bool requestInputEvents)
        {
            if (requestInputEvents && IsReady)
            {
                if (!inputEvents.eventsEnumerated)
                {
                    Logger.Log(LogLevel.Error, "MobiSimConnect:Process", $"Sim is Ready and Events not enumerated - running Enumeration");
                    EnumerateInputEvents();
                }
                else
                    inputEvents.RequestInputEventValues();
            }
            else if (!requestInputEvents && IsReady && inputEvents.eventsEnumerated)
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:Process", $"Sim is not Ready and Events are enumerated - reset Enumeration State");
                inputEvents.eventsEnumerated = false;
            }
        }

        public void SubscribeAllAddresses(bool bVarOnly = false)
        {
            string address;
            foreach (var value in ipcManager.ValueList)
            {
                if (value == null)
                    continue;

                address = value.Address;
                if (IPCTools.rxBvar.IsMatch(address))
                    inputEvents.SubscribeInputEvent(address);
                if (!bVarOnly && ConnectorMSFS.IsValidConnectorAddress(address))
                    mobiVariables.SubscribeAddress(address, value);
            }
            Logger.Log(LogLevel.Information, "MobiSimConnect:SubscribeAllAddresses", $"Subscribed all IPCValues.");
        }

        public void UnsubscribeAddress(string address)
        {
            mobiVariables.UnsubscribeAddress(address);
        }

        public void UnsubscribeUnusedAddresses()
        {
            mobiVariables.ReorderRegistrations();
        }

        public void SetSimVar(string address, double value)
        {
            address = IPCManager.FormatAddress(address);
            if (!IPCTools.rxAvar.IsMatch(address) && !IPCTools.rxLvarMobi.IsMatch(address))
            {
                Logger.Log(LogLevel.Error, "MobiSimConnect:SetSimVar", $"The Address '{address}' is not valid for MobiFlight!");
                return;
            }

            string code = address.Insert(1, ">");
            if (!IPCTools.rxAvarMobiString.IsMatch(address))
                code = $"{string.Format(CultureInfo.InvariantCulture, "{0:G}", value)} {code}";
            else
                code = $"'{value}' {code}";
            SendClientWasmCmd($"MF.SimVars.Set.{code}");
        }

        public void ExecuteCode(string code)
        {
            SendClientWasmCmd($"MF.SimVars.Set.{code}");
        }
    }
}
