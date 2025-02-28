using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Variables;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Elements
{
    public class ElementValue(ModelDisplayElement model, ActionMeta parent) : DisplayElement(model, parent)
    {
        public virtual Font Font { get { return Settings.GetFont(); } }
        public virtual ManagedVariable Variable { get; set; } = null;
        public virtual ValueFormat ValueFormat { get; set; } = model.ValueFormat.Copy();

        public override void SetDefaultState()
        {
            base.SetDefaultState();
            ValueFormat = Settings.ValueFormat.Copy();
        }

        public override bool HasChanges()
        {
            return base.HasChanges() || Variable?.IsChanged == true;
        }

        protected override bool IgnoreRender()
        {
            return base.IgnoreRender() || Variable == null || ValueFormat == null;
        }

        protected override void Render(Renderer render)
        {
            render.DrawText(ValueFormat.FormatValue(Variable), Font, Color, GetRectangle(render), Center, Scale, Settings.TextHorizontalAlignment, Settings.TextVerticalAlignment);
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (Variable == null)
            {
                Logger.Verbose($"Register Variable '{Settings.ValueAddress}'");
                Variable = App.PluginController.VariableManager.RegisterVariable(new ManagedAddress(Settings.ValueAddress));
            }
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (Variable != null)
            {
                Logger.Verbose($"Deregister Variable '{Variable.Address}'");
                App.PluginController.VariableManager.DeregisterVariable(Variable.Address);
                Variable = null;
            }
        }
    }
}
