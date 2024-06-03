namespace PilotsDeck
{
    public class ModelDisplaySwitch : ModelDisplayText, IModelSwitch
    {
        public virtual string AddressAction { get; set; } = "";
        public virtual string AddressMonitor { get; set; } = "";
        public virtual string AddressActionOff { get; set; } = "";
        public virtual int ActionType { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual bool ToggleSwitch { get; set; } = false;
        public virtual bool HoldSwitch { get; set; } = false;
        public override bool SwitchOnCurrentValue { get; set; } = true;
        public virtual bool UseControlDelay { get; set; } = false;
        public virtual bool UseLvarReset { get; set; } = false;
        public virtual string SwitchOnState { get; set; } = "";
        public virtual string SwitchOffState { get; set; } = "";

        public virtual bool HasLongPress { get; set; } = false;
        public virtual string AddressActionLong { get; set; } = "";
        public virtual int ActionTypeLong { get; set; } = (int)ActionSwitchType.MACRO;

        public virtual string SwitchOnStateLong { get; set; } = "";
        public virtual string SwitchOffStateLong { get; set; } = "";

        //Rotate Controls
        public virtual string AddressActionLeft { get; set; } = "";
        public virtual int ActionTypeLeft { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual string SwitchOnStateLeft { get; set; } = "";
        public virtual string SwitchOffStateLeft { get; set; } = "";

        public virtual string AddressActionRight { get; set; } = "";
        public virtual int ActionTypeRight { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual string SwitchOnStateRight { get; set; } = "";
        public virtual string SwitchOffStateRight { get; set; } = "";

        //Touch Control
        public virtual string AddressActionTouch { get; set; } = "";
        public virtual int ActionTypeTouch { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual string SwitchOnStateTouch { get; set; } = "";
        public virtual string SwitchOffStateTouch { get; set; } = "";

        //Guarded Switch
        public virtual bool IsGuarded { get; set; } = false;
        public virtual string AddressGuardActive { get; set; } = "";
        public virtual string GuardActiveValue { get; set; } = "";
        public virtual string AddressActionGuard { get; set; } = "";
        public virtual string AddressActionGuardOff { get; set; } = "";
        public virtual int ActionTypeGuard { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual string SwitchOnStateGuard { get; set; } = "";
        public virtual string SwitchOffStateGuard { get; set; } = "";
        public virtual string ImageGuard { get; set; }
        public virtual string GuardRect { get; set; } = "0; 0; 72; 72";
        public virtual bool UseImageGuardMapping { get; set; } = false;
        public virtual string ImageGuardMap { get; set; } = "";

        public ModelDisplaySwitch() : base()
        {

        }
    }
}
