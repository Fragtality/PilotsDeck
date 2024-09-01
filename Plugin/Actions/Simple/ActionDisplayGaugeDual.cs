using PilotsDeck.Plugin.Render;
using PilotsDeck.StreamDeck.Messages;
using PilotsDeck.Tools;
using System.Drawing;

namespace PilotsDeck.Actions.Simple
{
    public class ActionDisplayGaugeDual(StreamDeckEvent sdEvent) : ActionDisplayGauge(sdEvent)
    {
        protected override void RegisterVariables()
        {
            base.RegisterVariables();

            RessourceStore.AddState(VariableID.GaugeSecond, Settings.Address2, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);
        }

        protected override void UpdateVariables()
        {
            base.UpdateVariables();

            RessourceStore.UpdateState(VariableID.GaugeSecond, Settings.Address2, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);
        }

        protected override void DeregisterVariables()
        {
            base.DeregisterVariables();

            RessourceStore.RemoveState(VariableID.GaugeSecond);
        }

        protected override void DrawBar(RenderBar drawBar)
        {
            if (Settings.BarOrientation == GaugeOrientation.LEFT)
                Settings.IndicatorFlip = false;
            else
                Settings.IndicatorFlip = true;

            base.DrawBar(drawBar);

            drawBar.Value = Conversion.ToFloat(RessourceStore.GetState(VariableID.GaugeSecond)?.ScaledValue());
            drawBar.DrawBarIndicatorTriangle(ColorTranslator.FromHtml(Settings.IndicatorColor), Conversion.ToFloat(Settings.IndicatorSize, 10), 0, !Settings.IndicatorFlip);
        }

        protected override void DrawArc(RenderArc drawArc)
        {
            base.DrawArc(drawArc);

            drawArc.Value = Conversion.ToFloat(RessourceStore.GetState(VariableID.GaugeSecond)?.ScaledValue());
            drawArc.DrawArcIndicatorTriangle(ColorTranslator.FromHtml(Settings.IndicatorColor), Conversion.ToFloat(Settings.IndicatorSize, 10), 0, !Settings.IndicatorFlip);
        }

        protected override void DrawText(ValueState valueState, Renderer render)
        {
            base.DrawText(valueState, render);

            if (!Settings.DrawArc)
            {
                if (Settings.ShowText)
                {
                    valueState = RessourceStore.GetState(VariableID.GaugeSecond) ?? valueState;
                    string value = valueState?.StringValue ?? "";

                    GetFontParameters(value, out Font drawFont, out Color drawColor);
                    render.DrawText(valueState.FormattedValue, drawFont, drawColor, Settings.GetRectangleSecond());
                }
            }
        }
    }
}
