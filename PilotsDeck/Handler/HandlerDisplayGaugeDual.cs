using Serilog;
using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayGaugeDual : HandlerDisplayGauge
    {
        public override ModelDisplayGauge GaugeSettings { get { return Settings; } }
        public new ModelDisplayGaugeDual Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayGaugeDual] Read1: {Address} | Read2: {Settings.Address2}"; } }
        public virtual string CurrentValue2 { get; protected set; } = null;
        public virtual string LastAddress2 { get; protected set; }

        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue) && !string.IsNullOrEmpty(CurrentValue2); } }

        public HandlerDisplayGaugeDual(string context, ModelDisplayGaugeDual settings) : base(context, settings)
        {
            Settings = settings;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(GaugeSettings.Address) && !string.IsNullOrEmpty(Settings.Address2);
        }

        public override void RegisterAddress(IPCManager ipcManager)
        {
            base.RegisterAddress(ipcManager);

            ipcManager.RegisterAddress(Settings.Address2, AppSettings.groupStringRead);
            LastAddress2 = Settings.Address2;
        }

        public override void UpdateAddress(IPCManager ipcManager)
        {
            base.UpdateAddress(ipcManager);

            LastAddress2 = UpdateAddress(ipcManager, LastAddress2, Settings.Address2);
        }

        public override void DeregisterAddress(IPCManager ipcManager)
        {
            base.DeregisterAddress(ipcManager);

            ipcManager.DeregisterValue(Settings.Address2);
            if (Settings.Address2 != LastAddress2)
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Settings.Address2} != {LastAddress2} ] ");
        }

        public override void RefreshValue(IPCManager ipcManager)
        {
            int results = 0;
            
            if (RefreshValue(ipcManager, Address, out string currentValue))
                results++;
            CurrentValue = currentValue;

            if (RefreshValue(ipcManager, Settings.Address2, out currentValue))
                results++;
            CurrentValue2 = currentValue;

            if (results > 0)
                IsChanged = true;
        }

        protected override void DrawBar(string value, ImageRenderer render)
        {
            if (GaugeSettings.BarOrientation == (int)GaugeOrientation.LEFT)
                GaugeSettings.IndicatorFlip = false;
            else
                GaugeSettings.IndicatorFlip = true;

            base.DrawBar(value, render);

            value = CurrentValue2;
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            float min = ModelDisplayGauge.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayGauge.GetNumValue(GaugeSettings.MaximumValue, 100);
            render.DrawBarIndicator(GaugeSettings.GetRectangleBar(), ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), GaugeSettings.IndicatorSize, ModelDisplayGauge.GetNumValue(value, 0), min, max, !GaugeSettings.IndicatorFlip);
        }

        protected override void DrawArc(string value, ImageRenderer render)
        {
            base.DrawArc(value, render);

            value = CurrentValue2;
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            float min = ModelDisplayGauge.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayGauge.GetNumValue(GaugeSettings.MaximumValue, 100);

            render.DrawArcIndicator(GaugeSettings.GetArc(), ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), GaugeSettings.IndicatorSize, ModelDisplayGauge.GetNumValue(value, 0), min, max, !GaugeSettings.IndicatorFlip);
        }

        protected override void DrawText(string value, ImageRenderer render)
        {
            base.DrawText(value, render);

            if (!GaugeSettings.DrawArc)
            {
                value = CurrentValue2;
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
