namespace PilotsDeck
{
    public class ConnectorDummy : SimulatorConnector
    {
        public override bool IsRunning { get { return false; } }

        public override bool IsConnected { get { return false; } protected set { } }

        public override bool IsReady { get { return false; } }

        public override bool IsPaused { get { return false; } protected set { } }
        public override string AicraftString { get { return ""; } protected set { } }

        public override void Close()
        {
            
        }

        public override bool Connect()
        {
            return false;
        }

        public override void UnsubscribeAddress(string address)
        {
            
        }

        public override void Dispose()
        {
            
        }

        public override void Init(long tickCounter, IPCManager manager)
        {
            TickCounter = tickCounter;
            ipcManager = manager;
        }

        public override bool Process()
        {
            return false;
        }

        public override void SubscribeAddress(string address)
        {

        }

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, string offValue = null, int ticks = 1)
        {
            return false;
        }

        public override void SubscribeAllAddresses()
        {
            
        }
    }
}
