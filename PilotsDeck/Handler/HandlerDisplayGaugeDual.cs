using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayGaugeDual : HandlerDisplayGauge
    {
        public override ModelDisplayGauge GaugeSettings { get { return Settings; } }
        public new ModelDisplayGaugeDual Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayGaugeDual] Read1: {Address} | Read2: {Settings.Address2}"; } }

        public HandlerDisplayGaugeDual(string context, ModelDisplayGaugeDual settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(GaugeSettings.Address) && !string.IsNullOrEmpty(Settings.Address2);
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            ValueManager.RegisterValue(ID.Second, Settings.Address2);
        }

        public override void Deregister(ImageManager imgManager)
        {
            base.Deregister(imgManager);

            ValueManager.DeregisterValue(ID.Second);
        }

        public override void Update(ImageManager imgManager)
        {
            base.Update(imgManager);

            ValueManager.UpdateValueAddress(ID.Second, Settings.Address2);
        }

        protected override void DrawBar(string value, ImageRenderer render)
        {
            if (GaugeSettings.BarOrientation == (int)GaugeOrientation.LEFT)
                GaugeSettings.IndicatorFlip = false;
            else
                GaugeSettings.IndicatorFlip = true;

            base.DrawBar(value, render);

            value = ValueManager[ID.Second];
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);
            render.DrawBarIndicator(GaugeSettings.GetBar(), ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), ModelDisplayText.GetNumValue(GaugeSettings.IndicatorSize, 10), ModelDisplayText.GetNumValue(value, 0), min, max, !GaugeSettings.IndicatorFlip);
        }

        protected override void DrawArc(string value, ImageRenderer render)
        {
            base.DrawArc(value, render);

            value = ValueManager[ID.Second];
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);

            render.DrawArcIndicator(GaugeSettings.GetArc(), ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), ModelDisplayText.GetNumValue(GaugeSettings.IndicatorSize, 10), ModelDisplayText.GetNumValue(value, 0), min, max, !GaugeSettings.IndicatorFlip);
        }

        protected override void DrawText(string value, ImageRenderer render)
        {
            base.DrawText(value, render);

            if (!GaugeSettings.DrawArc)
            {
                value = ValueManager[ID.Second];
                if (GaugeSettings.DecodeBCD)
                    value = ModelDisplay.ConvertFromBCD(value);
                value = GaugeSettings.ScaleValue(value);

                if (GaugeSettings.ShowText)
                {
                    value = GaugeSettings.RoundValue(value);

                    GaugeSettings.GetFontParameters(TitleParameters, value, out Font drawFont, out Color drawColor);
                    render.DrawText(ModelDisplay.FormatValue(value, GaugeSettings.Format), drawFont, drawColor, ModelDisplayText.GetRectangleF(Settings.RectCoord2));
                }
            }
        }
    }
}
