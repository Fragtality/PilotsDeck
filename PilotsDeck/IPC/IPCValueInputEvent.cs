using System;
using System.Globalization;

namespace PilotsDeck
{
    public class IPCValueInputEvent(string _address) : IPCValue(_address)
    {
        private bool isChanged = false;
        private double DoubleValue = 0.0;
        private double lastValue = 0.0;
        public ulong Hash { get; set; }

        public override bool IsChanged { get { return isChanged; } }

        public override void Process(SimulatorType simType)
        {
            isChanged = lastValue != DoubleValue;
            lastValue = DoubleValue;
        }

        protected override string Read()
        {
            string num = Convert.ToString((float)DoubleValue, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf('E');
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
