using System;
using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayRadio : HandlerDisplaySwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayRadio Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayRadio] Read1: {Settings.AddressRadioActiv} | Read2: {Settings.AddressRadioStandby} | Write: {SwitchSettings.AddressAction}"; } }
        public override string Address { get { return Settings.AddressRadioActiv; } }

        protected int ticksIndication = 0;
        protected static readonly int ticksActive = 16;
        protected bool wasPushed = false;

        public HandlerDisplayRadio(string context, ModelDisplayRadio settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(SwitchSettings.AddressAction) && !string.IsNullOrEmpty(Settings.AddressRadioActiv) && !string.IsNullOrEmpty(Settings.AddressRadioStandby);
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            ValueManager.RegisterValue(ID.Standby, Settings.AddressRadioStandby);
        }

        public override void Deregister(ImageManager imgManager)
        {
            base.Deregister(imgManager);

            ValueManager.DeregisterValue(ID.Standby);
        }

        public override void Update(ImageManager imgManager)
        {
            base.Update(imgManager);

            ValueManager.UpdateValueAddress(ID.Standby, Settings.AddressRadioStandby);
        }

        public override bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            bool result = base.OnButtonUp(ipcManager, tick);
            if (result)
                wasPushed = true;

            return result;
        }

        protected override void Redraw(ImageManager imgManager)
        {
            if (!ValueManager.IsChanged(ID.Active) && !ValueManager.IsChanged(ID.Standby) && !ForceUpdate && !wasPushed)
                return;

            string valueAct = ValueManager[ID.Active];
            if (Settings.DecodeBCD)
                valueAct = ModelDisplay.ConvertFromBCD(valueAct);
            valueAct = Settings.ScaleValue(valueAct);
            valueAct = Settings.RoundValue(valueAct);
            valueAct = Settings.FormatValue(valueAct);

            string valueStb = ValueManager[ID.Standby];
            if (Settings.DecodeBCD && !Settings.StbyHasDiffFormat || Settings.StbyHasDiffFormat && Settings.DecodeBCDStby)
                valueStb = ModelDisplay.ConvertFromBCD(valueStb);
            valueStb = ModelDisplay.ScaleValue(valueStb, Settings.StbyHasDiffFormat ? Settings.ScalarStby : Settings.Scalar);
            valueStb = ModelDisplay.RoundValue(valueStb, Settings.StbyHasDiffFormat ? Settings.FormatStby : Settings.Format);
            valueStb = ModelDisplay.FormatValue(valueStb, Settings.StbyHasDiffFormat ? Settings.FormatStby : Settings.Format);

            string background = Settings.DefaultImage;
            if (wasPushed)
            {
                ticksIndication++;
                if (ticksIndication < ticksActive)
                    background = Settings.IndicationImage;
                else
                {
                    wasPushed = false;
                    ticksIndication = 0;
                }
            }

            if (lastText != valueAct + valueStb || ForceUpdate || wasPushed && ticksIndication < ticksActive)
            {
                Font fontAct = GetFont(FontStyle.Bold);
                Font fontStb = GetFont(FontStyle.Regular);

                Color colorAct;
                if (Settings.FontInherit && TitleParameters != null)
                    colorAct = ColorTranslator.FromHtml(TitleParameters.FontColor);
                else
                    colorAct = ColorTranslator.FromHtml(Settings.FontColor);

                Color colorStb;
                if (Settings.FontInherit && TitleParameters != null)
                    colorStb = GetDarkenedColor(TitleParameters.FontColor);
                else
                    colorStb = ColorTranslator.FromHtml(Settings.FontColorStby);

                ImageRenderer render = new(imgManager.GetImageObject(background, DeckType));
                render.DrawText(valueAct, fontAct, colorAct, Settings.GetRectangleText());
                render.DrawText(valueStb, fontStb, colorStb, ModelDisplayText.GetRectangleF(Settings.RectCoordStby));

                DrawImage = render.RenderImage64();
                IsRawImage = true;
                NeedRedraw = true;
                if (!wasPushed)
                    lastText = valueAct + valueStb;
                else
                    lastText = "";
                render.Dispose();
            }
        }

        public static Color GetDarkenedColor(string color, string subColor = "1f1f1f")
        {
            int clr = Convert.ToInt32(color.Replace("#", ""), 16);
            int sub = Convert.ToInt32(subColor, 16);

            return ColorTranslator.FromWin32(clr - sub);
        }

        private Font GetFont(FontStyle style)
        {
            if (Settings.FontInherit && TitleParameters != null)
                return new Font(TitleParameters.FontName, TitleParameters.FontSize, style);
            else
                return new Font(Settings.FontName, ModelDisplayText.GetNumValue(Settings.FontSize, 10), style);
        }
    }
}
