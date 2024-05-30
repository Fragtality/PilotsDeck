namespace PilotsDeck
{
    public class HandlerSwitchDisplay(string context, ModelSwitchDisplay settings, StreamDeckType deckType) : HandlerSwitch(context, settings, deckType)
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public new ModelSwitchDisplay Settings { get; protected set; } = settings;


        public override string ActionID { get { return $"(HandlerSwitchDisplay) ({Title.Trim()}) {(BaseSettings.IsEncoder ? "(Encoder) " : "")}(Read: {DisplaySettings.Address}) (Action: {(ActionSwitchType)BaseSettings.ActionType} / {Address}) (Long: {BaseSettings.HasLongPress} / {(ActionSwitchType)BaseSettings.ActionTypeLong} / {BaseSettings.AddressActionLong})"; } }
        public override string Address { get { return DisplaySettings.Address; } }

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
                    ModelSwitchDisplay.ManageImageMap(imgRefs[ID.Map], ImgManager, DeckType, false);
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
            ManagedImage newImage;
            if (!Settings.UseImageMapping)
                newImage = ImgManager.GetImage(DisplaySettings.OffImage, DeckType);
            else
            {
                string mappedImage = Settings.GetValueMapped("0");
                if (!string.IsNullOrEmpty(mappedImage))
                    newImage = ImgManager.GetImage(mappedImage, DeckType);
                else
                    newImage = ImgManager.GetImage(DisplaySettings.DefaultImage, DeckType);
            }

            DefaultImage64 = RefreshGuard(newImage, "0");
        }

        public override void Refresh()
        {
            if (!ValueManager.IsChanged(ID.Control) && !NeedRefresh && (!SwitchSettings.IsGuarded || !ValueManager.IsChanged(ID.Guard)))
                return;

            ManagedImage newImage;
            string currentValue = ValueManager[ID.Control];

            if (!Settings.UseImageMapping)
            {
                if (!string.IsNullOrWhiteSpace(currentValue) && ModelBase.Compare(Settings.OnState, currentValue))
                {
                    newImage = ImgManager.GetImage(Settings.OnImage, DeckType);
                }
                else if (ModelBase.Compare(Settings.OffState, currentValue))
                {
                    newImage = ImgManager.GetImage(Settings.OffImage, DeckType);
                }
                else if (Settings.HasIndication)
                {
                    if (Settings.IndicationValueAny || ModelBase.Compare(Settings.IndicationValue, currentValue))
                        newImage = ImgManager.GetImage(Settings.IndicationImage, DeckType);
                    else
                        newImage = ImgManager.GetImage(Settings.ErrorImage, DeckType);
                }
                else
                    newImage = ImgManager.GetImage(Settings.ErrorImage, DeckType); ;
            }
            else
            {
                string mappedImage = Settings.GetValueMapped(currentValue);
                if (!string.IsNullOrEmpty(mappedImage))
                    newImage = ImgManager.GetImage(mappedImage, DeckType);
                else
                    newImage = ImgManager.GetImage(Settings.ErrorImage, DeckType);
            }

            RenderImage64 = RefreshGuard(newImage, currentValue);
            NeedRedraw = true;
        }

        protected string RefreshGuard(ManagedImage image, string currentValue)
        {
            string result = image.GetImageBase64();

            if (SwitchSettings.IsGuarded && ModelBase.Compare(SwitchSettings.GuardActiveValue, ValueManager[ID.Guard]))
            {
                if (SwitchSettings.UseImageGuardMapping)
                {
                    string guardImage = ModelSwitchDisplay.GetValueMapped(currentValue, SwitchSettings.ImageGuardMap);
                    if (!string.IsNullOrEmpty(guardImage))
                    {
                        ImageRenderer render = new(image, DeckType);
                        render.DrawImage(ImgManager.GetImage(guardImage, DeckType).GetImageObject());
                        result = render.RenderImage64();
                        render.Dispose();
                    }
                    else
                        result = image.GetImageBase64();
                }
                else
                {
                    ImageRenderer render = new(image, DeckType);
                    render.DrawImage(ImgManager.GetImage(SwitchSettings.ImageGuard, DeckType).GetImageObject());
                    result = render.RenderImage64();
                    render.Dispose();
                }
            }

            return result;
        }
    }
}
