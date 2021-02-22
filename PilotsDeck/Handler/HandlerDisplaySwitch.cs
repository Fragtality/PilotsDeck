namespace PilotsDeck
{
    public class HandlerDisplaySwitch : HandlerDisplayText, IHandlerSwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings as ModelDisplayText; } }
        public virtual ModelDisplaySwitch SwitchSettings { get { return Settings as ModelDisplaySwitch; } }
        public new ModelDisplaySwitch Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplaySwitch] Read: {TextSettings.Address} | Write: {SwitchSettings.AddressAction}| LongWrite: {SwitchSettings.HasLongPress} - {SwitchSettings.AddressActionLong}"; } }
        protected virtual string LastSwitchState { get; set; }
        protected virtual string LastSwitchStateLong { get; set; }


        public HandlerDisplaySwitch(string context, ModelDisplaySwitch settings) : base(context, settings)
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

        public virtual bool Action(IPCManager ipcManager, bool longPress)
        {
            string newValue = HandlerSwitch.ToggleValue(LastSwitchState, SwitchSettings.OffState, SwitchSettings.OnState);
            bool result;

            if (longPress && SwitchSettings.HasLongPress)
            {
                newValue = HandlerSwitch.ToggleValue(LastSwitchStateLong, SwitchSettings.OffStateLong, SwitchSettings.OnStateLong);
                result = HandlerSwitch.RunAction(ipcManager, SwitchSettings.AddressActionLong, (ActionSwitchType)SwitchSettings.ActionTypeLong, newValue);
                if (result)
                    LastSwitchStateLong = newValue;
            }
            else
            {
                result = HandlerSwitch.RunAction(ipcManager, SwitchSettings.AddressAction, (ActionSwitchType)SwitchSettings.ActionType, newValue);
                if (result)
                    LastSwitchState = newValue;

            }

            return result;
        }
    }
}
