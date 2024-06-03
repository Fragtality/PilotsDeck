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
                newImage = mapRefs[ID.Map].GetMappedImage("0", DisplaySettings.DefaultImage);

            DefaultImage64 = GetGuardedImage64(newImage, "0", "0");
        }

        public override void Refresh()
        {
            if (!ValueManager.HasChangedValues() && !NeedRefresh)
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
                    newImage = ImgManager.GetImage(Settings.ErrorImage, DeckType);
            }
            else
            {
                newImage = mapRefs[ID.Map].GetMappedImage(currentValue, DisplaySettings.ErrorImage);
            }

            RenderImage64 = GetGuardedImage64(newImage, SwitchSettings.GuardActiveValue, currentValue);
            NeedRedraw = true;
        }
    }
}
