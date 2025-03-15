using PilotsDeck.Resources.Variables;

namespace PilotsDeck.Simulator.XP.WS
{
    public class IdMappingXP
    {
        public long RefId { get; set; } = 0;
        public bool IsRequested { get; set; } = true;
        public string Address { get; set; } = "";
        public ManagedVariable ValueRef { get; set; } = null;
        public bool IsString => StringLength > 0;
        public int StringLength { get; set; } = -1;
        public bool IsArray => ArrayIndex >= 0;
        public int ArrayIndex { get; set; } = -1;

        public static IdMappingXP Create(long id, ManagedVariable managedVariable)
        {
            var address = managedVariable.Address;
            return new()
            {
                RefId = id,
                Address = address.Address,
                ValueRef = managedVariable,
                StringLength = address.TryGetXpStringLength(out int len) ? len : -1,
                ArrayIndex = address.TryGetXpArrayIndex(out int index) ? index : -1
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is IdMappingXP idMapping)
                return this.RefId == idMapping.RefId && this.Address == idMapping.Address;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return RefId.GetHashCode() ^ Address.GetHashCode();
        }
    }
}
