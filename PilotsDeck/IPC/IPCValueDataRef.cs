using System;
using System.Globalization;

namespace PilotsDeck
{
    public class IPCValueDataRef(string address) : IPCValue(address)
    {
        public float FloatValue { get; set; } = 0.0f;
        private float _lastValue = 0.0f;
        private bool _valueChanged = false;

        public override bool IsChanged
        {
            get
            {
                return _valueChanged;
            }
        }

        public override void Process(SimulatorType simType)
        {
            _valueChanged = _lastValue != FloatValue;
            _lastValue = FloatValue;
        }

        public override dynamic RawValue()
        {
            return FloatValue;
        }

        protected override string Read()
        {
            string num = Convert.ToString(FloatValue, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                return num;
            else
                return string.Format("{0:F1}", FloatValue);
        }
    }
}