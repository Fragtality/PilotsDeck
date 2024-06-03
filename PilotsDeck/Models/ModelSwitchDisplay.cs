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

        public static void ManageImageMap(string map, ImageManager imgManager, StreamDeckType deckType, bool add = true)
        {
            if (string.IsNullOrEmpty(map))
                return;

            string[] parts = map.Split(':');
            foreach (var part in parts)
            {
                string[] mapping = part.Split('=');
                if (mapping.Length == 2 && !string.IsNullOrWhiteSpace(mapping[0]) && !string.IsNullOrEmpty(mapping[1]))
                {
                    if (add)
                        imgManager.AddImage($"Images/{mapping[1]}.png", deckType);
                    else
                        imgManager.RemoveImage($"Images/{mapping[1]}.png", deckType);
                }
            }
        }

        public virtual void ManageImageMap(ImageManager imgManager, StreamDeckType deckType, bool add = true)
        {
            if (!UseImageMapping || string.IsNullOrEmpty(ImageMap))
                return;

            ManageImageMap(ImageMap, imgManager, deckType, add);
        }

        public string GetValueMapped(string strValue)
        {
            return GetValueMapped(strValue, ImageMap);
        }

        public static string GetValueMapped(string strValue, string imageMap)
        {
            string result = ModelDisplayText.GetValueMapped(strValue, imageMap);
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
