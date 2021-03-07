namespace PilotsDeck
{
    public class ModelDisplayRadio : ModelDisplaySwitch
    {
        public virtual string AddressRadioActiv { get; set; } = "";
        public virtual string AddressRadioStandby { get; set; } = "";

        public virtual bool StbyHasDiffFormat { get; set; } = false;
        public virtual bool DecodeBCDStby { get; set; } = false;
        public virtual string ScalarStby { get; set; } = "1";
        public virtual string FormatStby { get; set; } = "";

        public virtual string FontColorStby { get; set; } = "#e0e0e0";
        public override string RectCoord { get; set; } = "3; 1; 64; 32";
        public virtual string RectCoordStby { get; set; } = "3; 42; 64; 31";

        public ModelDisplayRadio() : base()
        {
            DefaultImage = @"Images/Arrow.png";
            ErrorImage = @"Images/Error.png";
            IndicationImage = @"Images/ArrowBright.png";
            DrawBox = false;
        }
    }
}
