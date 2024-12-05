using PilotsDeck.Actions.Advanced.SettingsModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ViewModels
{
    public enum SettingType
    {
        NONE = 0,
        COLOR = 1,
        FONT,
        SIZE,
        POS,
        VALUEMAP,
        GAUGEMAP,
        GAUGEMARKER
    }

    public class GaugeMarkerSettings
    {
        public List<MarkerDefinition> GaugeMarkers = [];
        public List<MarkerRangeDefinition> GaugeRangeMarkers = [];
    }

    public static class SettingItem
    {
        public readonly static SolidColorBrush Default = SystemColors.WindowFrameBrush;
        public readonly static SolidColorBrush Highlight = SystemColors.HighlightBrush;
        public readonly static SolidColorBrush Green = new(Colors.Green);
        public readonly static Thickness ThicknessDefault = new(1);
        public readonly static Thickness ThicknessHighlight = new(1.5);

        public static SettingType CopiedType { get; set; } = SettingType.NONE;
        public static object CopiedValue { get; set; } = null;
        public static bool IsEmpty { get { return CopiedValue == null && CopiedType == SettingType.NONE; } }

        public static void CopyItem<T>(Func<T> get, SettingType type)
        {
            CopiedValue = get();
            CopiedType = type;
        }

        public static bool IsType(SettingType type)
        {
            return CopiedValue != null && CopiedType == type;
        }

        public static void PasteItem(out object item)
        {
            item = CopiedValue;
            CopiedValue = null;
            CopiedType = SettingType.NONE;
        }

        public static void Clipboard_Click<T>(SettingType type, Button button, Action<T> set, Func<T> get)
        {
            if (IsType(type))
            {
                PasteItem(out object item);
                if (item is T setting)
                    set(setting);
            }
            else if (IsEmpty)
            {
                CopyItem(get, type);
            }

            SetButton(button, type);
        }

        public static void SetButton(Button button, SettingType type)
        {
            if (CopiedValue != null)
            {
                if (CopiedType == type)
                {
                    button.BorderThickness = ThicknessHighlight;
                    button.BorderBrush = Green;
                    button.IsEnabled = true;
                }
                else
                {
                    button.BorderThickness = ThicknessHighlight;
                    button.BorderBrush = Highlight;
                    button.IsEnabled = false;
                }
            }
            else
            {
                button.BorderThickness = ThicknessDefault;
                button.BorderBrush = Default;
                button.IsEnabled = true;
            }
        }
    }

    public class FontSetting
    {
        public System.Drawing.Font Font;
        public System.Drawing.StringAlignment HorizontalAlignment;
        public System.Drawing.StringAlignment VerticalAlignment;
    }
}
