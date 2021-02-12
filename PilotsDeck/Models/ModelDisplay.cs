using System;

namespace PilotsDeck
{
    public class ModelDisplay : ModelBase
    {
        public virtual string Address { get; set; } = "";

        public virtual bool DecodeBCD { get; set; } = false;
        public virtual double Scalar { get; set; } = 1.0f;
        public virtual string Format { get; set; } = "";

        public virtual string ScaleValue(string value)
        {
            if (double.TryParse(value, out double num) && Scalar != 1)
                return Convert.ToString(num * Scalar);
            else
                return value;
        }

        public virtual string RoundValue(string value)
        {
            string[] parts = Format.Split(':');

            if (parts.Length >= 1 && int.TryParse(parts[0], out int num) && double.TryParse(value, out double dbl))
                return string.Format($"{{0:F{num}}}", Math.Round(dbl, num));
            else
                return value;
        }

        public virtual string FormatValue(string value)
        {
            string[] parts = Format.Split(':');
            string replaceFrom = AppSettings.stringReplace;

            if (Format.Length < 1 || parts.Length < 1 || !Format.Contains(replaceFrom))
                return value;
            else if (parts.Length >= 2 && int.TryParse(parts[0], out _) && Format.Substring(parts[0].Length + 1).Contains(replaceFrom))
                return string.Format(Format.Substring(parts[0].Length + 1).Replace(replaceFrom, "{0}"), value);
            else
                return string.Format(Format.Replace(replaceFrom, "{0}"), value);
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

            return Convert.ToString(numOut);
        }
    }
}
