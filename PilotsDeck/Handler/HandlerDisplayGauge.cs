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
        public override bool UseFont { get { return true; } }
        public virtual string DefaultImageRender { get; set; }

        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue); } }
        protected string lastText = "";

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
        }

        protected virtual void RenderDefaultImage(ImageManager imgManager)
        {
            ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(GaugeSettings.DefaultImage));
            render.Rotate(GaugeSettings.BarOrientation);
            render.DrawBar(ColorTranslator.FromHtml(GaugeSettings.BarColor), GaugeSettings.GetRectangleBar());
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

            ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(GaugeSettings.DefaultImage));
            string value = CurrentValue;

            DrawBar(value, render);

            DrawImage = render.RenderImage64();
            render.Dispose();
            IsRawImage = true;
            NeedRedraw = true;
            
            //sw.Stop();
            //Log.Logger.Verbose($"Time for Gauge-Frame: {sw.Elapsed.TotalMilliseconds}ms");
        }

        protected virtual void DrawBar(string value, ImageRenderer render)
        {
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);

            value = GaugeSettings.ScaleValue(value);

            RectangleF drawRect = GaugeSettings.GetRectangleBar();
            float min = ModelDisplayGauge.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayGauge.GetNumValue(GaugeSettings.MaximumValue, 100);

            render.Rotate(GaugeSettings.BarOrientation);
            render.DrawBar(ColorTranslator.FromHtml(GaugeSettings.BarColor), drawRect);

            if (GaugeSettings.DrawWarnRange)
                render.DrawBarStages(drawRect, GaugeSettings.GetColorRange(), GaugeSettings.GetWarnRange(), min, max, GaugeSettings.SymmRange);

            if (GaugeSettings.CenterLine)
                render.DrawBarCenterLine(drawRect, ColorTranslator.FromHtml(GaugeSettings.CenterLineColor), GaugeSettings.CenterLineThickness);

            render.DrawBarIndicator(drawRect, ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), GaugeSettings.IndicatorSize, ModelDisplayGauge.GetNumValue(value, 0), min, max, GaugeSettings.IndicatorFlip);

            if (GaugeSettings.ShowText)
            {
                value = GaugeSettings.RoundValue(value);

                if (GaugeSettings.BarOrientation == (int)GaugeOrientation.LEFT)
                    render.Rotate(180);

                GaugeSettings.GetFontParameters(TitleParameters, value, out Font drawFont, out Color drawColor);
                render.DrawText(ModelDisplay.FormatValue(value, GaugeSettings.Format), drawFont, drawColor, ModelDisplayText.GetRectangle(GaugeSettings.RectCoord));
            }
        }
    }
}
