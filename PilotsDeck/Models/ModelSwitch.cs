namespace PilotsDeck
{
    public interface IModelSwitch
    {
        //Base Control
        string AddressAction { get; set; }
        string AddressMonitor { get; set; }
        string AddressActionOff { get; set; }
        int ActionType { get; set; }
        string SwitchOnState { get; set; }
        string SwitchOffState { get; set; }
        bool ToggleSwitch { get; set; }
        bool HoldSwitch { get; set; }
        bool SwitchOnCurrentValue { get; set; }
        bool UseControlDelay { get; set; }
        bool UseLvarReset { get; set; }

        //Long Press Control
        bool HasLongPress { get; set; }
        string AddressActionLong { get; set; }
        int ActionTypeLong { get; set; }
        string SwitchOnStateLong { get; set; }
        string SwitchOffStateLong { get; set; }

        //Rotate Controls
        string AddressActionLeft { get; set; }
        int ActionTypeLeft { get; set; }
        string SwitchOnStateLeft { get; set; }
        string SwitchOffStateLeft { get; set; }

        string AddressActionRight { get; set; }
        int ActionTypeRight { get; set; }
        string SwitchOnStateRight { get; set; }
        string SwitchOffStateRight { get; set; }

        //Touch Control
        string AddressActionTouch { get; set; }
        int ActionTypeTouch { get; set; }
        string SwitchOnStateTouch { get; set; }
        string SwitchOffStateTouch { get; set; }
    }

    public class ModelSwitch : ModelBase, IModelSwitch
    {
        //Base Control
        public virtual string AddressAction { get; set; } = "";
        public virtual string AddressMonitor { get; set; } = "";
        public virtual string AddressActionOff { get; set; } = "";
        public virtual int ActionType { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual bool ToggleSwitch { get; set; } = false;
        public virtual bool HoldSwitch { get; set; } = false;
        public virtual bool UseControlDelay { get; set; } = false;
        public virtual bool UseLvarReset { get; set; } = false;
        public virtual string SwitchOnState { get; set; } = "";
        public virtual string SwitchOffState { get; set; } = "";

        //Long Press Control
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

        public ModelSwitch()
        {
            DefaultImage = @"Images/Switch.png";
            ErrorImage = @"Images/SwitchError.png";
        }
    }
}
