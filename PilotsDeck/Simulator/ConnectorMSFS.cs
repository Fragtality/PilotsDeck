using FSUIPC;
using System;
using WASM = FSUIPC.MSFSVariableServices;

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
        public override bool IsPaused { get { return pauseIndValue?.Value != "0"; } protected set { } }

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
                Logger.Log(LogLevel.Information, "ConnectorMSFS:Close", $"FSUIPC Closed.");
            else
                Logger.Log(LogLevel.Error, "ConnectorMSFS:Close", $"Failed to close FSUIPC!");

            WASM.OnLogEntryReceived -= OnVariableServiceLogEvent;
            WASM.OnVariableListChanged -= OnVariableServiceListChanged;
            WASM.Stop();
            if (!WASM.IsRunning)
                Logger.Log(LogLevel.Information, "ConnectorMSFS:Close", $"WASM stopped.");
            else
                Logger.Log(LogLevel.Warning, "ConnectorMSFS:Close", $"WASM still running!");

            isWASMReady = false;
        }

        protected virtual void OnVariableServiceLogEvent(object sender, LogEventArgs e)
        {
            Logger.Log(LogLevel.Debug, "ConnectorMSFS:WASMLOG", e.LogEntry);
        }

        protected virtual void OnVariableServiceListChanged(object sender, EventArgs e)
        {
            isWASMReady = true;
            Logger.Log(LogLevel.Information, "ConnectorMSFS:OnVariableServiceListChanged", $"VarListChanged Event received!");
        }

        public override bool Connect()
        {
            try
            {
                ticks = 0;
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Logger.Log(LogLevel.Information, "ConnectorMSFS:Connect", $"FSUIPC Connected. Starting WASM ...");
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
                    Logger.Log(LogLevel.Information, "ConnectorMSFS:Connect", $"WASM running.");
                }
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
                ticks += 10;
                if (!firstProcessSuccess || !lastStateProcess)
                {
                    isPausedValue.Connect();
                    pauseIndValue.Connect();
                    inMenuValue.Connect();
                    camReadyValue.Connect();
                    AircraftValue.Connect();
                }

                FSUIPCConnection.Process(AppSettings.groupStringRead);
                if (!IsReady)
                {
                    Logger.Log(LogLevel.Debug, "ConnectorMSFS:Process", $"Not ready!");
                    resultProcess = false;
                }

                if (!isWASMReady)
                {
                    if (!WASM.IsRunning)
                    {
                        Logger.Log(LogLevel.Debug, "ConnectorMSFS:Process", $"WASM not not Running!");
                        resultProcess = false;
                    }
                    else if (WASM.LVarsChanged.Count > 0 && WASM.LVars.Count > 0 && ticks > AppSettings.waitTicks)
                    {
                        Logger.Log(LogLevel.Debug, "ConnectorMSFS:Process", $"WASM not ready, but Lvars changed. Set to ready!");
                        isWASMReady = true;
                        resultProcess = true;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "ConnectorMSFS:Process", $"WASM not ready! (Changed: {WASM.LVarsChanged.Count}) (Variables: {WASM.LVars.Count}) (Ticks: {ticks})");
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorMSFS:Process", $"Exception in Process Call! (Exception: {ex.GetType()})");
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
            Logger.Log(LogLevel.Debug, "ConnectorMSFS:SubscribeAllAddresses", $"Subscribed all IPCValues. (Count: {ipcManager.AddressList.Count})");
        }

        protected bool UpdateLvar(string Address, string newValue, bool lvarReset, string offValue, bool useWASM)
        {
            bool result = SimTools.WriteLvar(Address, newValue, lvarReset, offValue, useWASM);
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
                    return SimTools.RunMacros(Address);
                case ActionSwitchType.SCRIPT:
                    return SimTools.RunScript(Address);
                case ActionSwitchType.LVAR:
                    return UpdateLvar(Address, newValue, !ignoreLvarReset && switchSettings.UseLvarReset, offValue, !AppSettings.Fsuipc7LegacyLvars);
                case ActionSwitchType.HVAR:
                    return SimTools.WriteHvar(Address);
                case ActionSwitchType.CONTROL:
                    return SimTools.SendControls(Address, switchSettings.UseControlDelay);
                case ActionSwitchType.OFFSET:
                    return SimTools.WriteOffset(Address, newValue);
                case ActionSwitchType.CALCULATOR:
                    return SimTools.RunCalculatorCode(Address, ticks);
                default:
                    Logger.Log(LogLevel.Error, "ConnectorMSFS:RunAction", $"Action-Type '{actionType}' not valid for Address '{Address}'!");
                    return false;
            }
        }
    }
}
