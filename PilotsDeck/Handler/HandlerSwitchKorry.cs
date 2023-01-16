﻿using System.Globalization;

namespace PilotsDeck
{
    public class HandlerSwitchKorry : HandlerSwitchDisplay
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchKorry KorrySettings { get { return Settings; } }
        public new ModelSwitchKorry Settings { get; protected set; }

        public virtual string DefaultImageRender { get; set; }

        public override string ActionID { get { return $"\"{StreamDeckTools.TitleLog(Title)}\" [HandlerSwitchKorry] ReadTop: {KorrySettings.AddressTop} | ReadBot: {KorrySettings.AddressBot} | Write: {BaseSettings.AddressAction}"; } }
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
            RenderDefaultImage();

            ValueManager.RegisterValue(ID.Bot, KorrySettings.AddressBot); 
        }

        public override void Deregister()
        {
            base.Deregister();

            ValueManager.DeregisterValue(ID.Bot); 
        }

        public override void Update()
        {
            base.Update();
            RenderDefaultImage();
            NeedRedraw = true;

            ValueManager.UpdateValueAddress(ID.Bot, KorrySettings.AddressBot);
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
            if (!ValueManager.IsChanged(ID.Top) && !ValueManager.IsChanged(ID.Bot)  && !ForceUpdate)
                return;

            ImageRenderer render = new(ImgManager.GetImageDefinition(DefaultImage, DeckType));

            string top = ValueManager[ID.Top];
            string bot = ValueManager[ID.Bot];

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
