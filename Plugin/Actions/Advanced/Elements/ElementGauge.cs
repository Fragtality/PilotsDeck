using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Variables;
using System.Drawing;

namespace PilotsDeck.Actions.Advanced.Elements
{
    public class ElementGauge(ModelDisplayElement model, ActionMeta parent) : DisplayElement(model, parent)
    {
        public virtual ManagedVariable GaugeSizeVariable { get; set; } = null;
        public virtual float GaugeValue { get { return (float)(GaugeSizeVariable?.NumericValue ?? 0.0) * Settings.GaugeValueScale; } }
        public virtual float GaugeDrawSize { get; set; }

        protected override RectangleF GetRectangle(Renderer render)
        {
            return new RectangleF(Position.X, Position.Y, render.DeviceCanvas.X, render.DeviceCanvas.Y);
        }

        public override bool HasChanges()
        {
            return base.HasChanges() || (Settings.UseGaugeDynamicSize && GaugeSizeVariable?.IsChanged == true);
        }        

        protected override void Render(Renderer render)
        {
            if (!Settings.GaugeIsArc)
                RenderBar(render);
            else
                RenderArc(render);
        }

        protected virtual void RenderBar(Renderer render)
        {
            RenderBar renderBar = new(Settings, GaugeValue, render);

            if (Settings.GaugeRevereseDirection)
                render.MirrorX(Position);

            if (Settings.UseGaugeDynamicSize && GaugeSizeVariable != null)
            {
                renderBar.DynamicWidth = renderBar.Width * ToolsRender.NormalizedRatio(GaugeValue, Settings.GaugeValueMin, Settings.GaugeValueMax);
                renderBar.DrawBar(Color);
            }
            else
                renderBar.DrawBar(Color);

            if (Settings.GaugeColorRanges.Count > 0)
            {
                Settings.GetRanges(out Color[] colors, out float[][] ranges);
                renderBar.DrawBarRanges(colors, ranges);
            }

            if (Settings.GaugeMarkers.Count > 0)
            {
                foreach (var marker in Settings.GaugeMarkers)
                    renderBar.DrawBarLine(marker.GetColor(), marker.Size, marker.Height, marker.Offset, ToolsRender.NormalizedRatio(marker.ValuePosition, Settings.GaugeValueMin, Settings.GaugeValueMax));
            }

            if (Settings.GaugeRevereseDirection)
                render.MirrorX(Position);
        }

        protected virtual void RenderArc(Renderer render)
        {
            RenderArc renderArc = new(Settings, GaugeValue, render);

            if (Settings.GaugeRevereseDirection)
            {
                renderArc.StartAngle += renderArc.SweepAngle;
                renderArc.SweepAngle *= -1;
            }

            if (Settings.UseGaugeDynamicSize && GaugeSizeVariable != null)
            {
                renderArc.SweepAngle *= ToolsRender.NormalizedRatio(GaugeValue, Settings.GaugeValueMin, Settings.GaugeValueMax);
                renderArc.DrawArc(Color);
                renderArc.SweepAngle = Settings.GaugeAngleSweep;
                if (Settings.GaugeRevereseDirection)
                    renderArc.SweepAngle *= -1;
            }
            else
            {
                renderArc.DrawArc(Color);
                renderArc.FixedRanges = true;
            }

            if (Settings.GaugeColorRanges.Count > 0)
            {
                Settings.GetRanges(out Color[] colors, out float[][] ranges);
                renderArc.DrawArcRanges(colors, ranges);
            }

            if (Settings.GaugeMarkers.Count > 0)
            {
                foreach (var marker in Settings.GaugeMarkers)
                    renderArc.DrawArcLine(marker);
            }
        }

        public override void RegisterRessources()
        {
            base.RegisterRessources();
            if (Settings.UseGaugeDynamicSize && GaugeSizeVariable == null)
            {
                Logger.Verbose($"Register Variable '{Settings.GaugeSizeAddress}'");
                GaugeSizeVariable = App.PluginController.VariableManager.RegisterVariable(Settings.GaugeSizeAddress);
            }
        }

        public override void DeregisterRessources()
        {
            base.DeregisterRessources();
            if (GaugeSizeVariable != null)
            {
                Logger.Verbose($"Deregister Variable '{GaugeSizeVariable.Address}'");
                App.PluginController.VariableManager.DeregisterVariable(GaugeSizeVariable.Address);
                GaugeSizeVariable = null;
            }
        }
    }
}
