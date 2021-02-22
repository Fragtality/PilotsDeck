namespace PilotsDeck
{
    public enum ActionSwitchType
    {
        MACRO,
        SCRIPT,
        CONTROL,
        LVAR,
        OFFSET
    }

    public class ModelSwitch : ModelBase
    {
        public virtual string AddressAction { get; set; } = "";
        public virtual int ActionType { get; set; } = (int)ActionSwitchType.MACRO;
        public virtual string OnState { get; set; } = "";
        public virtual string OffState { get; set; } = "";

        public virtual bool HasLongPress { get; set; } = false;
        public virtual string AddressActionLong { get; set; } = "";
        public virtual int ActionTypeLong { get; set; } = (int)ActionSwitchType.MACRO;

        public virtual string OnStateLong { get; set; } = "";
        public virtual string OffStateLong { get; set; } = "";

        public ModelSwitch()
        {
            DefaultImage = @"Images/Switch.png";
            ErrorImage = @"Images/SwitchError.png";
        }
    }
}
