using CFIT.AppTools;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources;
using PilotsDeck.StreamDeck.Messages;
using System.Drawing;


namespace PilotsDeck.Actions.Simple
{
    public class ActionDisplayGauge(StreamDeckEvent sdEvent) : ActionBaseSimple(sdEvent)
    {
        public override string ActionID { get { return $"{this.GetType().Name} (Title: {Title} | Gauge: {Settings.Address})"; } }

        protected bool DrawArcLast { get; set; } = false;

        protected override void CheckSettings()
        {
            DrawArcLast = Settings.DrawArc;

            if (Settings.SwitchOnCurrentValue)
            {
                Settings.SwitchOnCurrentValue = false;
                SettingModelUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(Settings.DefaultImage))
            {
                Settings.DefaultImage = @"Images/Empty.png";
                Settings.RectCoord = "7; 45; 60; 21";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.DefaultRect))
            {
                Settings.DefaultRect = "0; 0";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.CriticalRange) || Settings.CriticalRange.Split(';')?.Length < 2)
            {
                Settings.CriticalRange = "0; 10";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.WarnRange) || Settings.WarnRange.Split(';')?.Length < 2)
            {
                Settings.WarnRange = "10; 20";
                SettingModelUpdated = true;
            }
        }

        protected override void RegisterVariables()
        {
            base.RegisterVariables();

            RessourceStore.AddState(VariableID.Gauge, Settings.Address, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);

            if (Settings.UseColorSwitching)
                RessourceStore.AddState(VariableID.GaugeColor, Settings.AddressColorOff, Settings.StateColorOff);
        }

        public override void UpdateRessources()
        {
            base.UpdateRessources();

            if (DrawArcLast != Settings.DrawArc)
            {
                ResetCoords();
                DrawArcLast = Settings.DrawArc;
                SettingModelUpdated = true;
            }
        }

        protected override void UpdateVariables()
        {
            base.UpdateVariables();

            RessourceStore.UpdateState(VariableID.Gauge, Settings.Address, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);

            RessourceStore.RemoveState(VariableID.GaugeColor);
            if (Settings.UseColorSwitching)
                RessourceStore.AddState(VariableID.GaugeColor, Settings.AddressColorOff, Settings.StateColorOff);
        }

        protected override void DeregisterVariables()
        {
            base.DeregisterVariables();

            RessourceStore.RemoveState(VariableID.Gauge);

            if (Settings.UseColorSwitching)
                RessourceStore.RemoveState(VariableID.GaugeColor);
        }

        public override void Refresh()
        {
             if (!RessourceStore.HasChanges() && !NeedRefresh)
                return;

            var stateGauge = RessourceStore.GetState(VariableID.Gauge);
            
            Renderer render = new(CanvasInfo);
            if (Settings.UseImageMapping)
                render.DrawImage(RessourceStore.GetImageMap(ImageID.Map)?.GetMappedImage(stateGauge.StringValue, null) ?? ImageManager.DEFAULT_WAIT, Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);
            else
                render.DrawImage(RessourceStore.GetImage(ImageID.Background), Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);

            if (Settings.DrawArc)
            {
                RenderArc drawArc = new(Settings, Conversion.ToFloat(stateGauge?.ScaledValue()), render);
                DrawText(stateGauge, render);
                DrawArc(drawArc);
            }
            else
            {
                RenderBar drawBar = new(Settings, Conversion.ToFloat(stateGauge?.ScaledValue()), render);
                DrawBar(drawBar);
                DrawText(stateGauge, render);
            }

            if (Settings.HasAction && Settings.IsGuarded)
                RenderGuard(render, stateGauge?.ScaledValue() ?? "");

            DrawTitle(render);

            RenderImage64 = render.RenderImage64();
            render.Dispose();
            NeedRedraw = true;
            NeedRefresh = false;
        }

        protected virtual void DrawBar(RenderBar drawBar)
        {
            bool useOffColor = Settings.UseColorSwitching && RessourceStore.GetState(VariableID.GaugeColor)?.Compares() == true;
            Color drawColor;
            if (useOffColor)
                drawColor = ColorTranslator.FromHtml(Settings.GaugeColorOff);
            else
                drawColor = ColorTranslator.FromHtml(Settings.GaugeColor);

            drawBar.Renderer.RotateCenter((float)Settings.BarOrientation, new PointF(0, 0));
            drawBar.DrawBar(drawColor);

            if (Settings.DrawWarnRange && !useOffColor)
                drawBar.DrawBarRanges(GetColorRange(), GetWarnRange(), Settings.SymmRange);

            if (Settings.CenterLine)
                drawBar.DrawBarCenterLine(ColorTranslator.FromHtml(Settings.CenterLineColor), Conversion.ToFloat(Settings.CenterLineThickness, 2));

            drawBar.DrawBarIndicatorTriangle(ColorTranslator.FromHtml(Settings.IndicatorColor), Conversion.ToFloat(Settings.IndicatorSize, 10), 0, Settings.IndicatorFlip);
        }

        protected virtual void DrawArc(RenderArc drawArc)
        {
            bool useOffColor = Settings.UseColorSwitching && RessourceStore.GetState(VariableID.GaugeColor)?.Compares() == true;
            Color drawColor;
            if (useOffColor)
                drawColor = ColorTranslator.FromHtml(Settings.GaugeColorOff);
            else
                drawColor = ColorTranslator.FromHtml(Settings.GaugeColor);

            drawArc.DrawArc(drawColor);

            if (Settings.DrawWarnRange && !useOffColor)
                drawArc.DrawArcRanges(GetColorRange(), GetWarnRange(), Settings.SymmRange);

            if (Settings.CenterLine)
                drawArc.DrawArcCenterLine(ColorTranslator.FromHtml(Settings.CenterLineColor), Conversion.ToFloat(Settings.CenterLineThickness, 2));

            drawArc.DrawArcIndicatorTriangle(ColorTranslator.FromHtml(Settings.IndicatorColor), Conversion.ToFloat(Settings.IndicatorSize, 10), 0, Settings.IndicatorFlip);
        }

        protected virtual void DrawText(ValueState valueState, Renderer render)
        {
            if (Settings.ShowText)
            {
                string value = valueState?.StringValue ?? "";

                if (Settings.BarOrientation == GaugeOrientation.LEFT && !Settings.DrawArc)
                    render.RotateCenter(180, new PointF(0, 0));

                GetFontParameters(value, out Font drawFont, out Color drawColor);
                string text = valueState?.FormattedValue ?? "";
                text = ValueState.GetValueMapped(text, Settings.ValueMappings);
                render.DrawText(text, drawFont, drawColor, Settings.GetRectangleFirst(), CenterType.NONE);
            }
        }

        public virtual void ResetCoords()
        {
            if (!Settings.DrawArc)
            {
                Settings.GaugeSize = "58; 10";
                Settings.RectCoord = "7; 45; 60; 21";
            }
            else
            {
                Settings.GaugeSize = "48; 6";
                Settings.RectCoord = "16; 27; 60; 21";
            }
        }

        public virtual void GetFontParameters(string value, out Font drawFont, out Color drawColor)
        {
            drawFont = GetFont();
            drawColor = GetFontColor();

            if (Settings.UseWarnColors && Settings.DrawWarnRange)
            {
                if (ValueWithinRange(value, Settings.WarnRange))
                    drawColor = ColorTranslator.FromHtml(Settings.WarnColor);
                else if (ValueWithinRange(value, Settings.CriticalRange))
                    drawColor = ColorTranslator.FromHtml(Settings.CriticalColor);

                if (Settings.SymmRange && Conversion.IsNumberF(Settings.MinimumValue, out float minimumTotal) && Conversion.IsNumberF(Settings.MaximumValue, out float maximumTotal))
                {
                    float[][] ranges = GetWarnRange();

                    string rangeStr = Conversion.ToString(ToolsRender.NormalizedValue(maximumTotal, minimumTotal) - ToolsRender.NormalizedValue(ranges[1][1], minimumTotal));
                    rangeStr += ";" + Conversion.ToString(ToolsRender.NormalizedValue(maximumTotal, minimumTotal) - ToolsRender.NormalizedValue(ranges[1][0], minimumTotal));
                    if (ValueWithinRange(value, rangeStr))
                        drawColor = ColorTranslator.FromHtml(Settings.WarnColor);
                    else
                    {
                        rangeStr = Conversion.ToString(ToolsRender.NormalizedValue(maximumTotal, minimumTotal) - ToolsRender.NormalizedValue(ranges[0][1], minimumTotal));
                        rangeStr += ";" + Conversion.ToString(ToolsRender.NormalizedValue(maximumTotal, minimumTotal) - ToolsRender.NormalizedValue(ranges[0][0], minimumTotal));
                        if (ValueWithinRange(value, rangeStr))
                            drawColor = ColorTranslator.FromHtml(Settings.CriticalColor);
                    }
                }
            }
        }

        public static bool ValueWithinRange(string value, string range)
        {
            float[] rangeNum = Conversion.ToFloatArray(range, [0, 100]);
            if (Conversion.IsNumber(value, out double valueNum) && rangeNum?.Length == 2)
            {
                if (rangeNum[0] <= valueNum && valueNum <= rangeNum[1])
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public Color[] GetColorRange()
        {
            return [ColorTranslator.FromHtml(Settings.CriticalColor), ColorTranslator.FromHtml(Settings.WarnColor)];
        }

        public float[][] GetWarnRange()
        {
            return [Conversion.ToFloatArray(Settings.CriticalRange, [0, 10]), Conversion.ToFloatArray(Settings.WarnRange, [11, 25])];
        }
    }
}
