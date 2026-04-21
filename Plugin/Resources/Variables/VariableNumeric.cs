using CFIT.AppTools;
namespace PilotsDeck.Resources.Variables
{
    public class VariableNumeric(ManagedAddress address) : ManagedVariable(address)
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

        protected override bool CheckNotEqualLast()
        {
            return DoubleValue != ValueLast;
        }

        protected override void SetLast()
        {
            ValueLast = DoubleValue;
        }

        public override dynamic RawValue()
        {
            return DoubleValue;
        }

        public override void SetValueString(string value)
        {
            DoubleValue = Conversion.ToDouble(value);
        }

        public override void SetValueDouble(double value)
        {
            DoubleValue = value;
        }
    }
}
