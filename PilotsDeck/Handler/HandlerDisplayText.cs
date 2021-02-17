using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayText : HandlerValue
    {
        public virtual ModelDisplayText TextSettings { get { return Settings; } }
        public ModelDisplayText Settings { get; protected set; }

        public override string Address { get { return TextSettings.Address; } }
        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayText] Read: {TextSettings.Address}"; } }

        public override bool UseFont { get { return true; } }
        public virtual string DefaultImageRender { get; set; }
        public virtual string ErrorImageRender { get; set; }

        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue); } }
        protected string lastText = "";
        protected bool DrawBox = true;

        public HandlerDisplayText(string context, ModelDisplayText settings) : base(context, settings)
        {
            Settings = settings;
            DrawBox = Settings.DrawBox;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            if (TextSettings.HasIndication)
                imgManager.AddImage(TextSettings.IndicationImage);

            if (TextSettings.DrawBox)
                RenderImages(imgManager);
        }

        public override void Deregister(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Deregister(imgManager, ipcManager);

            if (TextSettings.HasIndication)
                imgManager.RemoveImage(TextSettings.IndicationImage);
        }

        public override void Update(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Update(imgManager, ipcManager);
            RenderImages(imgManager);
            NeedRedraw = true;

            if (DrawBox != TextSettings.DrawBox)
            {
                TextSettings.ResetRectText();
                DrawBox = TextSettings.DrawBox;
                UpdateSettingsModel = true;
            }
        }

        protected virtual void RenderImages(ImageManager imgManager)
        {
            if (TextSettings.DrawBox)
            {
                ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(TextSettings.DefaultImage));
                render.DrawBox(ColorTranslator.FromHtml(TextSettings.BoxColor), TextSettings.BoxSize, TextSettings.GetRectangleBox());
                DefaultImageRender = render.RenderImage64();
                render.Dispose();

                render = new ImageRenderer(imgManager.GetImageObject(TextSettings.ErrorImage));
                render.DrawBox(ColorTranslator.FromHtml("#d70000"), TextSettings.BoxSize, TextSettings.GetRectangleBox());
                ErrorImageRender = render.RenderImage64();
                render.Dispose();
            }
        }

        public override void SetDefault()
        {
            if (TextSettings.DrawBox && DrawImage != DefaultImageRender)
            {
                DrawImage = DefaultImageRender;
                IsRawImage = true;
                NeedRedraw = true;
            }
            else if (!TextSettings.DrawBox && DrawImage != DefaultImage)
            {
                DrawImage = DefaultImage;
                IsRawImage = false;
                NeedRedraw = true;
            }
        }

        public override void SetError()
        {
            if (IsInitialized)
            {
                if (TextSettings.DrawBox && DrawImage != ErrorImageRender)
                {
                    DrawImage = ErrorImageRender;
                    IsRawImage = true;
                    NeedRedraw = true;
                }
                else if (!TextSettings.DrawBox && DrawImage != ErrorImage)
                {
                    DrawImage = ErrorImage;
                    IsRawImage = false;
                    NeedRedraw = true;
                }
            }
            else
            {
                SetDefault();
            }
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
            Color boxColor = ColorTranslator.FromHtml(TextSettings.BoxColor);

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

                if (TextSettings.IndicationUseColor)
                        boxColor = ColorTranslator.FromHtml(TextSettings.IndicationColor);
            }
            else
                text = TextSettings.FormatValue(value);

            if (text != lastText || ForceUpdate)
            {
                ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(background));
                if (TextSettings.DrawBox)
                    render.DrawBox(boxColor, TextSettings.BoxSize, TextSettings.GetRectangleBox());

                if (text != "")
                    render.DrawText(text, drawFont, drawColor, TextSettings.GetRectangleText());
                
                DrawImage = render.RenderImage64();
                IsRawImage = true;
                NeedRedraw = true;
                lastText = text;
                render.Dispose();
            }
        }
    }
}
