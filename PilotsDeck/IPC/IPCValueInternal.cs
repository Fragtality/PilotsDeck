using System;
using System.Globalization;

namespace PilotsDeck
{
    public class IPCValueInternal(string _address) : IPCValue(_address)
    {
        private bool isChanged = false;
        private string _value = "";
        private string lastValue = "";
        public override bool IsChanged { get { return isChanged; } }

        public override void Process(SimulatorType simType)
        {
            isChanged = lastValue != _value;
            lastValue = _value;
        }

        public override void SetValue(string strValue)
        {
            _value = strValue;
        }

        public override void SetValue(double dblValue)
        {
            SetValue(Convert.ToString(dblValue, CultureInfo.InvariantCulture));
        }

        protected override string Read()
        {
            return _value;
        }

        public override dynamic RawValue()
        {
            if (double.TryParse(_value, NumberStyles.Number, new RealInvariantFormat(_value), out double value))
                return value;
            else
                return _value;
        }
    }
}
