using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Tools;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PilotsDeck.UI.ViewModels
{
    public class ViewModelAction(ActionMeta action, ActionDesigner view)
    {
        public ActionMeta Action { get; set; } = action;
        public bool IsEncoder { get { return Action?.IsEncoder == true; } }
        protected ActionDesigner MainView { get; set; } = view;
        public string Context { get { return Action.Context; } }
        public virtual ConcurrentDictionary<int, DisplayElement> DisplayElements { get { return Action.DisplayElements; } }
        public virtual ConcurrentDictionary<StreamDeckCommand, ConcurrentDictionary<int, ActionCommand>> ActionCommands { get { return Action.ActionCommands; } }

        public void UpdateAction(bool onlyRessources = false)
        {
            Action.UpdateRessources(true);
            if (!onlyRessources)
                MainView.RefreshControls();
        }

        public void SetInterActionDelay(StreamDeckCommand type, string delay)
        {
            if (Conversion.IsNumberI(delay, out int numValue))
            {
                Action.Settings.ActionDelays[type] = numValue;
                UpdateAction(true);
            }
        }

        public string GetInterActionDelay(StreamDeckCommand type)
        {
            return Conversion.ToString(Action.Settings.ActionDelays[type]);
        }

        public int AddDisplayElement(DISPLAY_ELEMENT type, ModelDisplayElement model = null)
        {
            int result = Action.AddDisplayElement(type, model);
            UpdateAction(true);
            return result;
        }

        public void RemoveDisplayElement(int elementID)
        {
            Action.RemoveDisplayElement(elementID);
            UpdateAction();
        }

        public int AddManipulator(int elementID, ELEMENT_MANIPULATOR type, ModelManipulator model = null)
        {
            int result = Action.DisplayElements[elementID]?.AddManipulator(type, model) ?? -1;
            UpdateAction(true);
            return result;
        }

        public void RemoveManipulator(int elementID, int manipulatorID)
        {
            Action.DisplayElements[elementID]?.RemoveManipulator(manipulatorID);
            UpdateAction();
        }

        public int AddManipulatorCondition(int elementID, int manipulatorID, ConditionHandler model = null)
        {
            int result = Action.DisplayElements[elementID]?.ElementManipulators[manipulatorID]?.AddCondition(model ?? new ConditionHandler()) ?? -1;
            UpdateAction(true);
            return result;
        }

        public void RemoveManipulatorCondition(int elementID, int manipulatorID, int conditionID)
        {
            Action.DisplayElements[elementID]?.ElementManipulators[manipulatorID]?.RemoveCondition(conditionID);
            UpdateAction();
        }

        public int AddCommand(StreamDeckCommand type, ModelCommand model = null)
        {
            model ??= new ModelCommand() { DeckCommandType = type };
            model.DeckCommandType = type;

            int result = Action.AddCommand(new ActionCommand(model), type);
            UpdateAction(true);
            return result;
        }

        public void RemoveCommand(StreamDeckCommand type, int commandID)
        {
            Action.RemoveCommand(type, commandID);
            UpdateAction();
        }

        public int AddCommandCondition(StreamDeckCommand type, int commandID, ConditionHandler model = null)
        {
            int result = Action.AddActionCondition(type, commandID, model ?? new ConditionHandler());
            UpdateAction(true);
            return result;
        }

        public void RemoveCommandCondition(StreamDeckCommand type, int commandID, int conditionID)
        {
            Action.RemoveActionCondition(type, commandID, conditionID);
            UpdateAction();
        }

        public bool SwapElement(int first, int second)
        {
            if (second >= 0 && second < Action.Settings.DisplayElements.Count)
            {
                return Swap(Action.Settings.DisplayElements, first, second);
            }
            else
                return false;
        }

        public bool SwapManipulator(int elementID, int first, int second)
        {
            if (second >= 0 && second < Action.Settings.DisplayElements[elementID].Manipulators.Count)
            {
                return Swap(Action.Settings.DisplayElements[elementID].Manipulators, first, second);
            }
            else
                return false;
        }

        public bool SwapManipulatorCondition(int elementID, int manipulatorID, int first, int second)
        {
            if (second >= 0 && second < Action.Settings.DisplayElements[elementID].Manipulators[manipulatorID].Conditions.Count)
            {
                return Swap(Action.Settings.DisplayElements[elementID].Manipulators[manipulatorID].Conditions, first, second);
            }
            else
                return false;
        }

        public bool SwapActionCondition(StreamDeckCommand type, int cmdID, int first, int second)
        {
            if (second >= 0 && second < Action.Settings.ActionCommands[type][cmdID].Conditions.Count)
            {
                return Swap(Action.Settings.ActionCommands[type][cmdID].Conditions, first, second);
            }
            else
                return false;
        }

        public bool SwapCommand(StreamDeckCommand type, int first, int second)
        {
            if (second >= 0 && second < Action.Settings.ActionCommands[type].Count)
            {
                return Swap(Action.Settings.ActionCommands[type], first, second);
            }
            else
                return false;
        }

        private bool Swap<E>(SortedDictionary<int, E> dict, int first, int second)
        {
            if (dict.TryGetValue(first, out E firstElement) && dict.TryGetValue(second, out E secondElement))
            {
                dict.Remove(first);
                dict.Remove(second);
                dict.Add(first, secondElement);
                dict.Add(second, firstElement);
                UpdateAction(true);
                return true;
            }
            else
                return false;
        }

        public void SetTemplate(ActionTemplate template)
        {
            switch (template)
            {
                case ActionTemplate.DISPLAY:
                    TemplateDisplayValue();
                    break;
                case ActionTemplate.DYNAMIC:
                    TemplateDisplayDynamicButton();
                    break;
                case ActionTemplate.KORRY:
                    TemplateDisplayKorryButton();
                    break;
                case ActionTemplate.RADIO:
                    TemplateComRadio();
                    break;
                case ActionTemplate.GAUGE:
                    TemplateGauge();
                    break;
                default:
                    TemplateSwitch();
                    break;
            }
        }

        public void TemplateSwitch()
        {
            Action.Settings.DisplayElements.Clear();
            Action.Settings.ClearDictionaries();
            Action.Settings.FillDictionaries();
            Action.Settings.CanvasSize = [Action.CanvasSize.X, Action.CanvasSize.Y];

            int id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            Action.Settings.DisplayElements[id].Image = "Images/Switch.png";
            Action.Settings.DisplayElements[id].Scale = ScaleType.DEVICE_KEEP;
            Action.Settings.DisplayElements[id].Center = CenterType.BOTH;
            Action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
            UpdateAction();
        }

        public void TemplateDisplayValue()
        {
            Action.Settings.DisplayElements.Clear();
            Action.Settings.ClearDictionaries();
            Action.Settings.FillDictionaries();
            Action.Settings.CanvasSize = [Action.CanvasSize.X, Action.CanvasSize.Y];

            Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            Action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Value");
            Action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
            UpdateAction();
        }

        public void TemplateDisplayDynamicButton()
        {
            Action.Settings.DisplayElements.Clear();
            Action.Settings.ClearDictionaries();
            Action.Settings.FillDictionaries();
            Action.Settings.CanvasSize = [Action.CanvasSize.X, Action.CanvasSize.Y];

            int id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "On Image");
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            Action.Settings.DisplayElements[id].Image = "Images/KorryOnBlueTop.png";
            Action.Settings.DisplayElements[id].Scale = ScaleType.DEVICE_KEEP;
            Action.Settings.DisplayElements[id].Center = CenterType.BOTH;
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Off Image");
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            Action.Settings.DisplayElements[id].Image = "Images/KorryOffWhiteBottom.png";
            Action.Settings.DisplayElements[id].Scale = ScaleType.DEVICE_KEEP;
            Action.Settings.DisplayElements[id].Center = CenterType.BOTH;
            Action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
            UpdateAction();
        }

        public void TemplateDisplayKorryButton()
        {
            Action.Settings.DisplayElements.Clear();
            Action.Settings.ClearDictionaries();
            Action.Settings.FillDictionaries();
            var scale = ToolsRender.GetScale(Action.CanvasSize, new System.Drawing.PointF(72, 72));
            Action.Settings.CanvasSize = [Action.CanvasSize.X, Action.CanvasSize.Y];
            if (!scale.IsSquare())
                scale.X = scale.Y;

            Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            int id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Top Image");
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            Action.Settings.DisplayElements[id].Image = "Images/korry/A-FAULT.png";
            Action.Settings.DisplayElements[id].Position = [9 * scale.X, 21 * scale.Y];
            Action.Settings.DisplayElements[id].Size = [54 * scale.X, 20 * scale.Y];
            Action.Settings.DisplayElements[id].Center = CenterType.NONE;
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Bottom Image");
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            Action.Settings.DisplayElements[id].Image = "Images/korry/A-ON-BLUE.png";
            Action.Settings.DisplayElements[id].Position = [9 * scale.X, 45 * scale.Y];
            Action.Settings.DisplayElements[id].Size = [54 * scale.X, 20 * scale.Y];
            Action.Settings.DisplayElements[id].Center = CenterType.NONE;
            Action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
            UpdateAction();
        }

        public void TemplateComRadio()
        {
            Action.Settings.DisplayElements.Clear();
            Action.Settings.ClearDictionaries();
            Action.Settings.FillDictionaries();
            var scale = ToolsRender.GetScale(Action.CanvasSize, new System.Drawing.PointF(72, 72));
            Action.Settings.CanvasSize = [Action.CanvasSize.X, Action.CanvasSize.Y];
            if (!scale.IsSquare())
                scale.X = scale.Y;

            int id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            Action.Settings.DisplayElements[id].Image = "Images/Arrow.png";
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Swap Image");
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.VISIBLE);
            Action.Settings.DisplayElements[id].Image = "Images/ArrowBright.png";
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Standby");
            Action.Settings.DisplayElements[id].Position = [3 * scale.X, 42 * scale.Y];
            Action.Settings.DisplayElements[id].Size = [64 * scale.X, 32 * scale.Y];
            Action.Settings.DisplayElements[id].FontSize *= scale.Y;
            Action.Settings.DisplayElements[id].Center = CenterType.HORIZONTAL;
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Active");
            Action.Settings.DisplayElements[id].Position = [3 * scale.X, 1 * scale.Y];
            Action.Settings.DisplayElements[id].Size = [64 * scale.X, 32 * scale.Y];
            Action.Settings.DisplayElements[id].FontSize *= scale.Y;
            Action.Settings.DisplayElements[id].Center = CenterType.HORIZONTAL;            
            Action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
            UpdateAction();
        }

        public void TemplateGauge()
        {
            Action.Settings.DisplayElements.Clear();
            Action.Settings.ClearDictionaries();
            Action.Settings.FillDictionaries();
            var scale = ToolsRender.GetScale(Action.CanvasSize, new System.Drawing.PointF(72, 72));
            Action.Settings.CanvasSize = [Action.CanvasSize.X, Action.CanvasSize.Y];

            int id = Action.AddDisplayElement(DISPLAY_ELEMENT.IMAGE, null, "Background");
            Action.Settings.DisplayElements[id].Image = "Images/Empty.png";
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.GAUGE, null, "Gauge");
            Action.Settings.DisplayElements[id].Position = [0, 0];
            Action.Settings.DisplayElements[id].Size = [50 * scale.Y, 8 * scale.Y];
            Action.Settings.DisplayElements[id].FontSize *= scale.Y;
            Action.Settings.DisplayElements[id].AddManipulator(ELEMENT_MANIPULATOR.INDICATOR);
            Action.Settings.DisplayElements[id].Manipulators[0].IndicatorSize *= scale.Y;
            id = Action.AddDisplayElement(DISPLAY_ELEMENT.VALUE, null, "Value");
            Action.Settings.DisplayElements[id].Position = [7 * scale.X, 45 * scale.Y];
            Action.Settings.DisplayElements[id].Size = [60 * scale.X, 21 * scale.Y];
            Action.Settings.DisplayElements[id].FontSize *= scale.Y;
            Action.AddCommand(new ActionCommand(new ModelCommand(StreamDeckCommand.KEY_UP)), StreamDeckCommand.KEY_UP);
            UpdateAction();
        }
    }
}
