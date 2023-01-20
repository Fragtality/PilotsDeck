using Serilog;
using System.Linq;

namespace PilotsDeck
{
    public enum SimulatorType
    {
        FSX,
        P3D,
        MSFS,
        XP,
        UNKNOWN
    }

    public abstract class SimulatorConnector
    {
        public virtual SimulatorType SimType { get; protected set; } = SimulatorType.UNKNOWN;

        public abstract bool IsRunning { get; }
        public abstract bool IsConnected { get; protected set; }
        public abstract bool IsReady { get; }
        public abstract bool IsPaused { get; protected set; }

        protected bool lastStateApp = false;
        public virtual bool LastStateApp()
        {
            bool result = lastStateApp;
            lastStateApp = IsRunning;
            return result;
        }

        protected bool lastStateConnect = false;
        public virtual bool LastStateConnect()
        {
            bool result = lastStateConnect;
            lastStateConnect = IsConnected;
            return result;
        }

        protected bool firstProcessSuccess = false;
        protected bool lastStateProcess = false;
        protected bool resultProcess = false;
        public virtual bool LastStateProcess()
        {
            bool result = lastStateProcess;
            lastStateProcess = resultProcess;
            return result;
        }
        public virtual bool FirstProcessSuccessfull()
        {
            if (resultProcess && lastStateProcess && !firstProcessSuccess)
            {
                firstProcessSuccess = true;
                return true;
            }
            else
                return false;
        }

        public abstract string AicraftString { get; protected set; }
        public virtual long TickCounter { get; set; }

        public abstract void Init(long tickCounter, IPCManager manager);
        public abstract bool Connect();
        public abstract void Close();
        public abstract void Dispose();

        protected IPCManager ipcManager;
        public abstract bool Process();
        public abstract void SubscribeAddress(string address);
        public abstract void UnsubscribeAddress(string address);
        public abstract void SubscribeAllAddresses();
        public abstract bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, bool ignoreLvarReset, string offValue = null, int ticks = 1);

        public static bool GetProcessRunning(string name)
        {
            return System.Diagnostics.Process.GetProcessesByName(name).FirstOrDefault() != null;
        }

        public static SimulatorConnector CreateConnector(string appString, long tickCounter, IPCManager ipcManager)
        {
            SimulatorConnector connector;

            switch (appString)
            {
                case "Prepar3D.exe":
                    connector = new ConnectorFSXP3D();
                    Logger.Log(LogLevel.Debug, "SimulatorConnector:CreateConnector", $"Created Connector for FSX/P3D.");
                    break;
                case "FlightSimulator.exe":
                    connector = new ConnectorMSFS(); 
                    Logger.Log(LogLevel.Debug, "SimulatorConnector:CreateConnector", $"Created Connector for MSFS.");
                    break;
                case "X-Plane.exe":
                    connector = new ConnectorXP();

                    Logger.Log(LogLevel.Debug, "SimulatorConnector:CreateConnector", $"Created Connector for X-Plane.");
                    break;
                case "fsx.exe":
                    connector = new ConnectorFSXP3D();
                    Logger.Log(LogLevel.Debug, "SimulatorConnector:CreateConnector", $"Created Connector for FSX/P3D.");
                    break;
                default:
                    connector = new ConnectorDummy();
                    Logger.Log(LogLevel.Debug, "SimulatorConnector:CreateConnector", $"Created Dummy Connector.");
                    break;
            }

            connector.Init(tickCounter, ipcManager);
            ipcManager.SimConnector = connector;
            return connector;
        }

        public static IPCValue CreateIPCValue(string address)
        {
            IPCValue value = null;

            if (IPCTools.rxOffset.IsMatch(address))
                value = new IPCValueOffset(address, AppSettings.groupStringRead);
            else if (IPCTools.rxDref.IsMatch(address) && IPCValueDataRefString.IsStringDataRef(address))
                value = new IPCValueDataRefString(address);
            else if (IPCTools.rxDref.IsMatch(address))
                value = new IPCValueDataRef(address);
            else if (IPCTools.rxLvar.IsMatch(address))
                value = new IPCValueLvar(address);

            return value;
        }
    }
}
