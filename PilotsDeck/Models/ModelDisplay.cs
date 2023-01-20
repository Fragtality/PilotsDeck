using System;
using System.Globalization;

namespace PilotsDeck
{
    public class ModelDisplay : ModelBase
    {
        public virtual string Address { get; set; } = "";
        public virtual bool DrawBox { get; set; } = true;
        public virtual bool DecodeBCD { get; set; } = false;
        public virtual string Scalar { get; set; } = "1";
        public virtual string Format { get; set; } = "";

        public virtual string ScaleValue(string value)
        {
            return ScaleValue(value, Scalar);
        }

        public static string ScaleValue(string strValue, string strScalar)
        {
            if (strScalar != "1" && double.TryParse(strValue, NumberStyles.Number, new RealInvariantFormat(strValue), out double value) && double.TryParse(strScalar, NumberStyles.Number, new RealInvariantFormat(strScalar), out double scalar))
                return string.Format(AppSettings.numberFormat, "{0:G}", value * scalar);
            else
                return strValue;
        }

        public virtual string RoundValue(string value)
        {
            return RoundValue(value, Format);
        }

        public static string RoundValue(string value, string format)
        {
            string[] parts = format.Split(':');
            int signsLeading = 0;
            int signsTrailing = 0;
            bool canRound = false;

            if (parts.Length >= 1 && parts[0].Contains('.'))
            {
                string[] signs = parts[0].Split('.');
                if (signs.Length == 1)
                    canRound = int.TryParse(signs[0], out signsLeading);
                if (signs.Length == 2)
                {
                    _ = int.TryParse(signs[0], out signsLeading);
                    canRound = int.TryParse(signs[1], out signsTrailing);
                    if (signsTrailing > 0)
                        signsLeading += 1;
                }
            }
            else if (parts.Length >= 1 && int.TryParse(parts[0], out signsTrailing))
                canRound = true;

            if (canRound && double.TryParse(value, NumberStyles.Number, new RealInvariantFormat(value), out double dbl))
                return string.Format(AppSettings.numberFormat, $"{{0,{signsLeading + signsTrailing}:F{signsTrailing}}}", Math.Round(dbl, signsTrailing)).Replace(' ', '0');
            else
                return value;
        }

        public virtual string FormatValue(string value)
        {
            if (!string.IsNullOrEmpty(value))
                return FormatValue(value, Format);
            else
                return value;
        }

        public static string FormatValue(string value, string format)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            string[] parts = format.Split(':');
            string replaceFrom = AppSettings.stringReplace;

            if (parts.Length >= 2 && parts[1].Contains(replaceFrom))
                return string.Format(AppSettings.numberFormat, parts[1].Replace(replaceFrom, "{0}"), value);
            else if (parts.Length == 1 && parts[0].Contains(replaceFrom))
                return string.Format(AppSettings.numberFormat, parts[0].Replace(replaceFrom, "{0}"), value);
            else
                return value;
        }

        public static string ConvertFromBCD(string value)
        {
            if (!short.TryParse(value, out short numShort))
                return value;

            byte[] numBytes = BitConverter.GetBytes(numShort);
            int numOut = 0;
            for (int i = numBytes.Length - 1; i >= 0; i--)
            {
                numOut *= 100;
                numOut += (10 * (numBytes[i] >> 4));
                numOut += numBytes[i] & 0xf;
            }

            return Convert.ToString(numOut, CultureInfo.InvariantCulture.NumberFormat);
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
