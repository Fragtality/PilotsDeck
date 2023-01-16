namespace PilotsDeck
{
    public class HandlerDisplaySwitch : HandlerDisplayText
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplaySwitch Settings { get; protected set; }

        public override string ActionID { get { return $"\"{StreamDeckTools.TitleLog(Title)}\" [HandlerDisplaySwitch] Read: {TextSettings.Address} | Write: {SwitchSettings.AddressAction}| LongWrite: {SwitchSettings.HasLongPress} - {SwitchSettings.AddressActionLong}"; } }

        public override bool HasAction { get; protected set; } = true;


        public HandlerDisplaySwitch(string context, ModelDisplaySwitch settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            SwitchSettings.SwitchOnCurrentValue = false;
            base.Register(imgManager, ipcManager);
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(SwitchSettings.AddressAction);
        }

        public override bool OnButtonDown(long tick)
        {
            TickDown = tick;
            return HandlerSwitch.RunButtonDown(SwitchSettings);
        }

        public override bool OnButtonUp(long tick)
        {
            bool result = HandlerSwitch.RunButtonUp(IPCManager, tick - TickDown, ValueManager, SwitchSettings);
            TickDown = 0;

            return result;
        }

        public override bool OnDialRotate(int ticks)
        {
            return HandlerSwitch.RunDialRotate(IPCManager, ticks, ValueManager, SwitchSettings);
        }

        public override bool OnTouchTap()
        {
            return HandlerSwitch.RunTouchTap(IPCManager, ValueManager, SwitchSettings);
        }
    }
}
