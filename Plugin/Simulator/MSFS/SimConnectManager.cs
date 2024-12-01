using Microsoft.FlightSimulator.SimConnect;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace PilotsDeck.Simulator.MSFS
{
    enum PAUSE_FLAGS
    {
        OFF = 0,
        PAUSE = 1,
        PAUSE_LEGACY = 2,
        PAUSE_ACTIVE = 4,
        PAUSE_SIM = 8
    };


    public partial class SimConnectManager
    {
        public static SimConnect SimConnect { get; set; } = null;
        protected Thread SimConnectThread { get; set; } = null;
        public SimConnectInputEvents InputEvents { get; protected set; } = null;
        public MobiModule MobiModule { get; protected set; } = null;

        public bool IsConnected { get { return IsSimConnected && MobiModule.IsMobiConnected; } }
        public bool IsSimConnected { get; protected set; } = false;
        public bool IsSimConnectInitialized { get; protected set; } = false;
        public bool IsReceiveRunning { get; protected set; } = false;
        protected bool RunReceiveThread { get; set; } = false;
        public static Mutex SimConnectMutex { get; protected set; } = new();
        protected bool ReceiveError { get; set; } = false;
        public bool QuitReceived { get; protected set; } = false;
        public bool IsPaused { get; protected set; } = true;
        public bool IsSessionReady { get { return IsCameraValid && !string.IsNullOrWhiteSpace(AircraftString); } }
        protected bool IsCameraValid { get { return CameraState != 0 && CameraState != 32 && CameraState != 36 && (CameraState < 11 || CameraState >= 29 || CameraState == 26); } }
        protected uint CameraState = 11;
        public string AircraftString { get; protected set; } = "";

        protected Dictionary<uint, string> SimEvents { get; } = [];
        public const uint DEFINE_ID_OFFSET_SIMEVENT = 30000;
        protected uint NextSimEventID { get; set; } = DEFINE_ID_OFFSET_SIMEVENT;
        protected ISimConnector.EventCallback EventCallback { get; }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public SimConnectManager(ISimConnector.EventCallback callbackFunc)
        {
            InputEvents = new SimConnectInputEvents(this);
            MobiModule = new MobiModule();
            EventCallback = callbackFunc;
        }

        public bool Connect()
        {
            try
            {
                if (IsSimConnected)
                    return true;

                if (SimConnect == null)
                {
                    SendMessage(App.WindowHandle, AppConfiguration.WM_PILOTSDECK_REQ_SIMCONNECT, IntPtr.Zero, IntPtr.Zero);
                    Logger.Verbose("Send WM_PILOTSDECK_REQ_SIMCONNECT to WindowHandle");
                }

                if (SimConnect != null && !IsSimConnectInitialized)
                {
                    IsReceiveRunning = false;
                    SimConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnOpen);
                    SimConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnQuit);
                    SimConnect.OnRecvEvent += new SimConnect.RecvEventEventHandler(SimConnect_OnReceiveEvent);
                    SimConnect.OnRecvEventFilename += new SimConnect.RecvEventFilenameEventHandler(SimConnect_OnReceiveEventFile);
                    SimConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnException);
                    SimConnect.SubscribeToSystemEvent(SIM_SYS_EVENTS.AIRCRAFT_LOADED, "AircraftLoaded");
                    SimConnect.SubscribeToSystemEvent(SIM_SYS_EVENTS.PAUSE, "Pause_EX1");
                    IsSimConnectInitialized = true;
                    Logger.Debug($"SimConnect Object initialized");
                }

                return SimConnect != null && IsSimConnectInitialized;
            }
            catch (Exception ex)
            {
                SimConnectThread = null;
                SimConnect = null;
                IsSimConnectInitialized = false;
                IsSimConnected = false;
                IsPaused = true;
                IsReceiveRunning = false;
                RunReceiveThread = false;
                CameraState = 11;
                AircraftString = "";

                Logger.LogException(ex);
            }

            return false;
        }

        public void Disconnect()
        {
            try
            {
                RemoveAllSimConnectEvents();

                if (InputEvents?.EventsEnumerated == true)
                    InputEvents?.RemoveAll();

                if (MobiModule?.IsMobiConnected == true)
                    MobiModule?.Disconnect();

                IsReceiveRunning = false;
                ReceiveError = false;

                if (SimConnect != null)
                {
                    try { SimConnect.Dispose(); } catch { }
                    SimConnect = null;
                }

                IsSimConnected = false;
                IsSimConnectInitialized = false;
                SimEvents?.Clear();
                NextSimEventID = DEFINE_ID_OFFSET_SIMEVENT;
                Logger.Information($"SimConnect Connection closed.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void SimConnect_OnOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            try
            {
                if (SimConnect != null)
                {
                    SimConnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(MobiModule.SimConnect_OnClientData);
                    SimConnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(SimConnect_OnSimobjectData);
                    SimConnect.OnRecvEnumerateInputEvents += new SimConnect.RecvEnumerateInputEventsEventHandler(InputEvents.SimConnect_OnRecvEnumerateInputEvents);
                    SimConnect.OnRecvGetInputEvent += new SimConnect.RecvGetInputEventEventHandler(InputEvents.SimConnect_OnRecvGetInputEvents);
                    SimConnect.OnRecvSystemState += new SimConnect.RecvSystemStateEventHandler(SimConnect_OnReceiveSystemState);
                    CreateInternalVariableSubscription();
                    MobiModule.CreateDataAreaDefaultChannel();
                    IsSimConnected = true;
                    IsReceiveRunning = true;
                    Logger.Information($"SimConnect OnOpen received.");
                }
                else
                    Logger.Error("SimConnect is NULL!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected static void CreateInternalVariableSubscription()
        {
            SimConnect.AddToDataDefinition(SIMCONNECT_REQUEST_ID.InternalVariables, "CAMERA STATE", "Enum", SIMCONNECT_DATATYPE.INT64, 0, 0);
            SimConnect.RequestDataOnSimObject(SIMCONNECT_REQUEST_ID.InternalVariables, SIMCONNECT_REQUEST_ID.InternalVariables, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        }

        public void CheckAircraftString()
        {
            if (string.IsNullOrEmpty(AircraftString) && IsCameraValid)
            {
                try
                {
                    Logger.Debug($"Request AircraftLoaded from Sim");
                    SimConnectMutex.TryWaitOne();
                    SimConnect.RequestSystemState(SIM_SYS_EVENTS.AIRCRAFT_LOADED, "AircraftLoaded");
                    SimConnectMutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    SimConnectMutex.TryReleaseMutex();
                }
            }
        }

        public void SimConnect_ReceiveMessage()
        {
            try
            {
                SimConnectMutex.TryWaitOne();
                SimConnect?.ReceiveMessage();
                SimConnectMutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                ReceiveError = true;
                SimConnectMutex.TryReleaseMutex();
                IsReceiveRunning = false;

                if (ex.Message != "0xC00000B0")
                    Logger.LogException(ex);
                else
                    Logger.Error($"Exception catched: '{ex.GetType()}' - '{ex.Message}'");
            }
        }

        protected void SimConnect_OnSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (int)SIMCONNECT_REQUEST_ID.InternalVariables && data.dwDefineID == (int)SIMCONNECT_REQUEST_ID.InternalVariables && data.dwData.Length >= 1)
            {
                CameraState = (uint)data.dwData[0];
                Logger.Debug($"CameraState switched to '{CameraState}'");
            }
        }

        public void EvaluateInputEvents()
        {
            if (IsSessionReady && !InputEvents.EventsEnumerated)
                InputEvents.EnumerateInputEvents();
            else if (!IsSessionReady && InputEvents.EventsEnumerated)
                InputEvents.RemoveAll();
        }

        protected void SimConnect_OnReceiveEvent(SimConnect sender, SIMCONNECT_RECV_EVENT recEvent)
        {
            try
            {
                if (recEvent?.uGroupID == (uint)NOTFIY_GROUP.DYNAMIC)
                {
                    if (SimEvents.TryGetValue(recEvent.uEventID, out string evtName))
                        EventCallback(evtName, recEvent.dwData);
                    else
                        Logger.Warning($"Event ID '{recEvent.uEventID}' is not subscribed!");
                }
                else if (recEvent?.uEventID == (uint)SIM_SYS_EVENTS.PAUSE)
                {
                    Logger.Debug($"Received 'Pause' Event - dwData: {recEvent.dwData}");
                    if (Sys.HasFlag(recEvent.dwData, (uint)PAUSE_FLAGS.PAUSE) || Sys.HasFlag(recEvent.dwData, (uint)PAUSE_FLAGS.PAUSE_ACTIVE) || Sys.HasFlag(recEvent.dwData, (uint)PAUSE_FLAGS.PAUSE_SIM))
                        IsPaused = true;
                    else if (recEvent.dwData == 0)
                        IsPaused = false;
                }
                else
                    Logger.Debug($"Received unknown Event '{recEvent.uEventID}' (dwData {recEvent.dwData})");
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception '{ex.GetType}' while receiving Events: {ex.Message} (eventID: {recEvent?.uEventID} | dwData {recEvent?.dwData})");
            }
        }

        protected void SimConnect_OnReceiveSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE recEvent)
        {
            if (recEvent?.dwRequestID == (uint?)SIM_SYS_EVENTS.AIRCRAFT_LOADED && !string.IsNullOrEmpty(recEvent?.szString))
            {
                Logger.Debug($"Received Aircraft String: {recEvent.szString}");
                AircraftString = recEvent.szString;
            }
        }

        protected void SimConnect_OnReceiveEventFile(SimConnect sender, SIMCONNECT_RECV_EVENT_FILENAME recEvent)
        {
            try
            {
                if (recEvent?.uEventID == (uint)SIM_SYS_EVENTS.AIRCRAFT_LOADED)
                {
                    Logger.Debug($"Received 'AircraftLoaded' Event - szFileName: {recEvent.szFileName}");
                    AircraftString = recEvent.szFileName;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception '{ex.GetType}' while receiving Events: {ex.Message} (eventID: {recEvent?.uEventID} | dwData {recEvent?.dwData})");
            }
        }

        protected static void SimConnect_OnException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            Logger.Error($"Exception '{((SIMCONNECT_EXCEPTION)data.dwException) as Enum}' received: (dwException {data.dwException} | dwID {data.dwID} | dwSendID {data.dwSendID} | dwIndex {data.dwIndex})");
        }

        protected void SimConnect_OnQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            QuitReceived = true;
            Disconnect();
        }

        public void SubscribeSimConnectEvent(string evtName)
        {
            try
            {
                if (SimEvents.ContainsValue(evtName))
                {
                    Logger.Warning($"The Event '{evtName}' is already subscribed!");
                    return;
                }

                SimConnect.MapClientEventToSimEvent((SIM_EVENTS)NextSimEventID, evtName);
                SimConnect.AddClientEventToNotificationGroup(NOTFIY_GROUP.DYNAMIC, (SIM_EVENTS)NextSimEventID, false);
                SimEvents.Add(NextSimEventID, evtName);

                Logger.Debug($"Event '{evtName}' subscribed with ID '{NextSimEventID}'");
                NextSimEventID++;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void UnsubscribeSimConnectEvent(string evtName)
        {
            try
            {
                if (!SimEvents.ContainsValue(evtName))
                {
                    Logger.Warning($"The Event '{evtName}' is not subscribed!");
                    return;
                }
                uint id = SimEvents.Where(kv => kv.Value == evtName).FirstOrDefault().Key;

                SimConnect.RemoveClientEvent(NOTFIY_GROUP.DYNAMIC, (SIM_EVENTS)id);
                SimEvents.Remove(id);
                Logger.Debug($"Event '{evtName}' with ID '{id}' is now unsubscribed");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void RemoveAllSimConnectEvents()
        {
            try
            {
                foreach (var evt in SimEvents)
                    SimConnect?.RemoveClientEvent(NOTFIY_GROUP.DYNAMIC, (SIM_EVENTS)evt.Key);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                SimEvents?.Clear();
                NextSimEventID = DEFINE_ID_OFFSET_SIMEVENT;
            }
        }
    }
}
