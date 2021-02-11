using System;
using System.Drawing;
using Serilog;

namespace PilotsDeck
{
    public class HandlerDisplayRadio : HandlerDisplaySwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override ModelDisplaySwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayRadio Settings { get; protected set; }

        public override string ActionID { get { return $"{Title} | Read1: {Settings.AddressRadio0} | Read2: {Settings.AddressRadio1} | Write: {SwitchSettings.AddressAction}"; } }

        protected new string[] CurrentValue { get; set; } = new string[2];
        protected virtual string[] CurrentAddress { get; set; } = new string[2];
        protected override bool CanRedraw { get { return CurrentValue[0] != null && CurrentValue[1] != null; } }

        public HandlerDisplayRadio(string context, ModelDisplayRadio settings) : base(context, settings)
        {
            Settings = settings;
        }

        protected override bool CheckInitialization()
        {
            return !string.IsNullOrEmpty(SwitchSettings.AddressAction) && (!string.IsNullOrEmpty(Settings.AddressRadio0) || !string.IsNullOrEmpty(Settings.AddressRadio1));
        }

        public override void RegisterAddress(IPCManager ipcManager)
        {
            ipcManager.RegisterAddress(Settings.AddressRadio0, AppSettings.groupStringRead);
            ipcManager.RegisterAddress(Settings.AddressRadio1, AppSettings.groupStringRead);
            CurrentAddress[0] = Settings.AddressRadio0;
            CurrentAddress[1] = Settings.AddressRadio1;
        }

        public override void UpdateAddress(IPCManager ipcManager)
        {
            CurrentAddress[0] = UpdateAddress(ipcManager, CurrentAddress[0], Settings.AddressRadio0);
            CurrentAddress[1] = UpdateAddress(ipcManager, CurrentAddress[1], Settings.AddressRadio1);
        }

        public override void DeregisterAddress(IPCManager ipcManager)
        {
            ipcManager.DeregisterValue(Settings.AddressRadio0);
            ipcManager.DeregisterValue(Settings.AddressRadio1);

            if (Settings.AddressRadio0 != CurrentAddress[0])
                throw new Exception($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Settings.AddressRadio0} != {CurrentAddress[0]} ] ");
            if (Settings.AddressRadio0 != CurrentAddress[1])
                throw new Exception($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Settings.AddressRadio1} != {CurrentAddress[1]} ] ");
        }

        protected override void Redraw(ImageManager imgManager)
        {
            // T O D O
        } 
    }
}
