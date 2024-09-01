using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ViewModels
{
    public enum ActionTemplate
    { 
        NONE = 0,
        DISPLAY,
        SWITCH,
        DYNAMIC,
        KORRY,
        RADIO,
        GAUGE
    }

    public static class ViewModel
    {
        public static string Compact(this string value, int num = 24)
        {
            if (value?.Length <= num)
                return value;
            else
                return $"...{value[(value.Length - num)..]}";
        }

        public static void SetSyntaxLabel(Label label, TextBox textBox)
        {
            SimValueType valid = TypeMatching.GetReadType(textBox.Text);
            label.Foreground = new SolidColorBrush(valid != 0 ? Colors.Green : Colors.Red);
            label.Content = valid != 0 ? $"Valid {valid}" : "Invalid Syntax";
        }

        public static void SetComboBox<T>(ComboBox comboBox, Dictionary<T, string> dict, T selected)
        {
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.ItemsSource = dict.ToList();

            int index = 0;
            foreach (T type in dict.Keys)
            {
                if (EqualityComparer<T>.Default.Equals(type, selected))
                    comboBox.SelectedIndex = index;
                index++;
            }
        }

        public static Dictionary<DISPLAY_ELEMENT, string> GetElementTypes()
        {
            return new()
            {
                {DISPLAY_ELEMENT.IMAGE, "Image" },
                {DISPLAY_ELEMENT.VALUE, "Value" },
                {DISPLAY_ELEMENT.TEXT, "Text" },
                {DISPLAY_ELEMENT.GAUGE, "Gauge" },
                {DISPLAY_ELEMENT.PRIMITIVE, "Primitive" },
            };
        }

        public static Dictionary<ELEMENT_MANIPULATOR, string> GetManipulatorTypes(ISelectableItem item)
        {
            Dictionary<ELEMENT_MANIPULATOR, string> dict = [];

            dict.Add(ELEMENT_MANIPULATOR.COLOR, "Color");
            dict.Add(ELEMENT_MANIPULATOR.VISIBLE, "Visible");
            if (item?.IsTypeElementValue() == true)
                dict.Add(ELEMENT_MANIPULATOR.FORMAT, "Format");
            if (item?.IsTypeElementGauge() == true)
                dict.Add(ELEMENT_MANIPULATOR.INDICATOR, "Indicator");
            dict.Add(ELEMENT_MANIPULATOR.SIZEPOS, "Size / Position");
            dict.Add(ELEMENT_MANIPULATOR.ROTATE, "Rotate");
            dict.Add(ELEMENT_MANIPULATOR.TRANSPARENCY, "Transparency");

            return dict;
        }

        public static Dictionary<PrimitiveType, string> GetPrimitiveTypes()
        {
            return new()
            {
                {PrimitiveType.LINE, "Line" },
                {PrimitiveType.RECTANGLE, "Rectangle" },
                {PrimitiveType.RECTANGLE_FILLED, "Rectangle Filled" },
                {PrimitiveType.CIRCLE, "Ellipse" },
                {PrimitiveType.CIRCLE_FILLED, "Ellipse Filled" },
            };
        }

        public static Dictionary<ActionTemplate, string> GetActionTemplates()
        {
            return new()
            {
                {ActionTemplate.NONE, "No Template" },
                {ActionTemplate.DISPLAY, "Display Value" },
                {ActionTemplate.SWITCH, "Simple Button" },
                {ActionTemplate.DYNAMIC, "Dynamic Button" },
                {ActionTemplate.KORRY, "Korry Button" },
                {ActionTemplate.RADIO, "COM Radio" },
                {ActionTemplate.GAUGE, "Display Gauge" },
            };
        }

        public static Dictionary<IndicatorType, string> GetIndicatorTypes()
        {
            return new()
            {
                {IndicatorType.TRIANGLE, "Triangle" },
                {IndicatorType.CIRCLE, "Circle" },
                {IndicatorType.DOT, "Dot" },
                {IndicatorType.LINE, "Line" },
                {IndicatorType.IMAGE, "Image" },
            };
        }

        public static Dictionary<CenterType, string> GetCenterTypes()
        {
            return new()
            {
                {CenterType.NONE, "No Centering" },
                {CenterType.HORIZONTAL, "Horizontal" },
                {CenterType.VERTICAL, "Vertical" },
                {CenterType.BOTH, "Both" },
            };
        }

        public static Dictionary<ScaleType, string> GetScaleTypes()
        {
            return new()
            {
                {ScaleType.NONE, "No Scaling" },
                {ScaleType.DEFAULT_KEEP, "Scale to Default Raster" },
                {ScaleType.DEFAULT_STRETCH, "Stretch Default Raster" },
                {ScaleType.DEVICE_KEEP, "Scale to Device Raster" },
                {ScaleType.DEVICE_STRETCH, "Stretch to Device Raster" },
            };
        }

        public static Dictionary<SimCommandType, string> GetSimTypes()
        {
            Dictionary<SimCommandType, string> dict = [];
            var model = new PropertyInspectorModel().ActionTypes;
            foreach (var item in model)
                dict.Add(item.Value, item.Key);
            return dict;
        }
    }
}
