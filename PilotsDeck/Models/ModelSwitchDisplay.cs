namespace PilotsDeck
{
    public class ModelSwitchDisplay : ModelSwitch
    {
        public virtual string Address { get; set; } = "";
        public override bool SwitchOnCurrentValue { get; set; } = true;

        public virtual string OnImage { get; set; } = @"Images/KorryOnBlueTop.png";
        public virtual string OffImage { get; set; } = @"Images/KorryOffWhiteBottom.png";
        public virtual string OnState { get; set; } = "";
        public virtual string OffState { get; set; } = "";

        public virtual bool HasIndication { get; set; } = false;
        public virtual bool IndicationValueAny { get; set; } = false;
        public virtual string IndicationImage { get; set; } = @"Images/Fault.png";
        public virtual string IndicationValue { get; set; } = "0";

        public virtual bool UseImageMapping { get; set; } = false;
        public virtual string ImageMap { get; set; } = "";

        public virtual void ManageImageMap(ImageManager imgManager, StreamDeckType deckType, bool add = true)
        {
            if (!UseImageMapping || string.IsNullOrEmpty(ImageMap))
                return;

            string[] parts = ImageMap.Split(':');
            foreach (var part in parts)
            {
                string[] mapping = part.Split('=');
                if (!string.IsNullOrEmpty(mapping[1]))
                {
                    if (add)
                        imgManager.AddImage($"Images/{mapping[1]}.png", deckType);
                    else
                        imgManager.RemoveImage($"Images/{mapping[1]}.png", deckType);
                }
            }
        }

        public string GetValueMapped(string strValue)
        {
            string result = ModelDisplayText.GetValueMapped(strValue, ImageMap);
            if (result != strValue)
                return $"Images/{result}.png";
            else
                return "";
        }

        public ModelSwitchDisplay()
        {
            DefaultImage = @"Images/SwitchDefault.png";
            ErrorImage = @"Images/Error.png";
        }
    }
}
