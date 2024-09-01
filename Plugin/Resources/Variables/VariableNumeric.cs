using PilotsDeck.Tools;

namespace PilotsDeck.Resources.Variables
{
    public class VariableNumeric(string address, SimValueType type) : ManagedVariable(address, type)
    {
        protected double DoubleValue { get; set; } = 0;
        protected double ValueLast { get; set; } = 0;
        public override bool IsNumericValue { get { return true; } }

        public override string Value
        {
            get { return Conversion.ToString(DoubleValue); }
        }

        public override double NumericValue
        {
            get { return DoubleValue; }
        }

        public override void CheckChanged()
        {
            IsChanged = DoubleValue != ValueLast;
            ValueLast = DoubleValue;
        }

        public override dynamic RawValue()
        {
            return DoubleValue;
        }

        public override void SetValue(string value)
        {
            DoubleValue = Conversion.ToDouble(value);
        }

        public override void SetValue(double value)
        {
            DoubleValue = value;
        }
    }
}
