namespace PilotsDeck
{
    public class HandlerDisplaySwitch(string context, ModelDisplaySwitch settings, StreamDeckType deckType) : HandlerDisplayText(context, settings, deckType)
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplaySwitch Settings { get; protected set; } = settings;

        public override string ActionID { get { return $"(HandlerSwitchDisplay) ({Title.Trim()}) {(TextSettings.IsEncoder ? "(Encoder) " : "")}(Read: {TextSettings.Address}) (Action: {(ActionSwitchType)SwitchSettings.ActionType} / {Address}) (Long: {SwitchSettings.HasLongPress} / {(ActionSwitchType)SwitchSettings.ActionTypeLong} / {SwitchSettings.AddressActionLong})"; } }
        public override bool HasAction { get; protected set; } = true;

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
            return HandlerSwitch.RunButtonDown(IPCManager, SwitchSettings);
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
