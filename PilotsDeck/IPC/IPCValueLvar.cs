using FSUIPC;
using System;
using System.Globalization;
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCValueLvar:Process", $"Exception while Reading LVar! (Address: {Address}) (SimType: {simType}) (Exception: {ex.GetType()})");
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

        public override void SetValue(string strValue)
        {
            if (double.TryParse(strValue, NumberStyles.Number, new RealInvariantFormat(strValue), out double value))
            {
                //isChanged = currentValue != value;
                //if (isChanged)
                //    currentValue = value;
                isChanged = true;
                currentValue = value;
            }
        }
    }
}
