namespace PilotsDeck
{
    public class ModelDisplayRadio : ModelDisplaySwitch
    {
        public virtual string AddressRadio1 { get; set; } = "";
        public virtual string AddressRadio2 { get; set; } = "";

        public ModelDisplayRadio()
        {
            //Default / Error Image?
        }
    }
}
