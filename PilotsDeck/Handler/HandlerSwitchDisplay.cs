﻿using Serilog;

namespace PilotsDeck
{
    public class HandlerSwitchDisplay : HandlerSwitch, IHandlerValue
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public new ModelSwitchDisplay Settings { get; protected set; }
        

        public override string ActionID { get { return $"\"{Title}\" [HandlerSwitchDisplay] Write: {BaseSettings.AddressAction} | Read: {DisplaySettings.Address}"; } }
        public override string Address { get { return DisplaySettings.Address; } }

        public virtual string CurrentValue { get; protected set; } = null;
        public virtual string LastAddress { get; protected set; }
        public virtual bool IsChanged { get; protected set; } = false;
        protected override bool CanRedraw { get { return CurrentValue != null; } }


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
        }

        public override void Deregister(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Deregister(imgManager, ipcManager);

            imgManager.RemoveImage(DisplaySettings.OnImage, DeckType);
            imgManager.RemoveImage(DisplaySettings.OffImage, DeckType);
            if (DisplaySettings.HasIndication)
                imgManager.RemoveImage(DisplaySettings.IndicationImage, DeckType);
        }

        public virtual void RefreshValue(IPCManager ipcManager)
        {
            IsChanged = HandlerValue.RefreshValue(ipcManager, Address, out string currentValue);
            CurrentValue = currentValue;
        }

        public virtual void RegisterAddress(IPCManager ipcManager)
        {
            ipcManager.RegisterAddress(Address, AppSettings.groupStringRead);
            LastAddress = Address;
        }

        public virtual void UpdateAddress(IPCManager ipcManager)
        {
            LastAddress = HandlerValue.UpdateAddress(ipcManager, LastAddress, Address);
        }

        public virtual void DeregisterAddress(IPCManager ipcManager)
        {
            ipcManager.DeregisterValue(Address);
            if (Address != LastAddress)
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Address} != {LastAddress} ] ");
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(BaseSettings.AddressAction) && !string.IsNullOrEmpty(DisplaySettings.Address);
        }

        public override bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            LastSwitchState = CurrentValue;
            LastSwitchStateLong = CurrentValue;

            return base.OnButtonUp(ipcManager, tick);
        }

        protected override void Redraw(ImageManager imgManager)
        {
            if (!IsChanged && !ForceUpdate)
                return;
            string lastImage = DrawImage;

            if (Settings.OnState == CurrentValue || Settings.OffState == CurrentValue)
            {
                if (Settings.OnState == CurrentValue)
                    DrawImage = Settings.OnImage;
                else
                    DrawImage = Settings.OffImage;
            }
            else if (Settings.HasIndication)
            {
                if (Settings.IndicationValueAny || Settings.IndicationValue == CurrentValue)
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
