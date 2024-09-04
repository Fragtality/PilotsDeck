using System;
using System.Collections.Generic;
using System.Globalization;

namespace PilotsDeck.Tools
{
    public static class Conversion
    {
        public static string ToString(double value)
        {
            string num = string.Format(CultureInfo.InvariantCulture, "{0:G15}", value);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                return num;
            else
                return string.Format(CultureInfo.InvariantCulture, $"{{0:F{(idxE < num.Length ? idxE - 1 : 15)}}}", value);
        }

        public static string ToString(float value)
        {
            string num = string.Format(CultureInfo.InvariantCulture, "{0:G7}", value);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                return num;
            else
                return string.Format(CultureInfo.InvariantCulture, $"{{0:F{(idxE < num.Length ? idxE - 1 : 7)}}}", value);
        }

        public static string ToString(double value, int digits)
        {
            string num = string.Format(CultureInfo.InvariantCulture, $"{{0,{digits}:G}}", value);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                return num;
            else
                return string.Format(CultureInfo.InvariantCulture, $"{{0,{digits}:F1}}", value);
        }

        public static double ToDouble(string valString, double def = 0)
        {
            if (double.TryParse(valString, NumberStyles.Number, new RealInvariantFormat(valString), out double numValue))
                return numValue;
            else if (bool.TryParse(valString, out bool boolValue))
                return boolValue ? 1 : 0;
            else
                return def;
        }

        public static float ToFloat(string valString, float def = 0)
        {
            if (float.TryParse(valString, NumberStyles.Number, new RealInvariantFormat(valString), out float numValue))
                return numValue;
            else if (bool.TryParse(valString, out bool boolValue))
                return boolValue ? 1 : 0;
            else
                return def;
        }

        public static float[] ToFloatArray(string valString, float[] defaults)
        {
            List<float> results = [];
            string[] parts = valString.Split(';');
            int n = 0;
            float def;
            foreach (var part in parts)
            {
                def = 0;
                if (n < defaults.Length)
                    def = defaults[n];
                results.Add(ToFloat(part, def));
                n++;
            }

            return [..results];
        }

        public static bool IsNumber(string valString)
        {
            return IsNumber(valString, out _) || bool.TryParse(valString, out _);
        }

        public static bool IsNumber(string valString, out double numValue)
        {
            if (!double.TryParse(valString, NumberStyles.Number, new RealInvariantFormat(valString), out numValue))
            {
                bool result = bool.TryParse(valString, out bool boolValue);
                numValue = boolValue ? 1 : 0;
                return result;
            }
            else
                return true;
        }

        public static bool IsNumberF(string valString, out float numValue)
        {
            if (!float.TryParse(valString, NumberStyles.Number, new RealInvariantFormat(valString), out numValue))
            {
                bool result = bool.TryParse(valString, out bool boolValue);
                numValue = boolValue ? 1 : 0;
                return result;
            }
            else
                return true;
        }

        public static bool IsNumberI(string valString, out int numValue)
        {
            if (!int.TryParse(valString, NumberStyles.Number, new RealInvariantFormat(valString), out numValue))
            {
                bool result = bool.TryParse(valString, out bool boolValue);
                numValue = boolValue ? 1 : 0;
                return result;
            }
            else
                return true;
        }

        public static string ConvertFromBCD(string value)
        {
            if (!short.TryParse(value, out short numShort))
                return value;

            return ToString(ConvertFromBCD(numShort));
        }

        public static int ConvertFromBCD(short numShort)
        {
            byte[] numBytes = BitConverter.GetBytes(numShort);
            int numOut = 0;
            for (int i = numBytes.Length - 1; i >= 0; i--)
            {
                numOut *= 100;
                numOut += (10 * (numBytes[i] >> 4));
                numOut += numBytes[i] & 0xf;
            }

            return numOut;
        }
    }

    public class RealInvariantFormat : IFormatProvider
    {
        public NumberFormatInfo formatInfo = CultureInfo.InvariantCulture.NumberFormat;

        public RealInvariantFormat(string value)
        {
            if (value == null)
            {
                formatInfo = new CultureInfo("en-US").NumberFormat;
                return;
            }

            int lastPoint = value.LastIndexOf('.');
            int lastComma = value.LastIndexOf(',');
            if (lastComma > lastPoint)
            {
                formatInfo = new CultureInfo("de-DE").NumberFormat;
            }
            else
            {
                formatInfo = new CultureInfo("en-US").NumberFormat;
            }
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(NumberFormatInfo))
            {
                return formatInfo;
            }
            else
                return null;
        }
    }
}
