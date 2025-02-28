using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Images;
using PilotsDeck.Resources.Variables;

namespace PilotsDeck.Actions.Advanced.Manipulators
{
    public class ManipulatorIndicator(ModelManipulator model, DisplayElement parent) : ElementManipulator(model, parent)
    {
        public ManagedVariable IndicatorVariable { get; set; } = null;
        public ManagedImage IndicatorImage { get; set; } = null;
        protected ElementGauge GaugeElement { get { return Element as ElementGauge; } }

        public override bool HasChanges()
        {
            return base.HasChanges() || IndicatorVariable?.IsChanged == true;
        }

        public override void RenderManipulator(Renderer render)
        {
            if (!ConditionStore.Conditions.IsEmpty && !ConditionStore.Compare())
                return;

            float value = (float)(IndicatorVariable?.NumericValue ?? 0.0) * Settings.IndicatorScale;
            if (!Element.Settings.GaugeIsArc)
            {
                RenderBar renderBar = new(Element.Settings, value, render);
                if (Settings.IndicatorReverse)
                    render.MirrorX(Element.Position);

                if (Settings.IndicatorType == IndicatorType.CIRCLE)
                    renderBar.DrawBarIndicatorCirle(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorLineSize, Settings.IndicatorOffset);
                else if (Settings.IndicatorType == IndicatorType.DOT)
                    renderBar.DrawBarIndicatorFullCircle(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorOffset);
                else if (Settings.IndicatorType == IndicatorType.LINE)
                    renderBar.DrawBarIndicatorLine(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorLineSize, Settings.IndicatorOffset);
                else if (Settings.IndicatorType == IndicatorType.IMAGE && IndicatorImage != null)
                    renderBar.DrawBarIndicatorImage(IndicatorImage.GetImageVariant(render.PreferredVariant), Settings.IndicatorSize, Settings.IndicatorOffset, Settings.IndicatorFlip);
                else
                    renderBar.DrawBarIndicatorTriangle(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorOffset, Settings.IndicatorFlip);
                
                if (Settings.IndicatorReverse)
                    render.MirrorX(Element.Position);
            }
            else
            {
                RenderArc renderArc = new(Element.Settings, value, render);
                if (Settings.IndicatorReverse)
                {
                    renderArc.StartAngle += renderArc.SweepAngle;
                    renderArc.SweepAngle *= -1;
                }

                if (Settings.IndicatorType == IndicatorType.CIRCLE)
                    renderArc.DrawArcIndicatorCircle(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorLineSize, Settings.IndicatorOffset);
                else if (Settings.IndicatorType == IndicatorType.DOT)
                    renderArc.DrawArcIndicatorFullCircle(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorOffset);
                else if (Settings.IndicatorType == IndicatorType.LINE)
                            renderArc.DrawArcIndicatorLine(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorLineSize, Settings.IndicatorOffset);
                else if (Settings.IndicatorType == IndicatorType.IMAGE && IndicatorImage != null)
                    renderArc.DrawArcIndicatorImage(IndicatorImage.GetImageVariant(render.PreferredVariant), Settings.IndicatorSize, Settings.IndicatorOffset, Settings.IndicatorFlip);
                else
                    renderArc.DrawArcIndicatorTriangle(Settings.GetIndicatorColor(), Settings.IndicatorSize, Settings.IndicatorOffset, Settings.IndicatorFlip);
            }
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (IndicatorVariable == null)
            {
                Logger.Verbose($"Register Variable '{Settings.IndicatorAddress}'");
                IndicatorVariable = App.PluginController.VariableManager.RegisterVariable(new ManagedAddress(Settings.IndicatorAddress));
            }

            if (Settings.IndicatorType == IndicatorType.IMAGE && IndicatorImage == null)
                IndicatorImage = App.PluginController.ImageManager.RegisterImage(Settings.IndicatorImage);
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (IndicatorVariable != null)
            {
                Logger.Verbose($"Deregister Variable '{IndicatorVariable.Address}'");
                App.PluginController.VariableManager.DeregisterVariable(IndicatorVariable.Address);
                IndicatorVariable = null;
            }

            if (IndicatorImage != null)
            {
                App.PluginController.ImageManager.DeregisterImage(IndicatorImage.RequestedFile);
                IndicatorImage = null;
            }
        }
    }
}
