using System.Globalization;

namespace PilotsDeck
{
    public class HandlerSwitchKorry : HandlerSwitchDisplay
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchKorry KorrySettings { get { return Settings; } }
        public new ModelSwitchKorry Settings { get; protected set; }

        public override string ActionID { get { return $"(HandlerSwitchKorry) ({Title.Trim()}) {(BaseSettings.IsEncoder ? "(Encoder) " : "")}(Top: {KorrySettings.AddressTop} / Bot: {KorrySettings.AddressBot}) (Action: {(ActionSwitchType)BaseSettings.ActionType} / {Address}) (Long: {BaseSettings.HasLongPress} / {(ActionSwitchType)BaseSettings.ActionTypeLong} / {BaseSettings.AddressActionLong})"; } }
        public override string Address { get { return KorrySettings.AddressTop; } }


        public HandlerSwitchKorry(string context, ModelSwitchKorry settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            KorrySettings.Address = KorrySettings.AddressTop;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(KorrySettings.AddressTop) && KorrySettings.ShowTopImage && !string.IsNullOrEmpty(BaseSettings.AddressAction) && !string.IsNullOrEmpty(KorrySettings.AddressBot) && KorrySettings.ShowBotImage;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            if (  (KorrySettings.UseOnlyTopAddr && KorrySettings.ShowTopImage && KorrySettings.ShowBotImage)
               || (KorrySettings.UseOnlyTopAddr && !KorrySettings.ShowTopImage && !KorrySettings.ShowBotImage))
            {
                KorrySettings.UseOnlyTopAddr = false;
                KorrySettings.ShowBotImage = false;
                KorrySettings.ShowTopImage = true;
                UpdateSettingsModel = true;
            }

            BaseSettings.SwitchOnCurrentValue = false;
            base.Register(imgManager, ipcManager);

            imgManager.AddImage(KorrySettings.TopImage, DeckType);
            imgRefs.Add(ID.ImgTop, KorrySettings.TopImage);
            imgManager.AddImage(KorrySettings.BotImage, DeckType);
            imgRefs.Add(ID.ImgBot, KorrySettings.BotImage);

            if (CommonSettings.UseImageMapping)
                mapRefs.Add(ID.MapBot, new(KorrySettings.ImageMapBot, DeckType, ValueManager));

            RenderDefaultImages();

            ValueManager.AddValue(ID.Bottom, KorrySettings.AddressBot); 
        }

        public override void Deregister()
        {
            base.Deregister();

            ImgManager.RemoveImage(KorrySettings.TopImage, DeckType);
            imgRefs.Remove(ID.ImgTop);
            ImgManager.RemoveImage(KorrySettings.BotImage, DeckType);
            imgRefs.Remove(ID.ImgBot);

            if (mapRefs.TryGetValue(ID.MapBot, out ImageMapping map))
                map?.DeregisterMap();
            imgRefs.Remove(ID.MapBot);

            ValueManager.RemoveValue(ID.Bottom); 
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);
            RenderDefaultImages();

            ValueManager.UpdateValue(ID.Bottom, KorrySettings.AddressBot);
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            UpdateImage(KorrySettings.TopImage, ID.ImgTop, out _);
            UpdateImage(KorrySettings.BotImage, ID.ImgBot, out _);

            ImageMapping.RefUpdateHelper(mapRefs, ID.MapBot, CommonSettings.UseImageMapping, KorrySettings.ImageMapBot, DeckType, ValueManager);
        }

        public override void UpdateActionSettings()
        {
            KorrySettings.Address = KorrySettings.AddressTop;
        }

        protected override void RenderDefaultImages()
        {
            ImageRenderer render = new (ImgManager.GetImage(KorrySettings.DefaultImage, DeckType), DeckType);

            if (KorrySettings.ShowTopImage)
            {
                if (!string.IsNullOrEmpty(KorrySettings.TopImage) && !CommonSettings.UseImageMapping)
                    render.DrawImage(ImgManager.GetImage(KorrySettings.TopImage, DeckType).GetImageObject(), KorrySettings.GetRectangleTop());
                else if (CommonSettings.UseImageMapping)
                    render.DrawImage(mapRefs[ID.Map].GetMappedImage("0", KorrySettings.TopImage).GetImageObject(), KorrySettings.GetRectangleTop());
            }

            if (KorrySettings.ShowBotImage)
            {
                if (!string.IsNullOrEmpty(KorrySettings.BotImage) && !CommonSettings.UseImageMapping)
                    render.DrawImage(ImgManager.GetImage(KorrySettings.BotImage, DeckType).GetImageObject(), KorrySettings.GetRectangleBot());
                else if (CommonSettings.UseImageMapping)
                    render.DrawImage(mapRefs[ID.MapBot].GetMappedImage("0", KorrySettings.BotImage).GetImageObject(), KorrySettings.GetRectangleBot());
            }

            if (SwitchSettings.IsGuarded)
                RenderGuard(render, "0", "0");

            DefaultImage64 = render.RenderImage64();
            render.Dispose();
        }

        public override void Refresh()
        {
            if (!ValueManager.HasChangedValues() && !NeedRefresh)
                return;

            ImageRenderer render = new(ImgManager.GetImage(KorrySettings.DefaultImage, DeckType), DeckType);

            string top = ValueManager[ID.Top];
            string bot = ValueManager[ID.Bottom];

            if (KorrySettings.ShowTopImage)
            {
                if (!CommonSettings.UseImageMapping && !string.IsNullOrEmpty(KorrySettings.TopImage) && ((ModelBase.Compare(KorrySettings.TopState, top) && !KorrySettings.ShowTopNonZero) || (KorrySettings.ShowTopNonZero && ValueNonZero(top))))
                    render.DrawImage(ImgManager.GetImage(KorrySettings.TopImage, DeckType).GetImageObject(), KorrySettings.GetRectangleTop());
                else if (CommonSettings.UseImageMapping)
                {
                    string topImage = mapRefs[ID.MapTop].GetMappedImageString(top);
                    if (!string.IsNullOrEmpty(topImage))
                        render.DrawImage(ImgManager.GetImage(topImage, DeckType).GetImageObject(), KorrySettings.GetRectangleTop());
                }
            }

            if (KorrySettings.ShowBotImage)
            {
                if (!CommonSettings.UseImageMapping && !string.IsNullOrEmpty(KorrySettings.BotImage) && ((ModelBase.Compare(KorrySettings.BotState, bot) && !KorrySettings.ShowBotNonZero) || (KorrySettings.ShowBotNonZero && ValueNonZero(bot))))
                    render.DrawImage(ImgManager.GetImage(KorrySettings.BotImage, DeckType).GetImageObject(), KorrySettings.GetRectangleBot());
                else if (CommonSettings.UseImageMapping)
                {
                    string BotImage = mapRefs[ID.MapBot].GetMappedImageString(bot);
                    if (!string.IsNullOrEmpty(BotImage))
                        render.DrawImage(ImgManager.GetImage(BotImage, DeckType).GetImageObject(), KorrySettings.GetRectangleBot());
                }
            }

            if (SwitchSettings.IsGuarded)
                RenderGuard(render, SwitchSettings.GuardActiveValue, ValueManager[ID.Guard]);

            RenderImage64 = render.RenderImage64();
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
