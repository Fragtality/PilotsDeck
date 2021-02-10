using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public class ModelDisplay : ModelBase
    {
        public string Address { get; set; } = "";

        public bool DecodeBCD { get; set; } = false;
        public double Scalar { get; set; } = 1.0f;
        public string Format { get; set; } = "";

        protected StreamDeckTools.StreamDeckTitleParameters TitleParameters { get; set; }

        public virtual void Update()
        {

        }

        public virtual void SetTitleParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            TitleParameters = titleParameters;
        }

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

        //public static string ScaleValue(string value, double scalar)
        //{
        //    if (double.TryParse(value, out double num))
        //        return Convert.ToString(num * scalar);
        //    else
        //        return value;
        //}

        //public static string RoundValue(string value, string format)
        //{
        //    string[] parts = format.Split(':');

        //    if (parts.Length >= 1 && int.TryParse(parts[0], out int num) && double.TryParse(value, out double dbl))
        //        return string.Format($"{{0:F{num}}}", Math.Round(dbl, num));
        //    else
        //        return value;
        //}

        //public static string FormatValue(string value, string format)
        //{
        //    string[] parts = format.Split(':');
        //    string replaceFrom = AppSettings.stringReplace;

        //    if (format.Length < 1 || parts.Length < 1 || !format.Contains(replaceFrom))
        //        return value;
        //    else if (parts.Length >= 2 && int.TryParse(parts[0], out _) && format.Substring(parts[0].Length + 1).Contains(replaceFrom))
        //        return string.Format(format.Substring(parts[0].Length + 1).Replace(replaceFrom, "{0}"), value);
        //    else
        //        return string.Format(format.Replace(replaceFrom, "{0}"), value);
        //}
    }
}
