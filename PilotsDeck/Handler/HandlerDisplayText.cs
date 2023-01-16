using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayText : HandlerBase
    {
        public virtual ModelDisplayText TextSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings => throw new System.NotImplementedException();
        public virtual ModelDisplayText Settings { get; protected set; }

        public override string ActionID { get { return $"\"{StreamDeckTools.TitleLog(Title)}\" [HandlerDisplayText] Read: {TextSettings.Address}"; } }
        public override string Address { get { return TextSettings.Address; } }

        public override bool UseFont { get { return true; } }
        public virtual string DefaultImageRender { get; set; }
        public virtual string ErrorImageRender { get; set; }

        protected string lastText = "";
        protected bool DrawBox = true;

        public HandlerDisplayText(string context, ModelDisplayText settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            DrawBox = settings.DrawBox;
        }

        public override bool OnButtonDown(long tick)
        {
            return false;
        }

        public override bool OnButtonUp(long tick)
        {
            return true;
        }

        public override bool OnDialRotate(int ticks)
        {
            return true;
        }

        public override bool OnTouchTap()
        {
            return true;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            if (TextSettings.HasIndication)
                _ = imgManager.AddImage(TextSettings.IndicationImage, DeckType);

            RenderImages();
            NeedRedraw = true;

            ValueManager.RegisterValue(ID.ControlState, Address);
        }

        public override void Deregister()
        {
            base.Deregister();

            if (TextSettings.HasIndication)
                ImgManager.RemoveImage(TextSettings.IndicationImage, DeckType);

            ValueManager.DeregisterValue(ID.ControlState);
        }

        public override void Update()
        {
            base.Update();
            RenderImages();
            NeedRedraw = true;

            if (DrawBox != TextSettings.DrawBox)
            {
                TextSettings.ResetRectText();
                DrawBox = TextSettings.DrawBox;
                UpdateSettingsModel = true;
            }

            ValueManager.UpdateValueAddress(ID.ControlState, Address);
        }

        protected virtual void RenderImages()
        {
            if (TextSettings.DrawBox)
            {
                ImageRenderer render = new(ImgManager.GetImageDefinition(TextSettings.DefaultImage, DeckType));
                render.DrawBox(ColorTranslator.FromHtml(TextSettings.BoxColor), ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());
                if (IsEncoder)
                    DrawTitle(render);
                DefaultImageRender = render.RenderImage64();
                render.Dispose();

                render = new(ImgManager.GetImageDefinition(TextSettings.ErrorImage, DeckType));
                render.DrawBox(ColorTranslator.FromHtml("#d70000"), ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());
                if (IsEncoder)
                    DrawTitle(render);
                ErrorImageRender = render.RenderImage64();
                render.Dispose();
                IsRawImage = true;
            }
            else if (IsEncoder)
            {
                ImageRenderer render = new(ImgManager.GetImageDefinition(TextSettings.DefaultImage, DeckType));
                DrawTitle(render);
                DefaultImageRender = render.RenderImage64();
                render.Dispose();
                IsRawImage = true;
            }
        }

        public override void RefreshTitle()
        {
            if (!IsInitialized && DrawImage != DefaultImageRender)
            {
                DrawImage = DefaultImageRender;
                IsRawImage = true;
                NeedRedraw = true;
            }
        }

        public override void SetDefault()
        {
            if ((TextSettings.DrawBox || IsEncoder) && DrawImage != DefaultImageRender)
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

        protected override void Redraw()
        {
            if (!ValueManager.IsChanged(ID.ControlState) && !ForceUpdate)
                return;

            string value = ValueManager[ID.ControlState];
            if (Settings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);

            value = TextSettings.ScaleValue(value);
            value = TextSettings.RoundValue(value);

            //evaluate value and set indication
            string background = DefaultImage;
            TextSettings.GetFontParameters(TitleParameters, out Font drawFont, out Color drawColor);
            Color boxColor = ColorTranslator.FromHtml(TextSettings.BoxColor);

            string text = "";
            if (TextSettings.HasIndication && ModelBase.Compare(TextSettings.IndicationValue, value)) 
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

            text = TextSettings.GetValueMapped(text);


            ImageRenderer render = new(ImgManager.GetImageDefinition(background, DeckType));

            if (TextSettings.DrawBox)
                render.DrawBox(boxColor, ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());

            if (text != "")
                render.DrawText(text, drawFont, drawColor, TextSettings.GetRectangleText());

            if (IsEncoder)
                DrawTitle(render);

            DrawImage = render.RenderImage64();
            IsRawImage = true;
            NeedRedraw = true;
            lastText = text;
            render.Dispose();
        }
    }
}
