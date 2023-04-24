using FSUIPC;
using System;

namespace PilotsDeck
{
    public class ConnectorMSFS : SimulatorConnector
    {
        private static readonly string inMenuAddr = "3365:1";
        private static readonly string isPausedAddr = "0262:2";
        private static readonly string pauseIndAddr = "0264:2";
        private static readonly string camReadyAddr = "026D:1";
        private IPCValueOffset inMenuValue;
        private IPCValueOffset isPausedValue;
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
        public override bool IsReady { get { return inMenuValue?.Value == "0" && isPausedValue?.Value == "0" && IsConnected && IsCamReady() && mobiConnect.IsReady; } }
        public override bool IsRunning { get { return GetProcessRunning("FlightSimulator"); } }
        public override bool IsPaused { get { return pauseIndValue?.Value != "0"; } protected set { } }

        protected static readonly string AircraftAddrString = "9540:64:s";
        protected IPCValueOffset AircraftValue = null;
        public override string AicraftString { get { return AircraftValue == null ? "" : AircraftValue.Value; } protected set { } }

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
                    foreach (var addr in ipcManager.AddressList)
                        ipcManager[addr].Connect();
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
            mobiConnect = new(manager);
            forceSubscribeAll = false;

            isPausedValue = new IPCValueOffset(isPausedAddr, AppSettings.groupStringRead, OffsetAction.Read);
            pauseIndValue = new IPCValueOffset(pauseIndAddr, AppSettings.groupStringRead, OffsetAction.Read);
            inMenuValue = new IPCValueOffset(inMenuAddr, AppSettings.groupStringRead, OffsetAction.Read);
            camReadyValue = new IPCValueOffset(camReadyAddr, AppSettings.groupStringRead, OffsetAction.Read);
            AircraftValue = new IPCValueOffset(AircraftAddrString, AppSettings.groupStringRead, OffsetAction.Read);

            SimType = SimulatorType.MSFS;
        }

        public override bool Process()
        {
            resultProcess = false;
            try
            {
                if (!firstProcessSuccess || !lastStateProcess)
                {
                    isPausedValue.Connect();
                    pauseIndValue.Connect();
                    inMenuValue.Connect();
                    camReadyValue.Connect();
                    AircraftValue.Connect();
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
                    mobiConnect.Process();
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

        public override void SubscribeAddress(string address)
        {
            if (((IPCTools.rxLvar.IsMatch(address) && !AppSettings.Fsuipc7LegacyLvars) || IPCTools.rxAvar.IsMatch(address)) && !IPCTools.rxOffset.IsMatch(address))
            {
                mobiConnect.SubscribeAddress(address);
            }
        }

        public override void UnsubscribeAddress(string address)
        {
            if (((IPCTools.rxLvar.IsMatch(address) && !AppSettings.Fsuipc7LegacyLvars) || IPCTools.rxAvar.IsMatch(address)) && !IPCTools.rxOffset.IsMatch(address))
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
            foreach (var address in ipcManager.AddressList)
            {
                ipcManager[address].Connect();
            }
            Logger.Log(LogLevel.Debug, "ConnectorMSFS:SubscribeAllAddresses", $"Subscribed all IPCValues. (Count: {ipcManager.AddressList.Count})");
            mobiConnect.SubscribeAllAddresses();
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

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, int ticks = 1)
        {
            switch (actionType)
            {
                case ActionSwitchType.MACRO:
                    return SimTools.RunMacros(Address);
                case ActionSwitchType.SCRIPT:
                    return SimTools.RunScript(Address);
                case ActionSwitchType.LVAR:
                    return UpdateLvar(Address, newValue);
                case ActionSwitchType.AVAR:
                    return UpdateLvar(Address, newValue);
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
    }
}
