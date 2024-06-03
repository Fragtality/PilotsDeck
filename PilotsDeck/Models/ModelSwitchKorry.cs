using System.Drawing;

namespace PilotsDeck
{
    public class ModelSwitchKorry : ModelSwitchDisplay
    {
        public virtual string AddressTop { get; set; } = "";
        public virtual string AddressBot { get; set; } = "";
        public virtual bool UseOnlyTopAddr { get; set; } = false;
        public virtual bool ShowTopImage { get; set; } = true;
        public virtual bool ShowBotImage { get; set; } = true;
        public override bool SwitchOnCurrentValue { get; set; } = false;

        public virtual string TopState { get; set; } = "";
        public virtual bool ShowTopNonZero { get; set; } = false;
        public virtual string BotState { get; set; } = "";
        public virtual bool ShowBotNonZero { get; set; } = false;
        public virtual string ImageMapBot { get; set; } = "";

        public virtual string TopImage { get; set; } = "Images/korry/A-FAULT.png";
        public virtual string BotImage { get; set; } = "Images/korry/A-ON-Blue.png";

        public virtual string TopRect { get; set; } = "9; 21; 54; 20";
        public virtual string BotRect { get; set; } = "9; 45; 54; 20";

        public ModelSwitchKorry()
        {
            DefaultImage = @"Images/Empty.png";
            ErrorImage = @"Images/Error.png";
        }

        public Rectangle GetRectangleTop()
        {
            return ModelDisplayText.GetRectangle(TopRect);
        }

        public Rectangle GetRectangleBot()
        {
            return ModelDisplayText.GetRectangle(BotRect);
        }
    }
}
