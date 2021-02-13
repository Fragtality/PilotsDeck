using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayText : HandlerValue
    {
        public virtual ModelDisplayText TextSettings { get { return Settings; } }
        public ModelDisplayText Settings { get; protected set; }

        public override string Address { get { return TextSettings.Address; } }

        public override bool UseFont { get { return true; } }

        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue); } }
        protected string lastText = "";

        public HandlerDisplayText(string context, ModelDisplayText settings) : base(context, settings)
        {
            Settings = settings;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            if (TextSettings.HasIndication)
                imgManager.AddImage(TextSettings.IndicationImage);
        }

        public override void Deregister(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Deregister(imgManager, ipcManager);

            if (TextSettings.HasIndication)
                imgManager.RemoveImage(TextSettings.IndicationImage);
        }

        protected override void Redraw(ImageManager imgManager)
        {
            if (!IsChanged && !ForceUpdate)
                return;

            string value = CurrentValue;
            if (Settings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);

            value = TextSettings.ScaleValue(value);
            value = TextSettings.RoundValue(value);

            //evaluate value and set indication
            string background = DefaultImage;
            TextSettings.GetFontParameters(TitleParameters, out Font drawFont, out Color drawColor);

            string text = "";
            if (TextSettings.HasIndication && TextSettings.IndicationValue == value)
            {
                background = TextSettings.IndicationImage;
                if (!TextSettings.IndicationHideValue)
                {
                    text = TextSettings.FormatValue(value);
                    if (TextSettings.IndicationUseColor)
                        drawColor = ColorTranslator.FromHtml(TextSettings.IndicationColor);
                }
            }
            else
                text = TextSettings.FormatValue(value);

            if (text != lastText || ForceUpdate)
            {
                DrawImage = ImageTools.DrawText(text, imgManager.GetImageObject(background), drawFont, drawColor, TextSettings.GetRectangle());
                IsRawImage = true;
                NeedRedraw = true;
                lastText = text;
            }
        }
    }
}
