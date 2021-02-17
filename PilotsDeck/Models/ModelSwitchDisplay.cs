namespace PilotsDeck
{
    public class ModelSwitchDisplay : ModelSwitch
    {
        public virtual string Address { get; set; } = "";

        public virtual string OnImage { get; set; } = @"Images/KorryOnBlueTop.png";
        public virtual string OffImage { get; set; } = @"Images/KorryOffWhiteBottom.png";

        public virtual bool HasIndication { get; set; } = false;
        public virtual bool IndicationValueAny { get; set; } = false;
        public virtual string IndicationImage { get; set; } = @"Images/Fault.png";
        public virtual string IndicationValue { get; set; } = "0";

        public ModelSwitchDisplay()
        {
            DefaultImage = @"Images/SwitchDefault.png";
            ErrorImage = @"Images/Error.png";
        }
    }
}
