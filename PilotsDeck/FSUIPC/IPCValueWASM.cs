using FSUIPC;
using Serilog;
using WASM = FSUIPC.MSFSVariableServices;

namespace PilotsDeck
{
    public class IPCValueWASM : IPCValue
    {
        private bool isChanged = false;
        private double currentValue = 0;

        public IPCValueWASM(string _address) : base(_address)
        {

        }

        public override bool IsChanged { get { return isChanged; } }

        //public override void Process(MSFSVariableServices WASM)
        public override void Process()
        {
            try
            {
                if (WASM.LVars.Exists(Address))
                {
                    double result = WASM.LVars[Address].Value;
                    isChanged = currentValue != result;
                    if (isChanged)
                        currentValue = result;
                }
            }
            catch
            {
                Log.Logger.Error($"Exception while Reading LVar {Address} via WASM");
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
