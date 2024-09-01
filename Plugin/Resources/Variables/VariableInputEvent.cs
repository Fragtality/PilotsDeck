namespace PilotsDeck.Resources.Variables
{
    public class VariableInputEvent(string address) : VariableNumeric(address, SimValueType.BVAR)
    {
        public ulong Hash { get; set; }
    }
}
