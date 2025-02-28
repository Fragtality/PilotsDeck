using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public static class ElementTemplates
    {
        public static void SetTemplate(ActionTemplate template, ActionMeta action)
        {
            switch (template)
            {
                case ActionTemplate.DISPLAY:
                    TemplateDisplayValue(action);
                    break;
                case ActionTemplate.DYNAMIC:
                    TemplateDisplayDynamicButton(action);
                    break;
                case ActionTemplate.KORRY:
                    TemplateDisplayKorryButton(action);
                    break;
                case ActionTemplate.RADIO:
                    TemplateComRadio(action);
                    break;
                case ActionTemplate.GAUGE:
                    TemplateGauge(action);
                    break;
                case ActionTemplate.SWITCH:
                    TemplateSwitch(action);
                    break;
                default:
                    break;
            }
        }

        public static void TemplateSwitch(ActionMeta action)
        {
            action.Settings.DisplayElements.Clear();
            action.Settings.ClearDictionaries();
            action.Settings.FillDictionaries();
            action.Settings.CanvasSize = [action.CanvasSize.X, action.CanvasSize.Y];

            int id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            action.Settings.DisplayElements[id].Image = "Images/Switch.png";
            action.Settings.DisplayElements[id].Scale = ScaleType.DEVICE_KEEP;
            action.Settings.DisplayElements[id].Center = CenterType.BOTH;
            action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
        }

        public static void TemplateDisplayValue(ActionMeta action)
        {
            action.Settings.DisplayElements.Clear();
            action.Settings.ClearDictionaries();
            action.Settings.FillDictionaries();
            action.Settings.CanvasSize = [action.CanvasSize.X, action.CanvasSize.Y];

            action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Value");
            action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
        }

        public static void TemplateDisplayDynamicButton(ActionMeta action)
        {
            action.Settings.DisplayElements.Clear();
            action.Settings.ClearDictionaries();
            action.Settings.FillDictionaries();
            action.Settings.CanvasSize = [action.CanvasSize.X, action.CanvasSize.Y];

            int id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "On Image");
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            action.Settings.DisplayElements[id].Image = "Images/KorryOnBlueTop.png";
            action.Settings.DisplayElements[id].Scale = ScaleType.DEVICE_KEEP;
            action.Settings.DisplayElements[id].Center = CenterType.BOTH;
            id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Off Image");
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            action.Settings.DisplayElements[id].Image = "Images/KorryOffWhiteBottom.png";
            action.Settings.DisplayElements[id].Scale = ScaleType.DEVICE_KEEP;
            action.Settings.DisplayElements[id].Center = CenterType.BOTH;
            action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
        }

        public static void TemplateDisplayKorryButton(ActionMeta action)
        {
            action.Settings.DisplayElements.Clear();
            action.Settings.ClearDictionaries();
            action.Settings.FillDictionaries();
            var scale = ToolsRender.GetScale(action.CanvasSize, new System.Drawing.PointF(72, 72));
            action.Settings.CanvasSize = [action.CanvasSize.X, action.CanvasSize.Y];
            if (!scale.IsSquare())
                scale.X = scale.Y;

            action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            int id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Top Image");
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            action.Settings.DisplayElements[id].Image = "Images/korry/A-FAULT.png";
            action.Settings.DisplayElements[id].Position = [9 * scale.X, 21 * scale.Y];
            action.Settings.DisplayElements[id].Size = [54 * scale.X, 20 * scale.Y];
            action.Settings.DisplayElements[id].Center = CenterType.NONE;
            id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Bottom Image");
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            action.Settings.DisplayElements[id].Image = "Images/korry/A-ON-BLUE.png";
            action.Settings.DisplayElements[id].Position = [9 * scale.X, 45 * scale.Y];
            action.Settings.DisplayElements[id].Size = [54 * scale.X, 20 * scale.Y];
            action.Settings.DisplayElements[id].Center = CenterType.NONE;
            action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
        }

        public static void TemplateComRadio(ActionMeta action)
        {
            action.Settings.DisplayElements.Clear();
            action.Settings.ClearDictionaries();
            action.Settings.FillDictionaries();
            var scale = ToolsRender.GetScale(action.CanvasSize, new System.Drawing.PointF(72, 72));
            action.Settings.CanvasSize = [action.CanvasSize.X, action.CanvasSize.Y];
            if (!scale.IsSquare())
                scale.X = scale.Y;

            int id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            action.Settings.DisplayElements[id].Image = "Images/Arrow.png";
            id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Swap Image");
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            action.Settings.DisplayElements[id].Image = "Images/ArrowBright.png";
            id = action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Standby");
            action.Settings.DisplayElements[id].Position = [3 * scale.X, 42 * scale.Y];
            action.Settings.DisplayElements[id].Size = [64 * scale.X, 32 * scale.Y];
            action.Settings.DisplayElements[id].Center = CenterType.HORIZONTAL;
            id = action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Active");
            action.Settings.DisplayElements[id].Position = [3 * scale.X, 1 * scale.Y];
            action.Settings.DisplayElements[id].Size = [64 * scale.X, 32 * scale.Y];
            action.Settings.DisplayElements[id].Center = CenterType.HORIZONTAL;
            action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
        }

        public static void TemplateGauge(ActionMeta action)
        {
            action.Settings.DisplayElements.Clear();
            action.Settings.ClearDictionaries();
            action.Settings.FillDictionaries();
            var scale = ToolsRender.GetScale(action.CanvasSize, new System.Drawing.PointF(72, 72));
            action.Settings.CanvasSize = [action.CanvasSize.X, action.CanvasSize.Y];

            int id = action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            action.Settings.DisplayElements[id].Image = "Images/Empty.png";
            id = action.AddDisplayElement(DISPLAY_ELEMENT.GAUGE, null, "Gauge");
            action.Settings.DisplayElements[id].Position = [0, 0];
            action.Settings.DisplayElements[id].Size = [50 * scale.Y, 8 * scale.Y];
            action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.INDICATOR);
            action.Settings.DisplayElements[id].Manipulators[0].IndicatorSize *= scale.Y;
            id = action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Value");
            action.Settings.DisplayElements[id].Position = [7 * scale.X, 45 * scale.Y];
            action.Settings.DisplayElements[id].Size = [60 * scale.X, 21 * scale.Y];
            action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
        }
    }
}
