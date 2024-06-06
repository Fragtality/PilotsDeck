using FSUIPC;
using System;
using System.Linq;

namespace PilotsDeck
{
    public class ConnectorMSFS : SimulatorConnector
    {
        private static readonly string inMenuAddr = "3365:1";
        private static readonly string pauseIndAddr = "0264:2";
        private static readonly string camReadyAddr = "026D:1";
        private IPCValueOffset inMenuValue;
        private IPCValueOffset pauseIndValue;
        private IPCValueOffset camReadyValue;

        public override bool IsConnected { get { return FSUIPCConnection.IsOpen && mobiConnect.IsConnected; } protected set { } }
        public virtual bool IsCamReady()
        {
            bool result = false;

            if (camReadyValue != null && int.TryParse(camReadyValue.Value, out int camReady))
            {
                return camReady >= 2 && camReady <= 5;
            }

            return result;
        }
        public override bool IsReady { get { return inMenuValue?.Value == "0" && pauseIndValue?.Value == "0" && IsConnected && IsCamReady() && mobiConnect.IsReady; } }
        public override bool IsRunning { get { return GetProcessRunning("FlightSimulator"); } }
        public override bool IsPaused { get { return pauseIndValue?.Value != "0"; } protected set { } }

        protected static readonly string AircraftPathAddrString = "0x3C00:256:s";
        protected static readonly string AircraftAddrString = "9540:64:s";
        protected IPCValueOffset AircraftPathValue = null;
        protected IPCValueOffset AircraftValue = null;
        public override string AicraftString { get { return AircraftValue == null ? "" : AircraftValue.Value; } protected set { } }
        public override string AicraftPathString { get { return AircraftPathValue == null ? "" : AircraftPathValue.Value; } protected set { } }

        protected MobiSimConnect mobiConnect = null;
        protected bool mobiConnectRequested = false;
        protected bool forceSubscribeAll = false;

        public override void Close()
        {
            mobiConnect.Disconnect();
            forceSubscribeAll = false;

            if (FSUIPCConnection.IsOpen)
                FSUIPCConnection.Close();

            if (!FSUIPCConnection.IsOpen)
                Logger.Log(LogLevel.Information, "ConnectorMSFS:Close", $"FSUIPC Closed.");
            else
                Logger.Log(LogLevel.Error, "ConnectorMSFS:Close", $"Failed to close FSUIPC!");
        }

        public override bool Connect()
        {
            try
            {
                if (!FSUIPCConnection.IsOpen)
                    FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Logger.Log(LogLevel.Information, "ConnectorMSFS:Connect", $"FSUIPC Connected.");
                    foreach (var value in ipcManager.ValueList)
                        value.Connect();
                }

                mobiConnectRequested = mobiConnect.Connect();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorMSFS:Connect", $"Exception while opening FSUIPC! (Exception: {ex.GetType()})");
            }

            return IsConnected;
        }

        public override void Dispose()
        {
            Close();
        }

        public override void Init(long tickCounter, IPCManager manager)
        {
            TickCounter = tickCounter;
            ipcManager = manager;
            mobiConnect = new(manager, OnReceiveEvent);
            forceSubscribeAll = false;

            pauseIndValue = new IPCValueOffset(pauseIndAddr, AppSettings.groupStringRead, OffsetAction.Read);
            inMenuValue = new IPCValueOffset(inMenuAddr, AppSettings.groupStringRead, OffsetAction.Read);
            camReadyValue = new IPCValueOffset(camReadyAddr, AppSettings.groupStringRead, OffsetAction.Read);
            AircraftValue = new IPCValueOffset(AircraftAddrString, AppSettings.groupStringRead, OffsetAction.Read);
            AircraftPathValue = new IPCValueOffset(AircraftPathAddrString, AppSettings.groupStringRead, OffsetAction.Read);

            SimType = SimulatorType.MSFS;
        }

        public override bool Process()
        {
            resultProcess = false;
            try
            {
                if (!firstProcessSuccess || !lastStateProcess)
                {
                    pauseIndValue.Connect();
                    inMenuValue.Connect();
                    camReadyValue.Connect();
                    AircraftValue.Connect();
                    AircraftPathValue.Connect();
                }
                if (!mobiConnectRequested)
                    mobiConnectRequested = mobiConnect.Connect();

                FSUIPCConnection.Process(AppSettings.groupStringRead);
                if (!IsReady)
                {
                    Logger.Log(LogLevel.Debug, "ConnectorMSFS:Process", $"Not ready!");
                    resultProcess = false;
                }

                if (mobiConnect.IsReady)
                    mobiConnect.Process(inMenuValue?.Value == "0" && IsCamReady());
                else
                    Logger.Log(LogLevel.Debug, "ConnectorMSFS:Process", $"MobiConnect not ready!");

                if (mobiConnect.IsReady && forceSubscribeAll && !mobiConnect.HasReceiveError)
                {
                    Logger.Log(LogLevel.Information, "ConnectorMSFS:Process", $"Resubscribe all Addresses via MobiConnect ...");
                    mobiConnect.SubscribeAllAddresses();
                    forceSubscribeAll = false;
                }

                if (mobiConnect.HasReceiveError)
                {
                    Logger.Log(LogLevel.Information, "ConnectorMSFS:Process", $"MobiConnect has receive Error! Trying Disconnect & Connect");
                    mobiConnect.Disconnect();
                    mobiConnectRequested = mobiConnect.Connect();
                    forceSubscribeAll = true;
                }

                resultProcess = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorMSFS:Process", $"Exception in Process Call! (Exception: {ex.GetType()})");
                resultProcess = false;
            }

            return resultProcess;
        }

        public static bool IsValidConnectorAddress(string address)
        {
            return !IPCTools.rxOffset.IsMatch(address) &&
                 (IPCTools.rxAvar.IsMatch(address) || IPCTools.rxCalcRead.IsMatch(address) ||
                 (!AppSettings.Fsuipc7LegacyLvars && IPCTools.rxLvarMobi.IsMatch(address)) || (!AppSettings.Fsuipc7LegacyLvars && IPCTools.rxLvar.IsMatch(address)));
        }

        public override void SubscribeAddress(string address, IPCValue value)
        {
            if (IPCTools.rxBvar.IsMatch(address))
            {
                mobiConnect.SubscribeInputEvent(address);
            }
            if (IsValidConnectorAddress(address))
            {
                mobiConnect.SubscribeAddress(address, value);
            }      
        }

        public override void UnsubscribeAddress(string address)
        {
            if (IPCTools.rxBvar.IsMatch(address))
            {
                mobiConnect.UnsubscribeInputEvent(address);
            }
            if (IsValidConnectorAddress(address))
            {
                mobiConnect.UnsubscribeAddress(address);
            }
        }

        public override void UnsubscribeUnusedAddresses()
        {
            mobiConnect.UnsubscribeUnusedAddresses();
        }

        public override void SubscribeAllAddresses()
        {
            foreach (var value in ipcManager.ValueList)
            {
                value.Connect();
            }
            Logger.Log(LogLevel.Debug, "ConnectorMSFS:SubscribeAllAddresses", $"Subscribed all IPCValues. (Count: {ipcManager.AddressList.Count})");
            mobiConnect.SubscribeAllAddresses();
        }

        public override void SubscribeSimEvent(string evtName, string receiverID, EventCallback callbackFunction)
        {
            try
            {
                bool doSub = false;
                if (RegisteredEvents.TryGetValue(evtName, out var regList))
                {
                    if (regList.Any(e => e.Name == evtName && e.ReceiverID == receiverID))
                        Logger.Log(LogLevel.Warning, "ConnectorMSFS:SubscribeSimEvent", $"The Event '{evtName}' is already subscribed for '{receiverID}'!");
                    else
                    {
                        regList.Add(new EventRegistration(evtName, receiverID, callbackFunction));
                        Logger.Log(LogLevel.Debug, "ConnectorMSFS:SubscribeSimEvent", $"Subscribed Event '{evtName}' for '{receiverID}'");
                    }
                }
                else
                {
                    RegisteredEvents.Add(evtName, []);
                    RegisteredEvents[evtName].Add(new EventRegistration(evtName, receiverID, callbackFunction));
                    Logger.Log(LogLevel.Debug, "ConnectorMSFS:SubscribeSimEvent", $"Subscribed Event '{evtName}' for '{receiverID}'");
                    doSub = true;
                }

                if (doSub)
                    mobiConnect.SubscribeSimConnectEvent(evtName);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ConnectorMSFS:SubscribeSimEvent", $"Exception '{ex.GetType()}' while subscribing Event '{evtName}' for '{receiverID}': {ex.Message}");
            }
        }
        public override void UnsubscribeSimEvent(string evtName, string receiverID)
        {
            try
            {
                bool doSub = false;
                if (RegisteredEvents.TryGetValue(evtName, out var regList))
                {
                    EventRegistration reg = regList.FirstOrDefault(e => e.Name == evtName && e.ReceiverID == receiverID, null);
                    if (reg == null)
                        Logger.Log(LogLevel.Warning, "ConnectorMSFS:UnsubscribeSimEvent", $"The Event '{evtName}' is not subscribed for '{receiverID}'!");
                    else
                    {
                        regList.Remove(reg);
                        Logger.Log(LogLevel.Debug, "ConnectorMSFS:UnsubscribeSimEvent", $"Unsubscribed Event '{evtName}' for '{receiverID}'");
                        if (regList.Count == 0)
                        {
                            doSub = true;
                            RegisteredEvents.Remove(evtName);
                        }
                    }
                }
                else
                    Logger.Log(LogLevel.Warning, "ConnectorMSFS:UnsubscribeSimEvent", $"The Event '{evtName}' is not subscribed!");

                if (doSub)
                    mobiConnect.UnsubscribeSimConnectEvent(evtName);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ConnectorMSFS:UnsubscribeSimEvent", $"Exception '{ex.GetType()}' while unsubscribing Event '{evtName}' for '{receiverID}': {ex.Message}");
            }
        }

        public void OnReceiveEvent(string evtName, object evtData)
        {
            try
            {
                if (RegisteredEvents.TryGetValue(evtName, out var events))
                {
                    foreach (var evt in events)
                        evt.Callback(evtName, evtData);
                }
                else
                    Logger.Log(LogLevel.Warning, "ConnectorMSFS:OnReceiveEvent", $"The Event '{evtName}' is not subscribed!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ConnectorMSFS:OnReceiveEvent", $"Exception '{ex.GetType()}' while receiving Events: {ex.Message}");
            }
        }

        protected bool UpdateLvar(string Address, string newValue)
        {
            bool result;

            if (IPCTools.rxLvar.IsMatch(Address) && AppSettings.Fsuipc7LegacyLvars)
            {
                result = SimTools.WriteLvar(Address, newValue);
            }
            else
            {
                result = SimTools.WriteSimVar(mobiConnect, Address, newValue);
            }

            if (result && !string.IsNullOrEmpty(newValue) && newValue[0] != '$' && ipcManager[Address] != null)
            {
                ipcManager[Address].SetValue(newValue);
            }

            return result;
        }

        protected bool SendInputEvent(string Address, string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                newValue = "1";
                Logger.Log(LogLevel.Debug, "ConnectorMSFS:SendInputEvent", $"Empty Value - using '1' as Default Value");
            }
            if (IPCTools.rxBvar.IsMatch(Address) && double.TryParse(newValue, new RealInvariantFormat(newValue), out double result))
            {
                return mobiConnect.SendInputEvent(Address, result);
            }
            else
            {
                Logger.Log(LogLevel.Warning, "ConnectorMSFS:SendInputEvent", $"Malformed Address or Value for Input-Event! ({Address} = {newValue})");
                return false;
            }
        }

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, int ticks = 1)
        {
            switch (actionType)
            {
                case ActionSwitchType.INTERNAL:
                    return SimTools.WriteInternal(Address, newValue);
                case ActionSwitchType.LUAFUNC:
                    return SimTools.RunLuaFunc(Address);
                case ActionSwitchType.MACRO:
                    return SimTools.RunMacros(Address);
                case ActionSwitchType.SCRIPT:
                    return SimTools.RunScript(Address);
                case ActionSwitchType.LVAR:
                    return UpdateLvar(Address, newValue);
                case ActionSwitchType.AVAR:
                    return UpdateLvar(Address, newValue);
                case ActionSwitchType.BVAR:
                    return SendInputEvent(Address, newValue);
                case ActionSwitchType.HVAR:
                    return SimTools.WriteHvar(mobiConnect, Address, switchSettings.UseControlDelay);
                case ActionSwitchType.CONTROL:
                    return SimTools.SendControls(Address, switchSettings.UseControlDelay);
                case ActionSwitchType.OFFSET:
                    return SimTools.WriteOffset(Address, newValue);
                case ActionSwitchType.CALCULATOR:
                    return SimTools.RunCalculatorCode(mobiConnect, Address, ticks);
                default:
                    Logger.Log(LogLevel.Error, "ConnectorMSFS:RunAction", $"Action-Type '{actionType}' not valid for Address '{Address}'!");
                    return false;
            }
        }

        public void EnumerateInputEvents()
        {
            mobiConnect.EnumerateInputEvents();
        }
    }
}
