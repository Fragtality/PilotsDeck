using CFIT.AppLogger;
using CFIT.AppTools;
using System;

namespace PilotsDeck.Resources.Variables
{
    public class VariableString(ManagedAddress address) : ManagedVariable(address)
    {
        public static readonly int MAX_CHARS = 1024;
        protected string StringValue { get; set; } = "0";
        protected string ValueLast { get; set; } = "0";

        public override string Value
        {
            get { return StringValue; }
        }

        public override double NumericValue
        {
            get { return Conversion.ToDouble(StringValue); }
        }

        protected override bool CheckNotEqualLast()
        {
            return StringValue?.Equals(ValueLast) == false;
        }

        protected override void SetLast()
        {
            ValueLast = StringValue;
        }

        public override dynamic RawValue()
        {
            if (Conversion.IsNumber(StringValue, out double numValue))
                return numValue;
            else
                return StringValue;
        }

        public override void SetValueString(string value)
        {
            if (App.Configuration.VerboseLogging)
                Logger.Verbose($"Setting Value {value}");
            StringValue = value;
            StringValue = StringValue.Trim('\x0');
        }

        public override void SetValueDouble(double value)
        {
            SetValue(Conversion.ToString(value));
        }

        public virtual void SetChar(int index, float chr)
        {
            SetChar(index, Convert.ToChar((int)chr));
        }

        public virtual void SetChar(int index, char chr)
        {
            if (index < MAX_CHARS && chr != 224)
            {
                char[] array = new char[MAX_CHARS];
                StringValue.CopyTo(0, array, 0, StringValue.Length);
                array[index] = chr;
                StringValue = new string(array);
                StringValue = StringValue.Trim('\x0');
            }
        }
    }
}
