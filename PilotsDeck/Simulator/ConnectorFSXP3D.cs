using FSUIPC;
using System;

namespace PilotsDeck
{
    public class ConnectorFSXP3D : SimulatorConnector
    {
        private static readonly string inMenuAddr = "3365:1";
        private static readonly string isPausedAddr = "0262:2";
        private static readonly string pauseIndAddr = "0264:2";
        private IPCValueOffset inMenuValue;
        private IPCValueOffset isPausedValue;
        private IPCValueOffset pauseIndValue;

        public override bool IsConnected { get { return FSUIPCConnection.IsOpen; } protected set { } }
        public override bool IsReady { get { return inMenuValue?.Value == "0" && isPausedValue?.Value == "0" && IsConnected; } }
        public override bool IsRunning { get { return GetProcessRunning("Prepar3D") || GetProcessRunning("fsx"); } }
        public override bool IsPaused { get { return pauseIndValue?.Value != "0"; } protected set { } }

        protected static readonly string AircraftAddrString = "9540:64:s";
        protected IPCValueOffset AircraftValue = null;
        public override string AicraftString { get { return AircraftValue == null ? "" : AircraftValue.Value; } protected set { } }

        public override void Close()
        {
            if (FSUIPCConnection.IsOpen)
                FSUIPCConnection.Close();

            if (!FSUIPCConnection.IsOpen)
                Logger.Log(LogLevel.Information, "ConnectorFSXP3D:Close", $"FSUIPC Closed.");
            else
                Logger.Log(LogLevel.Error, "ConnectorFSXP3D:Close", $"Failed to close FSUIPC!");
        }

        public override bool Connect()
        {
            try
            {
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Logger.Log(LogLevel.Information, "ConnectorFSXP3D:Connect", $"FSUIPC Connected.");
                    foreach (var addr in ipcManager.AddressList)
                        ipcManager[addr].Connect();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorFSXP3D:Connect", $"Exception while opening FSUIPC! (Exception: {ex.GetType()})");
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

            isPausedValue = new IPCValueOffset(isPausedAddr, AppSettings.groupStringRead, OffsetAction.Read);
            pauseIndValue = new IPCValueOffset(pauseIndAddr, AppSettings.groupStringRead, OffsetAction.Read);
            inMenuValue = new IPCValueOffset(inMenuAddr, AppSettings.groupStringRead, OffsetAction.Read);
            AircraftValue = new IPCValueOffset(AircraftAddrString, AppSettings.groupStringRead, OffsetAction.Read);

            if (GetProcessRunning("Prepar3D"))
                SimType = SimulatorType.P3D;
            else
                SimType = SimulatorType.FSX;
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
                    AircraftValue.Connect();
                }

                FSUIPCConnection.Process(AppSettings.groupStringRead);
                if (!IsReady)
                {
                    Logger.Log(LogLevel.Debug, "ConnectorFSXP3D:Process", $"Not ready!");
                    resultProcess = false;
                }
                resultProcess = true;

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorFSXP3D:Process", $"Exception in Process Call! (Exception: {ex.GetType()})");
                resultProcess = false;
            }

            return resultProcess;
        }

        public override void SubscribeAddress(string address)
        {
            
        }

        public override void UnsubscribeAddress(string address)
        {

        }

        public override void SubscribeAllAddresses()
        {
            foreach (var address in ipcManager.AddressList)
            {
                ipcManager[address].Connect();
            }
            Logger.Log(LogLevel.Debug, "ConnectorFSXP3D:SubscribeAllAddresses", $"Subscribed all IPCValues. (Count: {ipcManager.AddressList.Count})");
        }

        protected bool UpdateLvar(string Address, string newValue)
        {
            bool result = SimTools.WriteLvar(Address, newValue);
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
                case ActionSwitchType.LUAFUNC:
                    return SimTools.RunLuaFunc(Address);
                case ActionSwitchType.MACRO:
                    return SimTools.RunMacros(Address);
                case ActionSwitchType.SCRIPT:
                    return SimTools.RunScript(Address);
                case ActionSwitchType.LVAR:
                    return UpdateLvar(Address, newValue);
                case ActionSwitchType.CONTROL:
                    return SimTools.SendControls(Address, switchSettings.UseControlDelay);
                case ActionSwitchType.OFFSET:
                    return SimTools.WriteOffset(Address, newValue);
                default:
                    Logger.Log(LogLevel.Error, "ConnectorFSXP3D:RunAction", $"Action-Type '{actionType}' not valid for Address '{Address}'!");
                    return false;
            }
        }
    }
}
