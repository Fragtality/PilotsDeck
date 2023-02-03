using System.Globalization;

namespace PilotsDeck
{
    public class ValueManipulator
    {
        protected double value;
        protected string strValue;
        protected string result = "";
        protected string code;
        protected int factor;

        public static string CalculateSwitchValue(string currentValue, string offState, string onState, int ticks)
        {
            if (!string.IsNullOrWhiteSpace(onState) && onState[0] == '$' && double.TryParse(currentValue, NumberStyles.Number, new RealInvariantFormat(currentValue), out _))
            {
                ValueManipulator valueManipulator = new();
                string newValue = valueManipulator.GetValue(currentValue, onState, ticks);
                return newValue;
            }
            else
                return ToggleSwitchValue(currentValue, offState, onState);
        }

        public static string ToggleSwitchValue(string currentValue, string offState, string onState)
        {
            string newValue;
            if (currentValue == offState ||
                (double.TryParse(currentValue, new RealInvariantFormat(currentValue), out double val) && double.TryParse(offState, new RealInvariantFormat(offState), out double off) && val == off))
                newValue = onState;
            else
                newValue = offState;
            Logger.Log(LogLevel.Debug, "ValueManipulator:ToggleSwitchValue", $"Toggled Value '{currentValue}' -> '{newValue}'.");
            return newValue;
        }

        public string GetValue(string val, string strCode, int ticks)
        {
            code = strCode;
            factor = ticks;
            strValue = val;
            if (!double.TryParse(strValue, NumberStyles.Number, new RealInvariantFormat(strValue), out value) || string.IsNullOrEmpty(code) || code[0] != '$')
                return strValue;

            code = code.Trim()[1..];
            bool sequence = false;

            if (code.Contains(':') || code.Length == 1 || (code.Length >= 2 && !code.Contains(',') && double.TryParse(code, new RealInvariantFormat(code), out _)))
                Counter();
            else
            {
                sequence = true;
                Sequence();
            }

            Logger.Log(LogLevel.Debug, "ValueManipulator:GetValue", $"{(sequence ? "Sequence" : "Counter")} Value '{val}' -> '{result}'.");
            return result;
        }

        protected void Sequence()
        {
            bool bounce = code.EndsWith('<');
            code = code.Replace("<", "");
            bool numeric = false;
            if (double.TryParse(strValue, new RealInvariantFormat(strValue), out double val))
                numeric = true;

            string[] parts = code.Split(',');
            string defValue = parts[^1].Replace("=", "");
            for (int i=0; i<parts.Length; i++)
            {
                if (parts[i].Contains('='))
                {
                    parts[i] = parts[i].Replace("=","");
                    defValue = parts[i];
                }

                if (parts[i] == strValue || (numeric && double.TryParse(parts[i], new RealInvariantFormat(parts[i]), out double step) && val == step))
                {
                    if (i + 1 < parts.Length)
                    {
                        result = parts[i + 1].Replace("=", "");
                        break;
                    }
                    else
                    {
                        if (!bounce)
                            result = defValue;
                        else if (i >= 1)
                            result = parts[i-1].Replace("=", "");
                    }
                }
            }
            if (string.IsNullOrEmpty(result))
                result = defValue;
        }

        protected void Counter()
        {
            bool increase = code[0] != '-';
            if (code[0] == '-' || code[0] == '+')
                code = code[1..];
            string[] parts = code.Split(':');

            if (string.IsNullOrEmpty(parts[0]))
            {
                parts[0] = "1";
            }

            if (!double.TryParse(parts[0], NumberStyles.Number, new RealInvariantFormat(parts[0]), out double step))
                return;
            step *= (double)factor;

            if (parts.Length == 2)
            {
                if (!double.TryParse(parts[1], NumberStyles.Number, new RealInvariantFormat(parts[1]), out double limit))
                    return;

                if (increase && (value + step) <= limit)
                    value += step;

                if (!increase && (value - step) >= limit)
                    value -= step;
            }
            else
            {
                if (increase)
                    value += step;

                if (!increase)
                    value -= step;
            }

            result = string.Format(AppSettings.numberFormat, "{0:G}", value);
        }
    }
}
