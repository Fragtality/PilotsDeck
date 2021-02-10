using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayText : HandlerDisplay
    {
        public virtual ModelDisplayText TextSettings { get { return Settings; } }
        public ModelDisplayText Settings { get; protected set; }

        protected string lastText = "";

        public HandlerDisplayText(string context, ModelDisplayText settings) : base(context, settings)
        {
            Settings = settings;
        }

        public override void Refresh(ImageManager imgManager)
        {
            if (ValueRef == null || ValueRef.Value == null)
            {
                SetError();
                return;
            }
            else
            {
                if (!ValueRef.IsChanged && !ForceUpdate)
                    return;

                string value = ValueRef.Value;
                if (Settings.DecodeBCD)
                    value = ConvertFromBCD(value);

                value = CommonSettings.ScaleValue(value);
                value = CommonSettings.RoundValue(value);

                //evaluate value and set indication
                string background = DefaultImage;
                string color = TextSettings.FontColor;
                string text = "";
                if (TextSettings.HasIndication && TextSettings.IndicationValue == value)
                {
                    background = TextSettings.IndicationImage;
                    if (!TextSettings.IndicationHideValue)
                    {
                        text = CommonSettings.FormatValue(value);
                        if (TextSettings.IndicationUseColor)
                            color = TextSettings.IndicationColor;
                    }
                }
                else
                    text = CommonSettings.FormatValue(value);

                if (text != lastText || ForceUpdate)
                {
                    ConvertFontParameter(out Font drawFont);
                    DrawImage = ImageTools.DrawText(text, imgManager.GetImageObject(background), drawFont, ColorTranslator.FromHtml(color), TextSettings.FontRect);
                    IsRawImage = true;
                    NeedRedraw = true;
                    lastText = text;
                }
            }
        }

        protected virtual void ConvertFontParameter(out Font drawFont)
        {
            drawFont = new Font(TextSettings.FontName, TextSettings.FontSize, (FontStyle)TextSettings.FontStyle); //GraphicsUnit.Point ?
        }

    }
}
