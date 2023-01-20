using System.Globalization;

namespace PilotsDeck
{
    public class HandlerSwitchKorry : HandlerSwitchDisplay
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchKorry KorrySettings { get { return Settings; } }
        public new ModelSwitchKorry Settings { get; protected set; }

        public virtual string DefaultImageRender { get; set; }

        public override string ActionID { get { return $"(HandlerSwitchKorry) ({Title.Trim()}) {(BaseSettings.IsEncoder ? "(Encoder) " : "")}(Top: {KorrySettings.AddressTop} / Bot: {KorrySettings.AddressBot}) (Action: {(ActionSwitchType)BaseSettings.ActionType} / {Address}) (Long: {BaseSettings.HasLongPress} / {(ActionSwitchType)BaseSettings.ActionTypeLong} / {BaseSettings.AddressActionLong})"; } }
        public override string Address { get { return KorrySettings.AddressTop; } }


        public HandlerSwitchKorry(string context, ModelSwitchKorry settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            KorrySettings.Address = KorrySettings.AddressTop;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(KorrySettings.AddressTop) && !string.IsNullOrEmpty(BaseSettings.AddressAction) && (!string.IsNullOrEmpty(KorrySettings.AddressBot) || KorrySettings.UseOnlyTopAddr);
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            BaseSettings.SwitchOnCurrentValue = false;
            base.Register(imgManager, ipcManager);

            imgManager.AddImage(KorrySettings.TopImage, DeckType);
            imgRefs.Add(ID.Top, KorrySettings.TopImage);
            imgManager.AddImage(KorrySettings.BotImage, DeckType);
            imgRefs.Add(ID.Bottom, KorrySettings.BotImage);

            RenderDefaultImage();

            ValueManager.AddValue(ID.Bottom, KorrySettings.AddressBot); 
        }

        public override void Deregister()
        {
            base.Deregister();

            ImgManager.RemoveImage(KorrySettings.TopImage, DeckType);
            imgRefs.Remove(ID.Top);
            ImgManager.RemoveImage(KorrySettings.BotImage, DeckType);
            imgRefs.Remove(ID.Bottom);

            ValueManager.RemoveValue(ID.Bottom); 
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);
            RenderDefaultImage();
            NeedRedraw = true;

            ValueManager.UpdateValue(ID.Bottom, KorrySettings.AddressBot);
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            UpdateImage(KorrySettings.TopImage, ID.ImgTop);
            UpdateImage(KorrySettings.BotImage, ID.ImgBot);
        }

        public override void UpdateActionSettings()
        {
            KorrySettings.Address = KorrySettings.AddressTop;
        }

        protected virtual void RenderDefaultImage()
        {
            ImageRenderer render = new (ImgManager.GetImageDefinition(KorrySettings.DefaultImage, DeckType));

            if (!string.IsNullOrEmpty(KorrySettings.TopImage))
                render.DrawImage(ImgManager.GetImageDefinition(KorrySettings.TopImage, DeckType).GetImageObject(), KorrySettings.GetRectangleTop());
            if (!string.IsNullOrEmpty(KorrySettings.BotImage))
                render.DrawImage(ImgManager.GetImageDefinition(KorrySettings.BotImage, DeckType).GetImageObject(), KorrySettings.GetRectangleBot());

            DefaultImageRender = render.RenderImage64();
            render.Dispose();
        }

        public override void SetDefault()
        {
            if (DrawImage != DefaultImageRender)
            {
                DrawImage = DefaultImageRender;
                IsRawImage = true;
                NeedRedraw = true;
            }
        }

        protected override void Redraw()
        {
            if (!ValueManager.IsChanged(ID.Top) && !ValueManager.IsChanged(ID.Bottom)  && !ForceUpdate)
                return;

            ImageRenderer render = new(ImgManager.GetImageDefinition(DefaultImage, DeckType));

            string top = ValueManager[ID.Top];
            string bot = ValueManager[ID.Bottom];

            if (((ModelBase.Compare(KorrySettings.TopState, top) && !KorrySettings.ShowTopNonZero) || (KorrySettings.ShowTopNonZero && ValueNonZero(top))) && !string.IsNullOrEmpty(KorrySettings.TopImage))
                render.DrawImage(ImgManager.GetImageDefinition(KorrySettings.TopImage, DeckType).GetImageObject(), KorrySettings.GetRectangleTop());

            string testValue = bot;
            if (KorrySettings.UseOnlyTopAddr)
                testValue = top;

            if (((ModelBase.Compare(KorrySettings.BotState, testValue) && !KorrySettings.ShowBotNonZero) || (KorrySettings.ShowBotNonZero && ValueNonZero(testValue))) && !string.IsNullOrEmpty(KorrySettings.BotImage))
                render.DrawImage(ImgManager.GetImageDefinition(KorrySettings.BotImage, DeckType).GetImageObject(), KorrySettings.GetRectangleBot());

            DrawImage = render.RenderImage64();
            IsRawImage = true;
            NeedRedraw = true;
            render.Dispose();
        }

        public static bool ValueNonZero(string value)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(value))
            {
                if (float.TryParse(value, NumberStyles.Number, new RealInvariantFormat(value), out float num))
                {
                    if (num != 0.0f)
                        result = true;
                }
                else
                    result = true; //non empty is counted as non-zero
            }

            return result;
        }
    }
}
