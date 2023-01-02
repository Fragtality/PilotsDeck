using FSUIPC;
using Serilog;
using System;
using WASM = FSUIPC.MSFSVariableServices;

namespace PilotsDeck
{
    public class ConnectorMSFS : SimulatorConnector
    {
        private static readonly string inMenuAddr = "3365:1";
        private static readonly string isPausedAddr = "0262:2";
        private static readonly string camReadyAddr = "026D:1";
        private IPCValueOffset inMenuValue;
        private IPCValueOffset isPausedValue;
        private IPCValueOffset camReadyValue;
        private bool wasmReloaded = false;

        public override bool IsConnected { get { return FSUIPCConnection.IsOpen && WASM.IsRunning; } protected set { } }
        public virtual bool IsCamReady()
        {
            bool result = false;

            if (camReadyValue != null && int.TryParse(camReadyValue.Value, out int camReady))
            {
                return camReady >= 2 && camReady <= 5;
            }

            return result;
        }
        public override bool IsReady { get { return inMenuValue?.Value == "0" && isPausedValue?.Value == "0" && IsConnected && IsCamReady(); } }
        public override bool IsRunning { get { return GetProcessRunning("FlightSimulator"); } }
        public override bool IsPaused { get { return isPausedValue?.Value != "0"; } protected set { } }

        protected static readonly string AircraftAddrString = "9540:64:s";
        protected IPCValueOffset AircraftValue = null;
        public override string AicraftString { get { return AircraftValue == null ? "" : AircraftValue.Value; } protected set { } }

        protected bool isWASMReady = false;
        protected int ticks = 0;

        public override void Close()
        {
            if (FSUIPCConnection.IsOpen)
                FSUIPCConnection.Close();

            if (!FSUIPCConnection.IsOpen)
                Log.Logger.Information("ConnectorMSFS: FSUIPC Closed");
            else
                Log.Logger.Error("ConnectorMSFS: Failed to close FSUIPC");

            WASM.OnLogEntryReceived -= OnVariableServiceLogEvent;
            WASM.OnVariableListChanged -= OnVariableServiceListChanged;
            WASM.Stop();
            if (!WASM.IsRunning)
                Log.Logger.Information("ConnectorMSFS: FSUIPC WASM Stopped");
            else
                Log.Logger.Warning("ConnectorMSFS: FSUIPC WASM still running!");

            isWASMReady = false;
        }

        protected virtual void OnVariableServiceLogEvent(object sender, LogEventArgs e)
        {
            Log.Logger.Information($"ConnectorMSFS: {e.LogEntry}");
        }

        protected virtual void OnVariableServiceListChanged(object sender, EventArgs e)
        {
            isWASMReady = true;
            Log.Logger.Information($"ConnectorMSFS: VarListChanged");
        }

        public override bool Connect()
        {
            try
            {
                ticks = 0;
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Log.Logger.Information("ConnectorMSFS: FSUIPC Connected");
                    foreach (var addr in ipcManager.AddressList)
                        ipcManager[addr].Connect();
                }

                WASM.OnLogEntryReceived += OnVariableServiceLogEvent;
                WASM.OnVariableListChanged += OnVariableServiceListChanged;
                WASM.Init();
                WASM.LVARUpdateFrequency = 0;
                WASM.LogLevel = LOGLEVEL.LOG_LEVEL_INFO;

                WASM.Start();
                if (WASM.IsRunning)
                {
                    Log.Logger.Information("ConnectorMSFS: FSUIPC WASM Running");
                }
            }
            catch
            {
                Log.Logger.Error("ConnectorMSFS: Exception while opening FSUIPC");
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
            camReadyValue = new IPCValueOffset(camReadyAddr, AppSettings.groupStringRead, OffsetAction.Read);
            AircraftValue = new IPCValueOffset(AircraftAddrString, AppSettings.groupStringRead, OffsetAction.Read);

            SimType = SimulatorType.MSFS;
        }

        public override bool Process()
        {
            resultProcess = false;
            try
            {
                ticks += 10;
                if (!firstProcessSuccess || !lastStateProcess)
                {
                    isPausedValue.Connect();
                    inMenuValue.Connect();
                    camReadyValue.Connect();
                    AircraftValue.Connect();
                }

                FSUIPCConnection.Process(AppSettings.groupStringRead);
                if (!IsReady)
                {
                    Log.Logger.Debug("ConnectorMSFS: NOT READY");
                    resultProcess = false;
                }

                if (!isWASMReady)
                {
                    if (!WASM.IsRunning)
                    {
                        Log.Logger.Debug("ConnectorMSFS: WASM NOT READY - NOT RUNNING");
                        resultProcess = false;
                    }
                    else if (WASM.LVarsChanged.Count > 0 && WASM.LVars.Count > 0 && ticks > AppSettings.waitTicks)
                    {
                        Log.Logger.Debug("ConnectorMSFS: WASM NOT READY - But Lvars changed, set to READY");
                        isWASMReady = true;
                        resultProcess = true;
                    }
                    else
                    {
                        Log.Logger.Debug($"ConnectorMSFS: WASM NOT READY (Vars Changed {WASM.LVarsChanged.Count} | Vars Total {WASM.LVars.Count} | Ticks {ticks})");
                        resultProcess = false;
                    }
                }
                else if (IsReady && !wasmReloaded)
                {
                    wasmReloaded = true;
                    WASM.Reload();
                }

                resultProcess = true;
            }
            catch
            {
                Log.Logger.Error("ConnectorMSFS: Exception while process call to FSUIPC");
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
            Log.Logger.Debug("ConnectorMSFS: Subscribed all IPCValues");
        }

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, string offValue = null)
        {
            switch (actionType)
            {
                case ActionSwitchType.MACRO:
                    return IPCTools.RunMacros(Address);
                case ActionSwitchType.SCRIPT:
                    return IPCTools.RunScript(Address);
                case ActionSwitchType.LVAR:
                    return IPCTools.WriteLvar(Address, newValue, switchSettings.UseLvarReset, offValue, !AppSettings.Fsuipc7LegacyLvars);
                case ActionSwitchType.HVAR:
                    return IPCTools.WriteHvar(Address);
                case ActionSwitchType.CONTROL:
                    return IPCTools.SendControls(Address, switchSettings.UseControlDelay);
                case ActionSwitchType.OFFSET:
                    return IPCTools.WriteOffset(Address, newValue);
                case ActionSwitchType.CALCULATOR:
                    return IPCTools.RunCalculatorCode(Address);
                default:
                    Log.Logger.Error($"ConnectorMSFS: Action-Type {actionType} not valid for Address {Address}");
                    return false;
            }
        }
    }
}
