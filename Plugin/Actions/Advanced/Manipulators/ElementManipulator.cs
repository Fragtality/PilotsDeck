using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using System.Collections.Generic;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public abstract class ElementManipulator
    {
        public virtual ModelManipulator Settings { get; set; }
        public virtual DisplayElement Element { get; set; }
        public virtual ConditionStore ConditionStore { get; set; }

        public virtual bool HasChanges()
        {
            return ConditionStore.HasChanges();
        }

        public virtual void ManipulateElement()
        {

        }

        public virtual void RenderManipulator(Renderer render)
        {

        }

        public ElementManipulator(ModelManipulator model, DisplayElement parent)
        {
            Element = parent;
            Settings = model;
            ConditionStore = new(model, this);
        }

        public virtual int AddCondition(ConditionHandler condition, int? id = null)
        {
            id ??= ActionMeta.GetNextID(Settings.Conditions.Keys);
            if (!Settings.Conditions.TryAdd((int)id, condition))
            {
                Logger.Warning($"Could not add Condition ID '{id}' to Settings");
                return -1;
            }
            else
                Logger.Debug($"Added Condition for ID '{id}'");

            return (int)id;
        }

        public virtual bool RemoveCondition(int id)
        {
            if (!Settings.Conditions.ContainsKey(id))
                return false;

            if (!Settings.Conditions.Remove(id))
                return false;

            var oldDict = Settings.Conditions;
            Settings.Conditions = [];
            int n = 0;
            foreach (var condition in oldDict.Values)
                Settings.Conditions.TryAdd(n++, condition);
            Logger.Debug($"Removed Condition for ID '{id}'");

            return true;
        }

        public virtual void RegisterRessources()
        {
            ConditionStore.RegisterRessources();
        }

        public virtual void DeregisterRessources()
        {
            ConditionStore.DeregisterRessources();
        }

        public static ElementManipulator CreateInstance(ELEMENT_MANIPULATOR type, ModelManipulator model, DisplayElement parent)
        {
            ElementManipulator element = null;

            if (type == ELEMENT_MANIPULATOR.COLOR)
            {
                element = new ManipulatorColor(model ?? new ModelManipulator(type), parent);
            }
            else if (type == ELEMENT_MANIPULATOR.VISIBLE)
            {
                element = new ManipulatorVisible(model ?? new ModelManipulator(type), parent);
            }
            else if (type == ELEMENT_MANIPULATOR.INDICATOR)
            {
                element = new ManipulatorIndicator(model ?? new ModelManipulator(type), parent);
                if (element.Settings.IsNewModel && parent != null)
                {
                    if (parent.Parent.CanvasSize.Y == 100)
                    {
                        element.Settings.IndicatorSize *= 1.5f;
                        element.Settings.IndicatorLineSize *= 1.5f;
                    }
                    else if (parent.Parent.CanvasSize.X == 144)
                    {
                        element.Settings.IndicatorSize *= 2.0f;
                        element.Settings.IndicatorLineSize *= 2.0f;
                    }
                    element.Settings.IndicatorScale = parent.Settings.GaugeValueScale;
                }
            }
            else if (type == ELEMENT_MANIPULATOR.TRANSPARENCY)
            {
                element = new ManipulatorTransparency(model ?? new ModelManipulator(type), parent);
            }
            else if (type == ELEMENT_MANIPULATOR.ROTATE)
            {
                element = new ManipulatorRotate(model ?? new ModelManipulator(type), parent);
            }
            else if (type == ELEMENT_MANIPULATOR.FORMAT)
            {
                element = new ManipulatorFormat(model ?? new ModelManipulator(type), parent);
            }
            else if (type == ELEMENT_MANIPULATOR.SIZEPOS)
            {
                element = new ManipulatorSizePos(model ?? new ModelManipulator(type), parent);
            }
            else if (type == ELEMENT_MANIPULATOR.FLASH)
            {
                element = new ManipulatorFlash(model ?? new ModelManipulator(), parent);
            }

            if (element?.Settings?.IsNewModel == true)
            {
                element.Settings.IsNewModel = false;
                if (parent != null)
                    parent.Parent.SettingModelUpdated = true;
            }

            return element;
        }
    }
}
