using System.Drawing;

namespace PilotsDeck
{
    public class HandlerDisplayText(string context, ModelDisplayText settings, StreamDeckType deckType) : HandlerBase(context, settings, deckType)
    {
        public virtual ModelDisplayText TextSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings => throw new System.NotImplementedException();
        public virtual ModelDisplayText Settings { get; protected set; } = settings;

        public override string ActionID { get { return $"(HandlerDisplayText) ({Title.Trim()}) {(TextSettings.IsEncoder ? "(Encoder) " : "")}(Read: {TextSettings.Address})"; } }
        public override string Address { get { return TextSettings.Address; } }

        public override bool UseFont { get { return true; } }

        protected bool DrawBoxLast = settings.DrawBox;

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
                imgManager.AddImage(TextSettings.IndicationImage, DeckType);
            imgRefs.Add(ID.Indication, TextSettings.IndicationImage);

            RenderDefaultImages();

            ValueManager.AddValue(ID.Control, Address);
        }

        public override void Deregister()
        {
            base.Deregister();

            if (TextSettings.HasIndication)
                ImgManager.RemoveImage(TextSettings.IndicationImage, DeckType);
            imgRefs.Remove(ID.Indication);

            ValueManager.RemoveValue(ID.Control);
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);
            RenderDefaultImages();

            if (DrawBoxLast != TextSettings.DrawBox)
            {
                TextSettings.ResetRectText();
                DrawBoxLast = TextSettings.DrawBox;
                UpdateSettingsModel = true;
            }

            ValueManager.UpdateValue(ID.Control, Address);
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            UpdateImage(Settings.IndicationImage, ID.Indication, out _);
        }

        protected override void RenderDefaultImages()
        {
            //Default
            ImageRenderer render = new(ImgManager.GetImage(TextSettings.DefaultImage, DeckType), DeckType);
            if (TextSettings.DrawBox)
                render.DrawBox(ColorTranslator.FromHtml(TextSettings.BoxColor), ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());
            
            TextSettings.GetFontParameters(TitleParameters, out Font drawFont, out Color drawColor);
            string text = TextSettings.GetValueMapped("0");
            render.DrawText(text, drawFont, drawColor, TextSettings.GetRectangleText());

            if (HasAction && SwitchSettings.IsGuarded)
                render.DrawImage(ImgManager.GetImage(SwitchSettings.ImageGuard, DeckType).GetImageObject());

            if (IsEncoder)
                DrawTitle(render);

            DefaultImage64 = render.RenderImage64();
            render.Dispose();

            //Error
            render = new(ImgManager.GetImage(TextSettings.ErrorImage, DeckType), DeckType);
            if (TextSettings.DrawBox)
                render.DrawBox(ColorTranslator.FromHtml("#d70000"), ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());

            if (HasAction && SwitchSettings.IsGuarded)
                render.DrawImage(ImgManager.GetImage(SwitchSettings.ImageGuard, DeckType).GetImageObject());

            if (IsEncoder)
                DrawTitle(render);

            ErrorImage64 = render.RenderImage64();
            render.Dispose();

            //Wait
            render = new(ImgManager.GetImage(TextSettings.WaitImage, DeckType), DeckType);
            if (TextSettings.DrawBox)
                render.DrawBox(ColorTranslator.FromHtml(TextSettings.BoxColor), ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());

            if (IsEncoder)
                DrawTitle(render);

            WaitImage64 = render.RenderImage64();
            render.Dispose();

            NeedRedraw = true;
            NeedRefresh = true;
        }

        public override void RefreshTitle()
        {
            if (IsEncoder)
            {
                NeedRefresh = true;
                NeedRedraw = true;
                RenderDefaultImages();
            }
        }

        public override void Refresh()
        {
            if (!ValueManager.HasChangedValues() && !NeedRefresh)
                return;

            string value = ValueManager[ID.Control];
            if (Settings.DecodeBCD)
                value = ModelDisplay.ConvertFromBCD(value);

            value = TextSettings.ScaleValue(value);
            value = TextSettings.RoundValue(value);

            //evaluate value and set indication
            string background = TextSettings.DefaultImage;
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


            ImageRenderer render = new(ImgManager.GetImage(background, DeckType), DeckType);

            if (TextSettings.DrawBox)
                render.DrawBox(boxColor, ModelDisplayText.GetNumValue(TextSettings.BoxSize, 2), TextSettings.GetRectangleBox());

            if (text != "")
                render.DrawText(text, drawFont, drawColor, TextSettings.GetRectangleText());

            if (HasAction && SwitchSettings.IsGuarded && ModelBase.Compare(SwitchSettings.GuardActiveValue, ValueManager[ID.Guard]))
                render.DrawImage(ImgManager.GetImage(SwitchSettings.ImageGuard, DeckType).GetImageObject());

            if (IsEncoder)
                DrawTitle(render);

            RenderImage64 = render.RenderImage64();
            NeedRedraw = true;
            render.Dispose();
        }
    }
}
