using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System.Text.Json.Serialization;

namespace PilotsDeck.Actions.Advanced
{
    public enum Comparison
    {
        LESS,
        LESS_EQUAL,
        GREATER,
        GREATER_EQUAL,
        EQUAL,
        NOT_EQUAL,
        CONTAINS,
        NOT_CONTAINS,
        HAS_CHANGED
    }

    public class ConditionHandler
    {
        [JsonIgnore]
        public ManagedVariable Variable { get; set; } = null;
        public string Address { get; set; } = "";
        public Comparison Comparison { get; set; } = Comparison.EQUAL;
        public string Value { get; set; } = "";

        public ConditionHandler()
        {
            Variable = null;
            Address = "";
            Comparison = Comparison.EQUAL;
            Value = "";
        }

        public ConditionHandler(ConditionHandler source)
        {
            Variable = null;
            Address = new(source.Address);
            Comparison = source.Comparison;
            Value = new(source.Value);
        }

        public ConditionHandler Copy()
        {
            return new ConditionHandler(this);
        }

        public bool Compare()
        {
            if (Variable == null)
                return false;

            if (Variable.IsNumericValue && Conversion.IsNumber(Value))
            {
                double var = Variable.NumericValue;
                double val = Conversion.ToDouble(Value);

                if (Comparison == Comparison.LESS)
                    return var < val;
                else if (Comparison == Comparison.LESS_EQUAL)
                    return var <= val;
                else if (Comparison == Comparison.GREATER)
                    return var > val;
                else if (Comparison == Comparison.GREATER_EQUAL)
                    return var >= val;
                else if (Comparison == Comparison.EQUAL)
                    return var == val;
                else if (Comparison == Comparison.NOT_EQUAL)
                    return var != val;
                else if (Comparison == Comparison.CONTAINS)
                    return Variable.Value.Contains(Value);
                else if (Comparison == Comparison.NOT_CONTAINS)
                    return !Variable.Value.Contains(Value);
                else if (Comparison == Comparison.HAS_CHANGED)
                    return Variable.IsChanged;
            }
            else
            {
                string var = Variable.Value;
                if (Comparison == Comparison.EQUAL)
                    return var == Value;
                else if (Comparison == Comparison.NOT_EQUAL)
                    return var != Value;
                else if (Comparison == Comparison.CONTAINS)
                    return var.Contains(Value);
                else if (Comparison == Comparison.NOT_CONTAINS)
                    return !var.Contains(Value);
                else if (Comparison == Comparison.HAS_CHANGED)
                    return Variable.IsChanged;
            }

            return false;
        }
    }
}
