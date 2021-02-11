namespace PilotsDeck
{
    public class ModelDisplayRadio : ModelDisplaySwitch
    {
        public virtual string AddressRadio0 { get; set; } = "";
        public virtual string AddressRadio1 { get; set; } = "";

        public ModelDisplayRadio() : base()
        {
            //Default / Error Image?
        }
    }
}
