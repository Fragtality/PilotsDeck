using PilotsDeck.Resources.Variables;

namespace PilotsDeck.Simulator.XP
{
    public class IndexMappingXP
    {
        public int SimIndex { get; set; } = 0;
        public bool IsRequested { get; set; } = true;
        public string Address { get; set; } = "";
        public ManagedVariable ValueRef { get; set; } = null;
        public bool IsString { get; set; } = false;
        public int CharIndex { get; set; } = 0;

        public static IndexMappingXP Create(int nextIndex, ManagedVariable managedVariable)
        {
            return new()
            {
                SimIndex = nextIndex,
                Address = managedVariable.Address.Address,
                ValueRef = managedVariable,
                IsString = false
            };
        }

        public static IndexMappingXP CreateString(int nextIndex, string address, int charIndex, ManagedVariable managedVariable)
        {
            return new()
            {
                SimIndex = nextIndex,
                Address = address,
                ValueRef = managedVariable,
                IsString = true,
                CharIndex = charIndex
            };
        }
    }
}
