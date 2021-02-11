using Serilog;

namespace PilotsDeck
{
    public class HandlerDisplaySwitch : HandlerDisplayText, IHandlerSwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public virtual ModelDisplaySwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplaySwitch Settings { get; protected set; }

        public override string ActionID { get { return $"{Title} | Read: {TextSettings.Address} | Write: {SwitchSettings.AddressAction}"; } }
        //public virtual string AddressAction { get { return SwitchSettings.AddressAction; } }
        //public virtual int ActionType { get { return SwitchSettings.ActionType; } }
        protected virtual string LastSwitchState { get; set; }


        public HandlerDisplaySwitch(string context, ModelDisplaySwitch settings) : base(context, settings)
        {
            Settings = settings;
        }

        protected override bool CheckInitialization()
        {
            return !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(SwitchSettings.AddressAction);
        }

        public override void Update(IPCManager ipcManager)
        {
            base.Update(ipcManager);

            if (LastSwitchState != SwitchSettings.OffState && LastSwitchState != SwitchSettings.OnState)
                LastSwitchState = SwitchSettings.OffState;
        }

        public virtual bool Action(IPCManager ipcManager)
        {
            string newValue = HandlerSwitch.ToggleValue(LastSwitchState, SwitchSettings.OffState, SwitchSettings.OnState);
            bool result = HandlerSwitch.RunAction(ipcManager, SwitchSettings.AddressAction, (ActionSwitchType)SwitchSettings.ActionType, newValue);
            if (result)
                LastSwitchState = newValue;

            return result;
        }
    }
}
