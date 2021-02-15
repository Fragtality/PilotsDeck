using System;
using System.Diagnostics;
using System.Drawing;
using Serilog;

namespace PilotsDeck
{
    public class HandlerDisplayGauge : HandlerValue
    {
        public virtual ModelDisplayGauge GaugeSettings { get { return Settings; } }
        public ModelDisplayGauge Settings { get; protected set; }

        public override string Address { get { return GaugeSettings.Address; } }
        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayGauge] Read: {Address}"; } }
        public override bool UseFont { get { return true; } }
        public virtual string DefaultImageRender { get; set; }

        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue); } }
        protected string lastText = "";
        protected bool IsArc = false;

        public HandlerDisplayGauge(string context, ModelDisplayGauge settings) : base(context, settings)
        {
            Settings = settings;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);
            RenderDefaultImage(imgManager);
        }

        public override void Update(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Update(imgManager, ipcManager);
            RenderDefaultImage(imgManager);
            NeedRedraw = true;

            if (IsArc != GaugeSettings.DrawArc)
            {
                GaugeSettings.ResetCoords();
                IsArc = GaugeSettings.DrawArc;
                UpdateSettingsModel = true;
            }
        }

        protected virtual void RenderDefaultImage(ImageManager imgManager)
        {
            ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(GaugeSettings.DefaultImage));
            
            if (GaugeSettings.DrawArc)
            {
                render.DrawArc(GaugeSettings.GetArc(), ColorTranslator.FromHtml(GaugeSettings.GaugeColor));
            }
            else
            {
                render.Rotate(GaugeSettings.BarOrientation);
                render.DrawBar(ColorTranslator.FromHtml(GaugeSettings.GaugeColor), GaugeSettings.GetRectangleBar());
            }
            
            DefaultImageRender = render.RenderImage64();
            render.Dispose();
        }

        public override void SetDefault()
        {
            if (DrawImage != DefaultImageRender)
            {
                DrawImage = DefaultImageRender;
                IsRawImage = true;
                NeedRedraw = true;
            }
        }

        protected override void Redraw(ImageManager imgManager)
        {
            if (!IsChanged && !ForceUpdate)
                return;

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            
            string value = CurrentValue;
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(GaugeSettings.DefaultImage));

            if (GaugeSettings.DrawArc)
            {
                DrawText(value, render);
                DrawArc(value, render);
            }
            else
            {
                DrawBar(value, render);
                DrawText(value, render);
            }

            DrawImage = render.RenderImage64();
            render.Dispose();
            IsRawImage = true;
            NeedRedraw = true;
            
            //sw.Stop();
            //Log.Logger.Verbose($"Time for Gauge-Frame: {sw.Elapsed.TotalMilliseconds}ms [{ActionID}]");
        }

        protected virtual void DrawBar(string value, ImageRenderer render)
        {
            RectangleF drawRect = GaugeSettings.GetRectangleBar();
            float min = ModelDisplayGauge.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayGauge.GetNumValue(GaugeSettings.MaximumValue, 100);

            render.Rotate(GaugeSettings.BarOrientation);
            render.DrawBar(ColorTranslator.FromHtml(GaugeSettings.GaugeColor), drawRect);

            if (GaugeSettings.DrawWarnRange)
                render.DrawBarRanges(drawRect, GaugeSettings.GetColorRange(), GaugeSettings.GetWarnRange(), min, max, GaugeSettings.SymmRange);

            if (GaugeSettings.CenterLine)
                render.DrawBarCenterLine(drawRect, ColorTranslator.FromHtml(GaugeSettings.CenterLineColor), GaugeSettings.CenterLineThickness);

            render.DrawBarIndicator(drawRect, ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), GaugeSettings.IndicatorSize, ModelDisplayGauge.GetNumValue(value, 0), min, max, GaugeSettings.IndicatorFlip);
        }

        protected virtual void DrawArc(string value, ImageRenderer render)
        {
            Arc drawArc = GaugeSettings.GetArc();
            float min = ModelDisplayGauge.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayGauge.GetNumValue(GaugeSettings.MaximumValue, 100);
            
            render.DrawArc(drawArc, ColorTranslator.FromHtml(GaugeSettings.GaugeColor));

            if (GaugeSettings.DrawWarnRange)
                render.DrawArcRanges(drawArc, GaugeSettings.GetColorRange(), GaugeSettings.GetWarnRange(), min, max, GaugeSettings.SymmRange);
            
            if (GaugeSettings.CenterLine)
                render.DrawArcCenterLine(drawArc, ColorTranslator.FromHtml(GaugeSettings.CenterLineColor), GaugeSettings.CenterLineThickness);

            render.DrawArcIndicator(drawArc, ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), GaugeSettings.IndicatorSize, ModelDisplayGauge.GetNumValue(value, 0), min, max, GaugeSettings.IndicatorFlip);
        }

        protected virtual void DrawText(string value, ImageRenderer render)
        {
            if (GaugeSettings.ShowText)
            {
                value = GaugeSettings.RoundValue(value);

                if (GaugeSettings.BarOrientation == (int)GaugeOrientation.LEFT && !GaugeSettings.DrawArc)
                    render.Rotate(180);

                GaugeSettings.GetFontParameters(TitleParameters, value, out Font drawFont, out Color drawColor);
                render.DrawText(ModelDisplay.FormatValue(value, GaugeSettings.Format), drawFont, drawColor, ModelDisplayText.GetRectangle(GaugeSettings.RectCoord));
            }
        }
    }
}
