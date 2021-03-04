namespace PilotsDeck
{
    public class HandlerDisplaySwitch : HandlerDisplayText, IHandlerSwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings as ModelDisplayText; } }
        public virtual ModelDisplaySwitch SwitchSettings { get { return Settings as ModelDisplaySwitch; } }
        public new ModelDisplaySwitch Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplaySwitch] Read: {TextSettings.Address} | Write: {SwitchSettings.AddressAction}| LongWrite: {SwitchSettings.HasLongPress} - {SwitchSettings.AddressActionLong}"; } }

        public virtual long tickDown { get; protected set; }
        protected virtual string LastSwitchState { get; set; }
        protected virtual string LastSwitchStateLong { get; set; }


        public HandlerDisplaySwitch(string context, ModelDisplaySwitch settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            LastSwitchState = settings.OffState;
            LastSwitchStateLong = settings.OffStateLong;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(SwitchSettings.AddressAction);
        }

        public override void Update(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Update(imgManager, ipcManager);

            if ((LastSwitchState != SwitchSettings.OffState && LastSwitchState != SwitchSettings.OnState) ||
                (LastSwitchStateLong != SwitchSettings.OffStateLong && LastSwitchStateLong != SwitchSettings.OnStateLong))
            {
                LastSwitchState = SwitchSettings.OffState;
                LastSwitchStateLong = SwitchSettings.OffStateLong;
            }
        }

        public virtual bool OnButtonDown(IPCManager ipcManager, long tick)
        {
            tickDown = tick;
            return HandlerSwitch.RunButtonDown(ipcManager, SwitchSettings.GetSwitchSettings());
        }

        public virtual bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            bool result = HandlerSwitch.RunButtonUp(ipcManager, (tick - tickDown) >= AppSettings.longPressTicks, LastSwitchState, LastSwitchStateLong, SwitchSettings.GetSwitchSettings(), out string[] newValues);
            LastSwitchState = newValues[0];
            LastSwitchStateLong = newValues[1];
            tickDown = 0;

            return result;
        }
    }
}
