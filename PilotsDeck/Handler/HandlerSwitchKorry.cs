namespace PilotsDeck
{
    public class HandlerSwitchKorry : HandlerSwitch, IHandlerValue
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchKorry KorrySettings { get { return Settings; } }
        public new ModelSwitchKorry Settings { get; protected set; }

        public virtual string DefaultImageRender { get; set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerSwitchKorry] ReadTop: {KorrySettings.AddressTop} | ReadBot: {KorrySettings.AddressBot} | Write: {BaseSettings.AddressAction}"; } }

        public virtual string CurrentValue { get { return CurrentValues[0]; } }
        protected string[] CurrentValues { get; set; } = new string[2];
        protected virtual string[] CurrentAddress { get; set; } = new string[2];
        public string LastAddress { get { return CurrentAddress[0]; } }

        public virtual bool IsChanged { get; protected set; } = false;

        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValues[0]) && (!string.IsNullOrEmpty(CurrentValues[1]) || KorrySettings.UseOnlyTopAddr); } }


        public HandlerSwitchKorry(string context, ModelSwitchKorry settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            LastSwitchState = settings.OffState;
            LastSwitchStateLong = settings.OffStateLong;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(KorrySettings.AddressTop) && !string.IsNullOrEmpty(BaseSettings.AddressAction) && (!string.IsNullOrEmpty(KorrySettings.AddressBot) || KorrySettings.UseOnlyTopAddr);
        }

        public virtual void RegisterAddress(IPCManager ipcManager)
        {
            CurrentAddress = HandlerDisplayRadio.RegisterAddress(ipcManager, KorrySettings.AddressTop, KorrySettings.AddressBot, CurrentAddress);
        }

        public virtual void UpdateAddress(IPCManager ipcManager)
        {
            CurrentAddress = HandlerDisplayRadio.UpdateAddress(ipcManager, KorrySettings.AddressTop, KorrySettings.AddressBot, CurrentAddress);
        }

        public virtual void DeregisterAddress(IPCManager ipcManager)
        {
            HandlerDisplayRadio.DeregisterAddress(ipcManager, KorrySettings.AddressTop, KorrySettings.AddressBot, CurrentAddress, ActionID);
        }

        public virtual void RefreshValue(IPCManager ipcManager)
        {
            CurrentValues = HandlerDisplayRadio.RefreshValue(ipcManager, KorrySettings.AddressTop, KorrySettings.AddressBot, CurrentValues, out bool isChanged);
            IsChanged = isChanged;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);
            RenderDefaultImage(imgManager);
        }

        public override void Update(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Update(imgManager, ipcManager);
            RenderDefaultImage(imgManager);
            NeedRedraw = true;
        }

        protected virtual void RenderDefaultImage(ImageManager imgManager)
        {
            ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(KorrySettings.DefaultImage, DeckType));

            if (!string.IsNullOrEmpty(KorrySettings.TopImage))
                render.DrawImage(imgManager.GetImageObject(KorrySettings.TopImage, DeckType), KorrySettings.GetRectangleTop());
            if (!string.IsNullOrEmpty(KorrySettings.BotImage))
                render.DrawImage(imgManager.GetImageObject(KorrySettings.BotImage, DeckType), KorrySettings.GetRectangleBot());

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

        protected override void Redraw(ImageManager imgManager)
        {
            if (!IsChanged && !ForceUpdate)
                return;

            ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(DefaultImage, DeckType));

            if (((CurrentValues[0] == KorrySettings.TopState && !KorrySettings.ShowTopNonZero) || (KorrySettings.ShowTopNonZero && ValueNonZero(CurrentValues[0]))) && !string.IsNullOrEmpty(KorrySettings.TopImage))
                render.DrawImage(imgManager.GetImageObject(KorrySettings.TopImage, DeckType), KorrySettings.GetRectangleTop());

            string testValue = CurrentValues[1];
            if (KorrySettings.UseOnlyTopAddr)
                testValue = CurrentValues[0];

            if (((testValue == KorrySettings.BotState && !KorrySettings.ShowBotNonZero) || (KorrySettings.ShowBotNonZero && ValueNonZero(testValue))) && !string.IsNullOrEmpty(KorrySettings.BotImage))
                render.DrawImage(imgManager.GetImageObject(KorrySettings.BotImage, DeckType), KorrySettings.GetRectangleBot());

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
                if (float.TryParse(value, out float num))
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
