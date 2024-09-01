using PilotsDeck.Tools;
using System;

namespace PilotsDeck.Actions
{
    public static class ToolsValueState
    {
        public static string Toggle(string value, string onState, string offState)
        {
            string newValue = offState;
            if (value == offState || Conversion.ToDouble(value, 0.0f) == Conversion.ToDouble(offState, 0.0f))
                newValue = onState;

            if (value != newValue)
                Logger.Debug($"Toggled Value '{value}' -> '{newValue}'");
            return newValue;
        }

        public static string GetCounter(string code, double numValue, string stringValue, int ticks)
        {
            string result = "";
            bool increase = code[0] != '-';
            if (code[0] == '-' || code[0] == '+')
                code = code[1..];
            string[] parts = code.Split(':');

            if (string.IsNullOrEmpty(parts[0]))
            {
                parts[0] = "1";
            }

            if (!Conversion.IsNumber(parts[0], out double step))
                return result;
            step *= Math.Abs(ticks);

            if (parts.Length == 2)
            {
                if (!Conversion.IsNumber(parts[1], out double limit))
                    return result;

                if (increase && numValue + step <= limit)
                    numValue += step;

                if (!increase && numValue - step >= limit)
                    numValue -= step;
            }
            else if (parts.Length == 3)
            {
                if (!Conversion.IsNumber(parts[1], out double limit))
                    return result;

                if (!Conversion.IsNumber(parts[2], out double reset))
                    return result;

                if (increase && numValue + step <= limit)
                    numValue += step;
                else if (increase && numValue + step > limit)
                    numValue = reset;

                if (!increase && numValue - step >= limit)
                    numValue -= step;
                else if (!increase && numValue - step < limit)
                    numValue = reset;
            }
            else
            {
                if (increase)
                    numValue += step;

                if (!increase)
                    numValue -= step;
            }

            result = Conversion.ToString(numValue);
            Logger.Debug($"Counter Value '{stringValue}' -> '{result}' (ticks: {ticks} | steps: {step})");
            return result;
        }

        public static string GetSequence(string code, double numValue, string stringValue, bool isNumeric)
        {
            string result = "";
            bool bounce = code.EndsWith('<');
            code = code.Replace("<", "");

            string[] parts = code.Split(',');
            string defValue = parts[^1].Replace("=", "");
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Contains('='))
                {
                    parts[i] = parts[i].Replace("=", "");
                    defValue = parts[i];
                }

                if (parts[i] == stringValue || isNumeric && Conversion.IsNumber(parts[i], out double step) && numValue == step)
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
                            result = parts[i - 1].Replace("=", "");
                    }
                }
            }

            if (string.IsNullOrEmpty(result))
                result = defValue;

            Logger.Debug($"Sequence Value '{stringValue}' -> '{result}'");
            return result;
        }
    }
}
