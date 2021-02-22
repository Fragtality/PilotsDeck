namespace PilotsDeck
{
    public class ModelDisplaySwitch : ModelDisplayText
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

        public ModelDisplaySwitch() : base()
        {

        }
    }
}
