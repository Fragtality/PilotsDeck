using FSUIPC;
using System;
using System.Globalization;

namespace PilotsDeck
{
    public class IPCValueSimVar(string _address) : IPCValue(_address)
    {
        private bool isChanged = false;
        private string _value = "0";
        private string _lastValue = "0";

        public override bool IsChanged { get { return isChanged; } }

        public override void Process(SimulatorType simType)
        {
            try
            {
                if ((simType == SimulatorType.FSX || simType == SimulatorType.P3D || (simType == SimulatorType.MSFS && AppSettings.Fsuipc7LegacyLvars))
                    && FSUIPCConnection.IsOpen)
                    SetValue(FSUIPCConnection.ReadLVar(Address));

                isChanged = _lastValue != _value;
                _lastValue = _value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCValueSimVar:Process", $"Exception while Reading SimVar! (Address: {Address}) (SimType: {simType}) (Exception: {ex.GetType()})");
            }
        }

        protected override string Read()
        {
            return _value;
        }

        public override dynamic RawValue()
        {
            if (double.TryParse(_value, new RealInvariantFormat(_value), out double numValue))
                return numValue;
            else
                return _value;
        }

        public override void SetValue(string strValue)
        {
            _value = strValue;
        }

        public override void SetValue(double value)
        {
            string num = Convert.ToString(value, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                _value = num;
            else
                _value = string.Format("{0:F1}", num);
        }
    }
}
