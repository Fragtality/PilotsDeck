using Serilog;

namespace PilotsDeck
{
    public class HandlerSwitchDisplay : HandlerSwitch, IHandlerValue
    {
        public virtual ModelSwitchDisplay DisplaySettings { get { return Settings; } }
        public ModelSwitchDisplay Settings { get; protected set; }

        public override string ActionID { get { return $"{Title} | Write: {CommonSettings.AddressAction} | Read: {DisplaySettings.Address}"; } }
        public virtual string Address { get { return DisplaySettings.Address; } }

        protected virtual IPCValue ValueRef { get; set; } = null;
        protected override string currentValue { get { return ValueRef?.Value; } set { } }

        public HandlerSwitchDisplay(string context, ModelSwitchDisplay settings) : base(context, settings)
        {
            Settings = settings;
        }

        public virtual void RegisterValue(IPCManager ipcManager)
        {
            ValueRef = HandlerDisplay.RegisterValue(ipcManager, Context, Address);
        }

        public virtual void UpdateValue(IPCManager ipcManager)
        {
            ValueRef = HandlerDisplay.UpdateValue(ipcManager, Context, Address);
        }

        public virtual void DeregisterValue(IPCManager ipcManager)
        {
            HandlerDisplay.DeregisterValue(ipcManager, Context);
        }

        public override void Update()
        {
            base.Update();

            if (!string.IsNullOrEmpty(CommonSettings.AddressAction) && !string.IsNullOrEmpty(DisplaySettings.Address))
                CommonSettings.IsInitialized = true;
            else
                CommonSettings.IsInitialized = false;

            currentValue = DisplaySettings.OffState;
        }

        public override void Refresh(ImageManager imgManager)
        {
            if (ValueRef == null || ValueRef.Value == null || AddressAction == "")
            {
                SetError();
                return;
            }
            else
            {
                if (!ValueRef.IsChanged && !ForceUpdate)
                    return;
                string lastImage = DrawImage;

                string value = ValueRef.Value;
                if (Settings.OnState == value || Settings.OffState == value)
                {
                    if (Settings.OnState == value)
                        DrawImage = Settings.OnImage;
                    else
                        DrawImage = Settings.OffImage;
                }
                else if (Settings.HasIndication)
                {
                    if (Settings.IndicationValueAny || Settings.IndicationValue == value)
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
}
