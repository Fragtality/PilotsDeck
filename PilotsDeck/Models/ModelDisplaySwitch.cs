namespace PilotsDeck
{
    public class ModelDisplaySwitch : ModelDisplayText, IModelSwitch
    {
        public virtual string AddressAction { get; set; } = "";
        public virtual string AddressActionOff { get; set; } = "";
        public virtual int ActionType { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual bool ToggleSwitch { get; set; } = false;
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

        public ModelDisplaySwitch() : base()
        {

        }
    }
}
