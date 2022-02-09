namespace PilotsDeck
{
    public enum ActionSwitchType
    {
        MACRO,
        SCRIPT,
        CONTROL,
        LVAR,
        OFFSET,
        //5 is offset+lvar in PI
        VJOY = 6, //FSUIPC vJoy
        VJOYDRV = 7 //vJoy Driver by
    }

    public interface IModelSwitch
    {
        string AddressAction { get; set; }
        int ActionType { get; set; }
        string SwitchOnState { get; set; }
        string SwitchOffState { get; set; }
        bool SwitchOnCurrentValue { get; set; }
        bool UseControlDelay { get; set; }

        bool HasLongPress { get; set; }
        string AddressActionLong { get; set; }
        int ActionTypeLong { get; set; }
        string SwitchOnStateLong { get; set; }
        string SwitchOffStateLong { get; set; }
    }

    public class ModelSwitch : ModelBase, IModelSwitch
    {
        public virtual string AddressAction { get; set; } = "";
        public virtual int ActionType { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual bool UseControlDelay { get; set; } = false;
        public virtual string SwitchOnState { get; set; } = "";
        public virtual string SwitchOffState { get; set; } = "";

        public virtual bool HasLongPress { get; set; } = false;
        public virtual string AddressActionLong { get; set; } = "";
        public virtual int ActionTypeLong { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual string SwitchOnStateLong { get; set; } = "";
        public virtual string SwitchOffStateLong { get; set; } = "";

        public ModelSwitch()
        {
            DefaultImage = @"Images/Switch.png";
            ErrorImage = @"Images/SwitchError.png";
        }
    }
}
