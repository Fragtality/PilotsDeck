using Serilog;

namespace PilotsDeck
{
    public class HandlerSwitchDisplay : HandlerSwitch
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public new ModelSwitchDisplay Settings { get; protected set; }
        

        public override string ActionID { get { return $"\"{Title}\" [HandlerSwitchDisplay] Write: {BaseSettings.AddressAction} | Read: {DisplaySettings.Address}"; } }
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

            ValueManager.RegisterValue(ID.ControlState, DisplaySettings.Address);
            if (BaseSettings.SwitchOnCurrentValue)
            {
                BaseSettings.SwitchOffState = DisplaySettings.OffState;
                BaseSettings.SwitchOnState = DisplaySettings.OnState;
            }
        }

        public override void Deregister(ImageManager imgManager)
        {
            base.Deregister(imgManager);

            imgManager.RemoveImage(DisplaySettings.OnImage, DeckType);
            imgManager.RemoveImage(DisplaySettings.OffImage, DeckType);
            if (DisplaySettings.HasIndication)
                imgManager.RemoveImage(DisplaySettings.IndicationImage, DeckType);

            ValueManager.DeregisterValue(ID.ControlState);
        }

        public override void Update(ImageManager imgManager)
        {
            base.Update(imgManager);

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

        protected override void Redraw(ImageManager imgManager)
        {
            if (!ValueManager.IsChanged(ID.ControlState) && !ForceUpdate)
                return;

            string lastImage = DrawImage;
            string currentValue = ValueManager[ID.ControlState];

            if (Settings.OnState == currentValue || Settings.OffState == currentValue)
            {
                if (Settings.OnState == currentValue)
                    DrawImage = Settings.OnImage;
                else
                    DrawImage = Settings.OffImage;
            }
            else if (Settings.HasIndication)
            {
                if (Settings.IndicationValueAny || Settings.IndicationValue == currentValue)
                    DrawImage = Settings.IndicationImage;
                else
                    DrawImage = Settings.ErrorImage;
            }
            else
                DrawImage = Settings.ErrorImage;

            if (lastImage != DrawImage || ForceUpdate)
                NeedRedraw = true;
        }

    }
}
