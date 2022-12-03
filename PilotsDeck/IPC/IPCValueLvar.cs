using FSUIPC;
using Serilog;
using WASM = FSUIPC.MSFSVariableServices;

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

        public override void Process(SimulatorType simType)
        {
            try
            {
                double result = 0;

                if (simType == SimulatorType.FSX || simType == SimulatorType.P3D || (simType == SimulatorType.MSFS && AppSettings.Fsuipc7LegacyLvars))
                    result = FSUIPCConnection.ReadLVar(Address);
                else if (simType == SimulatorType.MSFS && !AppSettings.Fsuipc7LegacyLvars && WASM.LVars.Exists(Address))
                    result = WASM.LVars[Address].Value;

                isChanged = currentValue != result;
                if (isChanged)
                    currentValue = result;
            }
            catch
            {
                Log.Logger.Error($"Exception while Reading LVar {Address} via {simType}");
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
