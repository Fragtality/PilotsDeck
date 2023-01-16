using FSUIPC;
using Serilog;

namespace PilotsDeck
{
    public class ConnectorFSXP3D : SimulatorConnector
    {
        private static readonly string inMenuAddr = "3365:1";
        private static readonly string isPausedAddr = "0262:2";
        private IPCValueOffset inMenuValue;
        private IPCValueOffset isPausedValue;

        public override bool IsConnected { get { return FSUIPCConnection.IsOpen; } protected set { } }
        public override bool IsReady { get { return inMenuValue?.Value == "0" && isPausedValue?.Value == "0" && IsConnected; } }
        public override bool IsRunning { get { return GetProcessRunning("Prepar3D") || GetProcessRunning("fsx"); } }
        public override bool IsPaused { get { return isPausedValue?.Value != "0"; } protected set { } }

        protected static readonly string AircraftAddrString = "9540:64:s";
        protected IPCValueOffset AircraftValue = null;
        public override string AicraftString { get { return AircraftValue == null ? "" : AircraftValue.Value; } protected set { } }

        public override void Close()
        {
            if (FSUIPCConnection.IsOpen)
                FSUIPCConnection.Close();

            if (!FSUIPCConnection.IsOpen)
                Log.Logger.Information("ConnectorFSXP3D: FSUIPC Closed");
            else
                Log.Logger.Error("ConnectorFSXP3D: Failed to close FSUIPC");
        }

        public override bool Connect()
        {
            try
            {
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Log.Logger.Information("ConnectorFSXP3D: FSUIPC Connected");
                    foreach (var addr in ipcManager.AddressList)
                        ipcManager[addr].Connect();
                }
            }
            catch
            {
                Log.Logger.Error("ConnectorFSXP3D: Exception while opening FSUIPC");
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
                    inMenuValue.Connect();
                    AircraftValue.Connect();
                }

                FSUIPCConnection.Process(AppSettings.groupStringRead);
                if (!IsReady)
                {
                    Log.Logger.Debug("ConnectorFSXP3D: NOT READY");
                    resultProcess = false;
                }
                resultProcess = true;

            }
            catch
            {
                Log.Logger.Error("ConnectorFSXP3D: Exception while process call to FSUIPC");
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
            Log.Logger.Debug("ConnectorFSXP3D: Subscribed all IPCValues");
        }

        protected bool UpdateLvar(string Address, string newValue, bool lvarReset, string offValue, bool useWASM)
        {
            bool result = IPCTools.WriteLvar(Address, newValue, lvarReset, offValue, useWASM);
            if (result && !string.IsNullOrEmpty(newValue) && newValue[0] != '$' && ipcManager[Address] != null)
            {
                ipcManager[Address].SetValue(newValue);
            }

            return result;
        }

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, bool ignoreLvarReset, string offValue = null, int ticks = 1)
        {
            switch (actionType)
            {
                case ActionSwitchType.MACRO:
                    return IPCTools.RunMacros(Address);
                case ActionSwitchType.SCRIPT:
                    return IPCTools.RunScript(Address);
                case ActionSwitchType.LVAR:
                    return UpdateLvar(Address, newValue, !ignoreLvarReset && switchSettings.UseLvarReset, offValue, false);
                case ActionSwitchType.CONTROL:
                    return IPCTools.SendControls(Address, switchSettings.UseControlDelay);
                case ActionSwitchType.OFFSET:
                    return IPCTools.WriteOffset(Address, newValue);
                default:
                    Log.Logger.Error($"ConnectorFSXP3D: Action-Type {actionType} not valid for Address {Address}");
                    return false;
            }
        }
    }
}
