using Serilog;

namespace PilotsDeck
{
    public class HandlerDisplaySwitch : HandlerDisplayText, IHandlerSwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public virtual ModelDisplaySwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplaySwitch Settings { get; protected set; }

        public override string ActionID { get { return $"{Title} | Read: {CommonSettings.Address} | Write: {SwitchSettings.AddressAction}"; } }
        public virtual string AddressAction { get { return SwitchSettings.AddressAction; } }
        public virtual int ActionType { get { return SwitchSettings.ActionType; } }

        protected virtual string currentValue { get; set; }

        public HandlerDisplaySwitch(string context, ModelDisplaySwitch settings) : base(context, settings)
        {
            Settings = settings;
        }

        public override void Update()
        {
            base.Update();

            if (!string.IsNullOrEmpty(CommonSettings.Address) && !string.IsNullOrEmpty(SwitchSettings.AddressAction))
                CommonSettings.IsInitialized = true;
            else
                CommonSettings.IsInitialized = false;
        }

        public virtual bool Action(IPCManager ipcManager)
        {
            string newValue = HandlerSwitch.ToggleValue(currentValue, SwitchSettings.OffState, SwitchSettings.OnState);
            bool result = HandlerSwitch.RunAction(ipcManager, SwitchSettings.AddressAction, (ActionSwitchType)SwitchSettings.ActionType, newValue);
            if (result)
                currentValue = newValue;

            return result;
        }
    }
}
