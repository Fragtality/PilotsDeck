using System;
using Serilog;

namespace PilotsDeck
{
    public class HandlerSwitchDisplay : HandlerSwitch, IHandlerValue
    {
        public override ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public new ModelSwitchDisplay Settings { get; protected set; }
        

        public override string ActionID { get { return $"{Title} | Write: {BaseSettings.AddressAction} | Read: {DisplaySettings.Address}"; } }
        public override string Address { get { return DisplaySettings.Address; } }

        //protected virtual IPCValue ValueRef { get; set; } = null;
        //protected override string currentValue { get { return ValueRef?.Value; } set { } }
        public virtual string CurrentValue { get; protected set; } = null;
        public virtual string LastAddress { get; protected set; }
        public virtual bool IsChanged { get; protected set; } = false;
        protected override bool CanRedraw { get { return CurrentValue != null; } }


        public HandlerSwitchDisplay(string context, ModelSwitchDisplay settings) : base(context, settings)
        {
            Settings = settings;
        }

        public virtual void RefreshValue(IPCManager ipcManager)
        {
            IsChanged = HandlerValue.RefreshValue(ipcManager, Address, out string currentValue);
            CurrentValue = currentValue;
        }

        public virtual void RegisterAddress(IPCManager ipcManager)
        {
            //ValueRef = RegisterValue(ipcManager, Context, Address);
            ipcManager.RegisterAddress(Address, AppSettings.groupStringRead);
            LastAddress = Address;
        }

        public virtual void UpdateAddress(IPCManager ipcManager)
        {
            //ValueRef = UpdateValue(ipcManager, Context, Address);
            LastAddress = HandlerValue.UpdateAddress(ipcManager, LastAddress, Address);
        }

        public virtual void DeregisterAddress(IPCManager ipcManager)
        {
            ipcManager.DeregisterValue(Address);
            if (Address != LastAddress)
                throw new Exception($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Address} != {LastAddress} ] ");
        }

        protected override bool CheckInitialization()
        {
            return !string.IsNullOrEmpty(BaseSettings.AddressAction) && !string.IsNullOrEmpty(DisplaySettings.Address);
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
