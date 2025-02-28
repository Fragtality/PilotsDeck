using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.Clipboard
{
    public class SettingButtonBinding
    {
        public readonly static SolidColorBrush Default = SystemColors.WindowFrameBrush;
        public readonly static SolidColorBrush Highlight = SystemColors.HighlightBrush;
        public readonly static SolidColorBrush Green = new(Colors.Green);
        public readonly static Thickness ThicknessDefault = new(1);
        public readonly static Thickness ThicknessHighlight = new(1.5);

        public virtual Button Button { get; }
        public virtual string SettingProperty { get; }
        public virtual SettingType SettingType { get; }
        protected virtual object CopiedValue { get { return SettingClipboard.CopiedValue; } }
        protected virtual SettingType CopiedType { get { return SettingClipboard.CopiedType; } }

        public SettingButtonBinding(Button button, string propertyName, SettingType type)
        {
            Button = button;
            SettingProperty = propertyName;
            SettingType = type;

            Button.IsEnabled = false;
            Button.Command = null;
            Button.CommandParameter = type;
            Button.BorderBrush = Default;
            Button.BorderThickness = ThicknessDefault;
        }

        public virtual void UpdateBinding(ICopyPasteSettings settingInterface)
        {
            if (CopiedValue != null)
            {
                if (CopiedType == SettingType)
                {
                    Button.BorderThickness = ThicknessHighlight;
                    Button.BorderBrush = Green;
                    Button.Command = settingInterface.GetPasteCommand(SettingProperty, SettingType);
                    Button.IsEnabled = Button.Command != null;
                }
                else
                {
                    Button.BorderThickness = ThicknessHighlight;
                    Button.BorderBrush = Highlight;
                    Button.IsEnabled = false;
                    Button.Command = null;
                }
            }
            else
            {
                Button.BorderThickness = ThicknessDefault;
                Button.BorderBrush = Default;
                Button.Command = settingInterface.GetCopyCommand(SettingProperty, SettingType);
                Button.IsEnabled = Button.Command != null;
            }
        }
    }
}
