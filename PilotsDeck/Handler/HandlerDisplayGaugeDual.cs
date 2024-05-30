using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayGaugeDual(string context, ModelDisplayGaugeDual settings, StreamDeckType deckType) : HandlerDisplayGauge(context, settings, deckType)
    {
        public override ModelDisplayGauge GaugeSettings { get { return Settings; } }
        public new ModelDisplayGaugeDual Settings { get; protected set; } = settings;

        public override string ActionID { get { return $"(HandlerDisplayGaugeDual) ({Title.Trim()}) {(GaugeSettings.IsEncoder ? "(Encoder) " : "")}(Read1: {GaugeSettings.Address} / Read2: {Settings.Address2}) (HasAction: {HasAction}) (Action: {(ActionSwitchType)SwitchSettings.ActionType} / {Address}) (Long: {SwitchSettings.HasLongPress} / {(ActionSwitchType)SwitchSettings.ActionTypeLong} / {SwitchSettings.AddressActionLong})"; } }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(GaugeSettings.Address) && !string.IsNullOrEmpty(Settings.Address2);
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            ValueManager.AddValue(ID.GaugeSecond, Settings.Address2);
        }

        public override void Deregister()
        {
            base.Deregister();

            ValueManager.RemoveValue(ID.GaugeSecond);
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);

            ValueManager.UpdateValue(ID.GaugeSecond, Settings.Address2);
        }

        protected override void DrawBar(string value, ImageRenderer render, bool defaultImage = false)
        {
            if (GaugeSettings.BarOrientation == (int)GaugeOrientation.LEFT)
                GaugeSettings.IndicatorFlip = false;
            else
                GaugeSettings.IndicatorFlip = true;

            base.DrawBar(value, render);

            if (!defaultImage)
                value = ValueManager[ID.GaugeSecond];
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);
            render.DrawBarIndicator(GaugeSettings.GetBar(), ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), ModelDisplayText.GetNumValue(GaugeSettings.IndicatorSize, 10), ModelDisplayText.GetNumValue(value, 0), min, max, !GaugeSettings.IndicatorFlip);
        }

        protected override void DrawArc(string value, ImageRenderer render, bool defaultImage = false)
        {
            base.DrawArc(value, render);

            if (!defaultImage)
                value = ValueManager[ID.GaugeSecond];
            if (GaugeSettings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);
            value = GaugeSettings.ScaleValue(value);

            float min = ModelDisplayText.GetNumValue(GaugeSettings.MinimumValue, 0);
            float max = ModelDisplayText.GetNumValue(GaugeSettings.MaximumValue, 100);

            render.DrawArcIndicator(GaugeSettings.GetArc(), ColorTranslator.FromHtml(GaugeSettings.IndicatorColor), ModelDisplayText.GetNumValue(GaugeSettings.IndicatorSize, 10), ModelDisplayText.GetNumValue(value, 0), min, max, !GaugeSettings.IndicatorFlip);
        }

        protected override void DrawText(string value, ImageRenderer render, bool defaultImage = false)
        {
            base.DrawText(value, render);

            if (!GaugeSettings.DrawArc)
            {
                if (!defaultImage)
                    value = ValueManager[ID.GaugeSecond];
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
