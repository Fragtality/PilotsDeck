namespace PilotsDeck.UI.ActionDesignerUI.Clipboard
{
    public static class SettingClipboard
    {
        public static SettingType CopiedType { get; set; } = SettingType.NONE;
        public static object CopiedValue { get; set; } = null;
        public static bool IsEmpty { get { return CopiedValue == null && CopiedType == SettingType.NONE; } }

        public static void CopyToClipboard(object item, SettingType type)
        {
            CopiedValue = item;
            CopiedType = type;
        }

        public static bool IsType(SettingType type)
        {
            return CopiedValue != null && CopiedType == type;
        }

        public static T PasteFromClipboard<T>()
        {
            T item = (T)CopiedValue;
            CopiedValue = null;
            CopiedType = SettingType.NONE;

            return item;
        }
    }
}
