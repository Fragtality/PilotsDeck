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
        public bool IsConnected { get { return isSimConnected && isMobiConnected; } }
        public bool IsReady { get { return IsConnected && isReceiveRunning; } }
        public bool HasReceiveError { get { return !isReceiveRunning; } }

        protected uint nextID = 1;
        protected const int reorderTreshold = 150;
        protected Dictionary<string, uint> addressToIndex = new();
        protected Dictionary<uint, bool> simVarUsed = new();
        protected Dictionary<uint, IPCValue> simVars = new();
        protected List<uint> subscribeQueue = new();
        protected IPCManager ipcManager;

        public MobiSimConnect(IPCManager manager)
        {
            ipcManager = manager;
        }

        public bool Connect()
        {
            try
            {
                if (isSimConnected)
                    return true;
                
                simConnect = new SimConnect(CLIENT_NAME, simConnectHandle, WM_PILOTSDECK_SIMCONNECT, null, 0);
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnQuit);
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
            //simConnect.CreateClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, MOBIFLIGHT_MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

            simConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);
            //simConnect.CreateClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE, MOBIFLIGHT_MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

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
            //simConnect.CreateClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, MOBIFLIGHT_MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_RESPONSE, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);
            //simConnect.CreateClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE, MOBIFLIGHT_MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

            simConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_SIMVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS);
            //simConnect.CreateClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS, MOBIFLIGHT_MESSAGE_SIZE, SIMCONNECT_CREATE_CLIENT_DATA_FLAG.DEFAULT);

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
                            //Logger.Log(LogLevel.Debug, "MobiSimConnect:SimConnect_OnClientData", $"ID: {data.dwRequestID} | Value: {simData.data}");
                            ipcValue.SetValue(simData.data);
                        }
                        else
                            Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnClientData", $"The Value for ID '{data.dwRequestID}' is NULL! (Data: {data})");
                    }
                    else
                        Logger.Log(LogLevel.Error, "MobiSimConnect:SimConnect_OnClientData", $"The received ID '{data.dwRequestID}' is not subscribed! (Data: {data})");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SimConnect_OnClientData", $"Exception during SimConnect OnClientData! (Exception: {ex.GetType()}) (Data: {data})");
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
                subscribeQueue.Clear();
                addressToIndex.Clear();
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

                if (!addressToIndex.ContainsKey(address))
                {
                    simVars.Add(nextID, ipcManager[address]);
                    simVarUsed.Add(nextID, true);
                    addressToIndex.Add(address, nextID);

                    subscribeQueue.Add(nextID);
                    nextID++;
                }
                else if (addressToIndex.TryGetValue(address, out uint index) && !simVarUsed[index])
                {
                    simVarUsed[index] = true;
                    simVars[index] = ipcManager[address];
                }
                else
                    Logger.Log(LogLevel.Error, "MobiSimConnect:SubscribeAddress", $"The Address '{address}' is already subscribed!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiSimConnect:SubscribeAddress", $"Exception while subscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public void Process()
        {
            SubscribeQueue();
        }

        public void SubscribeAllAddresses()
        {
            foreach (var address in ipcManager.AddressList)
            {
                if ((IPCTools.rxLvar.IsMatch(address) && !AppSettings.Fsuipc7LegacyLvars) || IPCTools.rxAvar.IsMatch(address))
                {
                    SubscribeAddress(address);
                }
            }
            Logger.Log(LogLevel.Information, "MobiSimConnect:SubscribeAllAddresses", $"Subscribed all IPCValues.");
        }

        protected void RegisterVariable(uint ID, string address)
        {
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
                    string address = addressToIndex.Where(mapping => mapping.Value == unusedIndex.Key).FirstOrDefault().Key;
                    addressToIndex.Remove(address);
                }

                var usedAddresses = addressToIndex.Keys.ToList();
                simVarUsed.Clear();
                simVars.Clear();
                addressToIndex.Clear();
                nextID = 1;
                SendClientWasmCmd("MF.SimVars.Clear");

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
