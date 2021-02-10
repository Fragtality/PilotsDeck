namespace PilotsDeck
{
    public class HandlerDisplayRadio : HandlerDisplaySwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override ModelDisplaySwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayRadio Settings { get; protected set; }

        //OVERRIDES for MULTI ADDRESSES
        //override current value?

        public HandlerDisplayRadio(string context, ModelDisplayRadio settings) : base(context, settings)
        {
            Settings = settings;
        }

        public override void Refresh(ImageManager imgManager)
        {
            // T O D O
        }

    }
}
