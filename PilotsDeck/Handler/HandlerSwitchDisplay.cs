namespace PilotsDeck
{
    public class HandlerSwitchDisplay : HandlerSwitch
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public new ModelSwitchDisplay Settings { get; protected set; }
        

        public override string ActionID { get { return $"\"{StreamDeckTools.TitleLog(Title)}\" [HandlerSwitchDisplay] Write: {BaseSettings.AddressAction} | Read: {DisplaySettings.Address}"; } }
        public override string Address { get { return DisplaySettings.Address; } }

        public HandlerSwitchDisplay(string context, ModelSwitchDisplay settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        public override void Register(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Register(imgManager, ipcManager);

            imgManager.AddImage(DisplaySettings.OnImage, DeckType);
            imgManager.AddImage(DisplaySettings.OffImage, DeckType);
            if (DisplaySettings.HasIndication)
                imgManager.AddImage(DisplaySettings.IndicationImage, DeckType);
            DisplaySettings.ManageImageMap(imgManager, DeckType, true);

            ValueManager.RegisterValue(ID.ControlState, DisplaySettings.Address);
            if (BaseSettings.SwitchOnCurrentValue)
            {
                BaseSettings.SwitchOffState = DisplaySettings.OffState;
                BaseSettings.SwitchOnState = DisplaySettings.OnState;
            }
        }

        public override void Deregister()
        {
            base.Deregister();

            ImgManager.RemoveImage(DisplaySettings.OnImage, DeckType);
            ImgManager.RemoveImage(DisplaySettings.OffImage, DeckType);
            if (DisplaySettings.HasIndication)
                ImgManager.RemoveImage(DisplaySettings.IndicationImage, DeckType);
            DisplaySettings.ManageImageMap(ImgManager, DeckType, false);

            ValueManager.DeregisterValue(ID.ControlState);
        }

        public override void Update()
        {
            base.Update();

            ValueManager.UpdateValueAddress(ID.ControlState, Address);
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

        protected override void Redraw()
        {
            if (!ValueManager.IsChanged(ID.ControlState) && !ForceUpdate)
                return;

            string lastImage = DrawImage;
            string currentValue = ValueManager[ID.ControlState];

            if (!Settings.UseImageMapping)
            {
                if (ModelBase.Compare(Settings.OnState, currentValue))
                {
                    DrawImage = Settings.OnImage;
                }
                else if (ModelBase.Compare(Settings.OffState, currentValue))
                {
                    DrawImage = Settings.OffImage;
                }
                else if (Settings.HasIndication)
                {
                    if (Settings.IndicationValueAny || ModelBase.Compare(Settings.IndicationValue, currentValue))
                        DrawImage = Settings.IndicationImage;
                    else
                        DrawImage = Settings.ErrorImage;
                }
                else
                    DrawImage = Settings.ErrorImage;
            }
            else
            {
                DrawImage = Settings.GetValueMapped(currentValue);
                if (string.IsNullOrEmpty(DrawImage))
                    DrawImage = Settings.ErrorImage;
            }

            if (lastImage != DrawImage || ForceUpdate)
                NeedRedraw = true;
        }

    }
}
