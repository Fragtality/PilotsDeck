namespace PilotsDeck
{
    public class HandlerSwitchDisplay : HandlerSwitch
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public new ModelSwitchDisplay Settings { get; protected set; }
        

        public override string ActionID { get { return $"(HandlerSwitchDisplay) ({Title.Trim()}) {(BaseSettings.IsEncoder ? "(Encoder) " : "")}(Read: {DisplaySettings.Address}) (Action: {(ActionSwitchType)BaseSettings.ActionType} / {Address}) (Long: {BaseSettings.HasLongPress} / {(ActionSwitchType)BaseSettings.ActionTypeLong} / {BaseSettings.AddressActionLong})"; } }
        public override string Address { get { return DisplaySettings.Address; } }

        public HandlerSwitchDisplay(string context, ModelSwitchDisplay settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            if (this.GetType() == typeof(HandlerSwitchDisplay))
            {
                imgManager.AddImage(DisplaySettings.OnImage, DeckType);
                imgRefs.Add(ID.On, DisplaySettings.OnImage);
                imgManager.AddImage(DisplaySettings.OffImage, DeckType);
                imgRefs.Add(ID.Off, DisplaySettings.OffImage);
                if (DisplaySettings.HasIndication)
                    imgManager.AddImage(DisplaySettings.IndicationImage, DeckType);
                imgRefs.Add(ID.Indication, DisplaySettings.IndicationImage);

                DisplaySettings.ManageImageMap(imgManager, DeckType, true);
                imgRefs.Add(ID.Map, DisplaySettings.ImageMap);
            }

            ValueManager.AddValue(ID.Control, DisplaySettings.Address);
            if (BaseSettings.SwitchOnCurrentValue)
            {
                BaseSettings.SwitchOffState = DisplaySettings.OffState;
                BaseSettings.SwitchOnState = DisplaySettings.OnState;
            }

            RenderDefaultImages();
        }

        public override void Deregister()
        {
            base.Deregister();

            if (this.GetType() == typeof(HandlerSwitchDisplay))
            {
                ImgManager.RemoveImage(DisplaySettings.OnImage, DeckType);
                imgRefs.Remove(ID.On);
                ImgManager.RemoveImage(DisplaySettings.OffImage, DeckType);
                imgRefs.Remove(ID.Off);
                if (DisplaySettings.HasIndication)
                    ImgManager.RemoveImage(DisplaySettings.IndicationImage, DeckType);
                imgRefs.Remove(ID.Indication);

                DisplaySettings.ManageImageMap(ImgManager, DeckType, false);
                imgRefs.Remove(ID.Map);
            }

            ValueManager.RemoveValue(ID.Control);
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);
            RenderDefaultImages();
            ValueManager.UpdateValue(ID.Control, Address);
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            if (this.GetType() == typeof(HandlerSwitchDisplay))
            {
                UpdateImage(DisplaySettings.OnImage, ID.On, out _);
                UpdateImage(DisplaySettings.OffImage, ID.Off, out _);
                UpdateImage(DisplaySettings.IndicationImage, ID.Indication, out _);

                if (DisplaySettings.ImageMap != imgRefs[ID.Map])
                {
                    DisplaySettings.ManageImageMap(imgRefs[ID.Map], ImgManager, DeckType, false);
                    DisplaySettings.ManageImageMap(ImgManager, DeckType, true);
                    imgRefs[ID.Map] = DisplaySettings.ImageMap;
                }
            }
        }

        public override void UpdateActionSettings()
        {
            if (BaseSettings.SwitchOnCurrentValue)
            {
                BaseSettings.SwitchOnState = DisplaySettings.OnState;
                BaseSettings.SwitchOffState = DisplaySettings.OffState;
            }
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(BaseSettings.AddressAction) && !string.IsNullOrEmpty(DisplaySettings.Address);
        }

        protected override void RenderDefaultImages()
        {
            if (!Settings.UseImageMapping)
                DefaultImage64 = ImgManager.GetImage(DisplaySettings.OffImage, DeckType).GetImageBase64();
            else
            {
                string mappedImage = Settings.GetValueMapped("0");
                if (!string.IsNullOrEmpty(mappedImage))
                    DefaultImage64 = ImgManager.GetImage(mappedImage, DeckType).GetImageBase64();
                else
                    DefaultImage64 = ImgManager.GetImage(DisplaySettings.DefaultImage, DeckType).GetImageBase64();
            }
        }

        public override void Refresh()
        {
            if (!ValueManager.IsChanged(ID.Control) && !NeedRefresh)
                return;

            string newImage;
            string currentValue = ValueManager[ID.Control];

            if (!Settings.UseImageMapping)
            {
                if (!string.IsNullOrWhiteSpace(currentValue) && ModelBase.Compare(Settings.OnState, currentValue))
                {
                    newImage = ImgManager.GetImage(Settings.OnImage, DeckType).GetImageBase64();
                }
                else if (ModelBase.Compare(Settings.OffState, currentValue))
                {
                    newImage = ImgManager.GetImage(Settings.OffImage, DeckType).GetImageBase64();
                }
                else if (Settings.HasIndication)
                {
                    if (Settings.IndicationValueAny || ModelBase.Compare(Settings.IndicationValue, currentValue))
                        newImage = ImgManager.GetImage(Settings.IndicationImage, DeckType).GetImageBase64();
                    else
                        newImage = ErrorImage64;
                }
                else
                    newImage = ErrorImage64;
            }
            else
            {
                string mappedImage = Settings.GetValueMapped(currentValue);
                if (!string.IsNullOrEmpty(mappedImage))
                    newImage = ImgManager.GetImage(mappedImage, DeckType).GetImageBase64();
                else
                    newImage = ErrorImage64;
            }

            RenderImage64 = newImage;
            NeedRedraw = true;
        }

    }
}
