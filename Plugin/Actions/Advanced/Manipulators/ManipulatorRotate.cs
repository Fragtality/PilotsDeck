using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Variables;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorRotate(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public ManagedVariable RotateVariable { get; set; } = null;
        public bool RotateContinous { get { return Settings.RotateContinous; } }

        public override bool HasChanges()
        {
            return base.HasChanges() || (RotateContinous && RotateVariable?.IsChanged == true);
        }

        public override void ManipulateElement()
        {
            float rotation = Element.Rotation;
            bool empty = ConditionStore.Conditions.IsEmpty;
            bool compares = ConditionStore.Compare();

            if (RotateContinous && (empty || (!empty && compares)) && RotateVariable?.IsNumericValue == true)
            {
                float value = (float)RotateVariable.NumericValue;
                if (value < Settings.RotateMinValue || value > Settings.RotateMaxValue)
                    rotation = Settings.RotateAngleStart;
                else
                    rotation = ToolsRender.NormalizeAngle(Settings.RotateAngleStart + (ToolsRender.NormalizedRatio(value, Settings.RotateMinValue, Settings.RotateMaxValue) * Settings.RotateAngleSweep));

            }
            else if (!RotateContinous && compares)
                rotation = Settings.RotateToValue;

            if (rotation != Element.Rotation)
            {
                Element.Rotation = rotation;
                Logger.Verbose($"Rotation set to {Element.Rotation}");
            }
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (RotateVariable == null && Settings.RotateContinous)
            {
                Logger.Verbose($"Register Variable '{Settings.RotateAddress}'");
                RotateVariable = App.PluginController.VariableManager.RegisterVariable(new ManagedAddress(Settings.RotateAddress));
            }
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (RotateVariable != null)
            {
                Logger.Verbose($"Deregister Variable '{RotateVariable.Address}'");
                App.PluginController.VariableManager.DeregisterVariable(RotateVariable.Address);
                RotateVariable = null;
            }
        }
    }
}
