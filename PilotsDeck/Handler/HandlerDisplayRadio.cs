using System;
using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayRadio : HandlerDisplaySwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayRadio Settings { get; protected set; }

        public override string ActionID { get { return $"(HandlerDisplayRadio) ({Title.Trim()}) {(TextSettings.IsEncoder ? "(Encoder) " : "")}(Active: {Settings.AddressRadioActiv} / Standby: {Settings.AddressRadioStandby}) (Action: {(ActionSwitchType)SwitchSettings.ActionType} / {Address}) (Long: {SwitchSettings.HasLongPress} / {(ActionSwitchType)SwitchSettings.ActionTypeLong} / {SwitchSettings.AddressActionLong})"; } }
        public override string Address { get { return Settings.AddressRadioActiv; } }

        protected int ticksIndication = 0;
        protected static readonly int ticksActive = 16;
        protected string lastActive = "";
        protected bool firstLoad = true;

        public HandlerDisplayRadio(string context, ModelDisplayRadio settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            TextSettings.HasIndication = true;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(SwitchSettings.AddressAction) && !string.IsNullOrEmpty(Settings.AddressRadioActiv) && !string.IsNullOrEmpty(Settings.AddressRadioStandby);
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            ValueManager.AddValue(ID.Standby, Settings.AddressRadioStandby);
        }

        public override void Deregister()
        {
            base.Deregister();

            ValueManager.RemoveValue(ID.Standby);
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);

            ValueManager.UpdateValue(ID.Standby, Settings.AddressRadioStandby);
        }

        protected override void Redraw()
        {
            if (!ValueManager.IsChanged(ID.Active) && !ValueManager.IsChanged(ID.Standby) && !ForceUpdate && ticksIndication == 0)
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

            bool wasPushed = false;
            if (lastActive != ValueManager[ID.Active] && ticksIndication < ticksActive && !firstLoad)
            {
                ticksIndication++;
                if (ticksIndication < ticksActive)
                {
                    background = Settings.IndicationImage;
                    wasPushed = true;
                }
                else
                {
                    ticksIndication = 0;
                    lastActive = ValueManager[ID.Active];
                }
            }
            else if (firstLoad)
            {
                lastActive = ValueManager[ID.Active];
                firstLoad = false;
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

                ImageRenderer render = new(ImgManager.GetImageDefinition(background, DeckType));

                render.DrawText(valueAct, fontAct, colorAct, Settings.GetRectangleText());
                render.DrawText(valueStb, fontStb, colorStb, ModelDisplayText.GetRectangleF(Settings.RectCoordStby));

                if (IsEncoder)
                    DrawTitle(render, new PointF(100, 51.0f));

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

        protected override void RenderImages()
        {
            if (IsEncoder)
            {
                ImageRenderer render = new(ImgManager.GetImageDefinition(TextSettings.DefaultImage, DeckType));
                DrawTitle(render, new PointF(100, 51.0f));
                DefaultImageRender = render.RenderImage64();
                render.Dispose();

                render = new(ImgManager.GetImageDefinition(TextSettings.ErrorImage, DeckType));
                DrawTitle(render, new PointF(100, 51.0f));
                ErrorImageRender = render.RenderImage64();
                render.Dispose();
                IsRawImage = true;
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
