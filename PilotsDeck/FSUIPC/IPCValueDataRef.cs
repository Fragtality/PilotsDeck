using System;
using System.Globalization;

namespace PilotsDeck
{
    public class IPCValueDataRef : IPCValue
    {
        public float FloatValue { get; set; } = 0.0f;
        private float _lastValue = 0.0f;

        public IPCValueDataRef(string address) : base(address)
        {

        }

        public override bool IsChanged
        {
            get
            { 
                bool result = _lastValue != FloatValue;
                _lastValue = FloatValue;
                return result;
            }
        }

        public override dynamic RawValue()
        {
            return FloatValue;
        }

        protected override string Read()
        {
            string num = Convert.ToString(FloatValue, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf("E");
            if (idxE < 0)
                return num;
            else
                return string.Format("{0:F1}", FloatValue);
        }
    }
}
