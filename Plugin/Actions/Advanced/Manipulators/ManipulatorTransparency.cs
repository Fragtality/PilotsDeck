using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Variables;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorTransparency(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public ManagedVariable TransparencyVariable { get; set; } = null;
        public bool DynamicTransparency { get {  return Settings.DynamicTransparency; } }

        public override bool HasChanges()
        {
            return base.HasChanges() || (DynamicTransparency && TransparencyVariable?.IsChanged == true);
        }

        public override void ManipulateElement()
        {
            float transparency = Element.Settings.Transparency;
            bool empty = ConditionStore.Conditions.IsEmpty;
            bool compares = ConditionStore.Compare();

            if (DynamicTransparency && (empty || (!empty && compares)) && TransparencyVariable?.IsNumericValue == true)
            {
                float value = (float)TransparencyVariable.NumericValue;
                if (value < Settings.TransparencyMinValue)
                    transparency = 0.0f;
                else if (value > Settings.TransparencyMaxValue)
                    transparency = 1.0f;
                else
                    transparency = ToolsRender.NormalizedRatio(value, Settings.TransparencyMinValue, Settings.TransparencyMaxValue);
            }
            else if (!DynamicTransparency && compares)
                transparency = Settings.TransparencySetValue;

            if (transparency != Element.Transparency)
            {
                Element.Transparency = transparency;
                Logger.Verbose($"Transparency set to {Element.Transparency}");
            }
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (TransparencyVariable == null && Settings.DynamicTransparency)
            {
                Logger.Verbose($"Register Variable '{Settings.TransparencyAddress}'");
                TransparencyVariable = App.PluginController.VariableManager.RegisterVariable(Settings.TransparencyAddress);
            }
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (TransparencyVariable != null)
            {
                Logger.Verbose($"Deregister Variable '{TransparencyVariable.Address}'");
                App.PluginController.VariableManager.DeregisterVariable(TransparencyVariable.Address);
                TransparencyVariable = null;
            }
        }
    }
}
