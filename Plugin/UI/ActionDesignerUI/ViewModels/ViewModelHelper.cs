using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Simulator;
using PilotsDeck.UI.ActionDesignerUI.TreeViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
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

    public static class ViewModelHelper
    {
        public static Dictionary<DISPLAY_ELEMENT, string> ElementTypes { get; } = new()
        {
            {DISPLAY_ELEMENT.IMAGE, "Image" },
            {DISPLAY_ELEMENT.VALUE, "Value" },
            {DISPLAY_ELEMENT.TEXT, "Text" },
            {DISPLAY_ELEMENT.GAUGE, "Gauge" },
            {DISPLAY_ELEMENT.PRIMITIVE, "Primitive" },
        };

        public static void SetElementTypes(Collection<KeyValuePair<Enum, string>> target)
        {
            foreach (var type in ElementTypes)
                target.Add(new(type.Key, type.Value));
        }

        public static void SetManipulatorTypes(Collection<KeyValuePair<Enum, string>> target, DISPLAY_ELEMENT type)
        {
            target.Add(new(ELEMENT_MANIPULATOR.VISIBLE, "Visible"));
            if (type == DISPLAY_ELEMENT.VALUE)
                target.Add(new(ELEMENT_MANIPULATOR.FORMAT, "Format"));
            if (type == DISPLAY_ELEMENT.GAUGE)
                target.Add(new(ELEMENT_MANIPULATOR.INDICATOR, "Indicator"));
            target.Add(new(ELEMENT_MANIPULATOR.COLOR, "Color"));
            target.Add(new(ELEMENT_MANIPULATOR.SIZEPOS, "Size / Position"));
            target.Add(new(ELEMENT_MANIPULATOR.ROTATE, "Rotate"));
            target.Add(new(ELEMENT_MANIPULATOR.TRANSPARENCY, "Transparency"));
            target.Add(new(ELEMENT_MANIPULATOR.FLASH, "Flash"));
        }

        public static Dictionary<ELEMENT_MANIPULATOR, string> GetManipulatorTypes(TreeItemData item)
        {
            Dictionary<ELEMENT_MANIPULATOR, string> dict = [];

            dict.Add(ELEMENT_MANIPULATOR.VISIBLE, "Visible");
            if (item?.ElementType == DISPLAY_ELEMENT.VALUE || item == null)
                dict.Add(ELEMENT_MANIPULATOR.FORMAT, "Format");
            if (item?.ElementType == DISPLAY_ELEMENT.GAUGE || item == null)
                dict.Add(ELEMENT_MANIPULATOR.INDICATOR, "Indicator");
            dict.Add(ELEMENT_MANIPULATOR.COLOR, "Color");
            dict.Add(ELEMENT_MANIPULATOR.SIZEPOS, "Size / Position");
            dict.Add(ELEMENT_MANIPULATOR.ROTATE, "Rotate");
            dict.Add(ELEMENT_MANIPULATOR.TRANSPARENCY, "Transparency");
            dict.Add(ELEMENT_MANIPULATOR.FLASH, "Flash");

            return dict;
        }

        public static Dictionary<ELEMENT_MANIPULATOR, string> ManipulatorTypes { get; } = new()
        {
            { ELEMENT_MANIPULATOR.VISIBLE, "Visible" },
            { ELEMENT_MANIPULATOR.FORMAT, "Format" },
            { ELEMENT_MANIPULATOR.INDICATOR, "Indicator" },
            { ELEMENT_MANIPULATOR.COLOR, "Color" },
            { ELEMENT_MANIPULATOR.SIZEPOS, "Size / Position" },
            { ELEMENT_MANIPULATOR.ROTATE, "Rotate" },
            { ELEMENT_MANIPULATOR.TRANSPARENCY, "Transparency" },
            { ELEMENT_MANIPULATOR.FLASH, "Flash" },
        };

        public static Dictionary<PrimitiveType, string> PrimitiveTypes { get; } = new()
        {
            { PrimitiveType.LINE, "Line" },
            { PrimitiveType.RECTANGLE, "Rectangle" },
            { PrimitiveType.RECTANGLE_FILLED, "Rectangle Filled" },
            { PrimitiveType.CIRCLE, "Ellipse" },
            { PrimitiveType.CIRCLE_FILLED, "Ellipse Filled" },
        };


        public static Dictionary<ActionTemplate, string> ActionTemplates { get; } = new()
        {
            { ActionTemplate.NONE, "No Template" },
            { ActionTemplate.DISPLAY, "Display Value" },
            { ActionTemplate.SWITCH, "Simple Button" },
            { ActionTemplate.DYNAMIC, "Dynamic Button" },
            { ActionTemplate.KORRY, "Korry Button" },
            { ActionTemplate.RADIO, "COM Radio" },
            { ActionTemplate.GAUGE, "Display Gauge" },
        };

        public static void SetTemplateTypes(Collection<KeyValuePair<Enum, string>> target)
        {
            foreach (var type in ActionTemplates)
                target.Add(new(type.Key, type.Value));
        }

        public static Dictionary<IndicatorType, string> IndicatorTypes { get; } = new()
        {
            { IndicatorType.TRIANGLE, "Triangle" },
            { IndicatorType.CIRCLE, "Circle" },
            { IndicatorType.DOT, "Dot" },
            { IndicatorType.LINE, "Line" },
            { IndicatorType.IMAGE, "Image" },
        };

        public static Dictionary<CenterType, string> CenterTypes { get; } = new()
        {
            { CenterType.NONE, "No Centering" },
            { CenterType.HORIZONTAL, "Horizontal" },
            { CenterType.VERTICAL, "Vertical" },
            { CenterType.BOTH, "Both" },
        };

        public static Dictionary<ScaleType, string> ScaleTypes { get; } = new()
        {
            { ScaleType.NONE, "No Scaling" },
            { ScaleType.DEFAULT_KEEP, "Scale to Default Raster" },
            { ScaleType.DEFAULT_STRETCH, "Stretch Default Raster" },
            { ScaleType.DEVICE_KEEP, "Scale to Device Raster" },
            { ScaleType.DEVICE_STRETCH, "Stretch to Device Raster" },
        };

        public static Dictionary<SimCommandType, string> GetSimTypes()
        {
            Dictionary<SimCommandType, string> dict = [];
            var model = new PropertyInspectorModel().ActionTypes;
            foreach (var item in model)
                dict.Add(item.Value, item.Key);
            return dict;
        }

        public static Dictionary<StreamDeckCommand, string> DeckCommandTypes { get; } = new()
        {
            { StreamDeckCommand.KEY_DOWN, "Key Down" },
            { StreamDeckCommand.KEY_UP, "Key Up" },
            { StreamDeckCommand.DIAL_DOWN, "Dial Down" },
            { StreamDeckCommand.DIAL_UP, "Dial Up" },
            { StreamDeckCommand.DIAL_LEFT, "Dial Left" },
            { StreamDeckCommand.DIAL_RIGHT, "Dial Right" },
            { StreamDeckCommand.TOUCH_TAP, "Touch Tap" },
        };

        public static Dictionary<Comparison, string> ComparisonTypes { get; } = new()
        {
            { Comparison.LESS, "<" },
            { Comparison.LESS_EQUAL, "<=" },
            { Comparison.GREATER, ">" },
            { Comparison.GREATER_EQUAL, ">=" },
            { Comparison.EQUAL, "==" },
            { Comparison.NOT_EQUAL, "!=" },
            { Comparison.CONTAINS, "contains" },
            { Comparison.NOT_CONTAINS, "not contains" },
            { Comparison.HAS_CHANGED, "has changed" },
        };
    }
}
