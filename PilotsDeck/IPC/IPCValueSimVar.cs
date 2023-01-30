using FSUIPC;
using System;
using System.Globalization;
using WASM = FSUIPC.MSFSVariableServices;

namespace PilotsDeck
{
    public class IPCValueSimVar : IPCValue
    {
        private bool isChanged = false;
        private double DoubleValue = 0.0;
        private double lastValue = 0.0;

        public IPCValueSimVar(string _address) : base(_address)
        {

        }

        public override bool IsChanged { get { return isChanged; } }

        public override void Process(SimulatorType simType)
        {
            try
            {
                //double result = 0;

                if (simType == SimulatorType.FSX || simType == SimulatorType.P3D || (simType == SimulatorType.MSFS && AppSettings.Fsuipc7LegacyLvars))
                    SetValue(FSUIPCConnection.ReadLVar(Address));
                else if (simType == SimulatorType.MSFS && !AppSettings.Fsuipc7LegacyLvars && !AppSettings.preferrMobiWASM && WASM.LVars.Exists(Address))
                    SetValue(WASM.LVars[Address].Value);

                //isChanged = currentValue != result;
                //if (isChanged)
                //    currentValue = result;
                isChanged = lastValue != DoubleValue;
                lastValue = DoubleValue;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCValueSimVar:Process", $"Exception while Reading SimVar! (Address: {Address}) (SimType: {simType}) (Exception: {ex.GetType()})");
            }
        }

        protected override string Read()
        {
            string num = Convert.ToString(DoubleValue, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf("E");
            if (idxE < 0)
                return num;
            else
                return string.Format("{0:F1}", DoubleValue);
        }

        public override dynamic RawValue()
        {
            return DoubleValue;
        }

        public override void SetValue(string strValue)
        {
            if (double.TryParse(strValue, NumberStyles.Number, new RealInvariantFormat(strValue), out double value))
            {
                SetValue(value);
            }
        }

        public override void SetValue(double value)
        {
            DoubleValue = value;
        }
    }
}
