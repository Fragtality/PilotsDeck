using FSUIPC;
using Serilog;

namespace PilotsDeck
{
    public class IPCValueLvar : IPCValue
    {
        private bool isChanged = false;
        private double currentValue = 0;

        public IPCValueLvar(string _address) : base(_address)
        {

        }

        public override bool IsChanged { get { return isChanged; } }

        public override void Process()
        {
            try
            {
                double result = FSUIPCConnection.ReadLVar(Address);
                isChanged = currentValue != result;
                if (isChanged)
                    currentValue = result;
            }
            catch
            {
                Log.Logger.Error($"Exception while Reading LVar {Address}");
            }
        }

        protected override string Read()
        {
            return currentValue.ToString();
        }

        public override dynamic RawValue()
        {
            return currentValue;
        }
    }
}
