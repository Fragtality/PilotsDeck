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

        public string GetValue(string val, string strCode, int ticks)
        {
            code = strCode;
            factor = ticks;
            strValue = val;
            if (!double.TryParse(strValue, NumberStyles.Number, new RealInvariantFormat(strValue), out value) || string.IsNullOrEmpty(code) || code[0] != '$')
                return strValue;

            code = code.Trim()[1..];

            if (code[0] == '+' || code[0] == '-')
                Counter();
            else if (code.Contains(',') || code.Contains('='))
                Sequence();

            return result;
        }

        protected void Sequence()
        {
            bool bounce = code.EndsWith('<');
            code = code.Replace("<", "");

            string[] parts = code.Split(',');
            string defValue = parts[^1];
            for (int i=0; i<parts.Length; i++)
            {
                if (parts[i].Contains('='))
                {
                    parts[i] = parts[i].Replace("=","");
                    defValue = parts[i];
                }

                if (parts[i] == strValue)
                {
                    if (i + 1 < parts.Length)
                    {
                        result = parts[i + 1];
                        break;
                    }
                    else
                    {
                        if (!bounce)
                            result = defValue;
                        else if (i >= 1)
                            result = parts[i-1];
                    }
                }
            }
            if (string.IsNullOrEmpty(result))
                result = defValue;
        }

        protected void Counter()
        {
            bool increase = code[0] == '+';
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

                if (increase && value <= limit - step)
                    value += step;

                if (!increase && value >= limit + step)
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
