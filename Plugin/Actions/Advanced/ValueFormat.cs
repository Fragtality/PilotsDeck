using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Resources.Variables;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PilotsDeck.Actions.Advanced
{
    public enum DisplayValueType
    {
        NUMBER = 1,
        STRING,
    }

    public class ValueFormat()
    {
        public DisplayValueType TypePreferrence { get; set; } = DisplayValueType.NUMBER;
        public bool DecodeBCD { get; set; } = false;
        public bool UseAbsoluteValue { get; set; } = false;
        public double Scalar { get; set; } = 1;
        public double Offset { get; set; } = 0;
        public bool OffsetFirst { get; set; } = false;
        public int Digits { get; set; } = -1;
        public int DigitsTrailing { get; set; } = -1;
        public bool LimitDigits { get; set; } = false;
        public bool InsertSign { get; set; } = false;
        public bool InsertSpace { get; set; } = false;
        public int Round { get; set; } = -1;
        public bool RoundFloor { get; set; } = false;
        public bool RoundCeiling { get; set; } = false;
        public int SubIndex { get; set; } = -1;
        public int SubLength { get; set; } = 0;
        public string FormatString { get; set; } = "";
        public SortedDictionary<string, string> Mappings { get; set; } = [];

        public ValueFormat Copy()
        {
            ValueFormat model = new()
            {
                TypePreferrence = TypePreferrence,
                DecodeBCD = DecodeBCD,
                UseAbsoluteValue = UseAbsoluteValue,
                Scalar = Scalar,
                Offset = Offset,
                OffsetFirst = OffsetFirst,
                Digits = Digits,
                DigitsTrailing = DigitsTrailing,
                LimitDigits = LimitDigits,
                InsertSign = InsertSign,
                InsertSpace = InsertSpace,
                Round = Round,
                RoundFloor = RoundFloor,
                RoundCeiling = RoundCeiling,
                SubIndex = SubIndex,
                SubLength = SubLength,
                FormatString = FormatString,
            };

            foreach (var mapping in Mappings)
                model.Mappings.TryAdd(mapping.Key, mapping.Value);

            return model;
        }

        public string FormatValue(ManagedVariable variable)
        {
            if (variable == null)
                if (App.PluginController.State != Plugin.PluginState.IDLE)
                    return "";
                else
                    return "0";

            double value = variable.NumericValue;
            string result = variable.Value;

            if (variable.IsNumericValue && TypePreferrence == DisplayValueType.NUMBER)
            {
                if (DecodeBCD)
                    value = Conversion.ConvertFromBCD(Convert.ToInt16(value));

                if (UseAbsoluteValue)
                    value = Math.Abs(value);

                if (OffsetFirst && Offset != 0)
                    value += Offset;

                if (Scalar != 1)
                    value *= Scalar;

                if (!OffsetFirst && Offset != 0)
                    value += Offset;

                if (RoundFloor)
                    value = Math.Floor(value);
                else if (RoundCeiling)
                    value = Math.Ceiling(value);
                else if (Round >= 0)
                    value = Math.Round(value, Round);

                string prefix = "";
                if (Digits >= 0 && value >= 0)
                {
                    if (InsertSign)
                        prefix = "+";
                    else if (InsertSpace)
                        prefix = " ";
                }
                if (value < 0)
                    prefix = "-";

                string strValue = Conversion.ToString(value);
                if (strValue.StartsWith('-'))
                    strValue = strValue[1..];
                int idxDecimal = strValue.IndexOf('.');
                int numTrailing = 0;
                int numLeading;
                if (idxDecimal != -1)
                    numTrailing = strValue.Length - idxDecimal - 1;
                if (numTrailing > 0)
                    numLeading = idxDecimal;
                else
                    numLeading = strValue.Length;

                string strIntegral = strValue[0..(idxDecimal != -1 ? idxDecimal : strValue.Length)];
                if (strIntegral.Length < Digits)
                    strIntegral = $"{new string('0', Digits - numLeading)}{strIntegral}";
                else if (strIntegral.Length > Digits && LimitDigits && Digits >= 0)
                    strIntegral = strIntegral[(numLeading - Digits)..];

                string strFraction = "";
                if (idxDecimal != -1)
                {
                    strFraction = strValue[(idxDecimal + 1)..];
                    if (numTrailing < DigitsTrailing)
                        strFraction = $"{strFraction}{new string('0', DigitsTrailing - numTrailing)}";
                    else if (numTrailing > DigitsTrailing && LimitDigits && DigitsTrailing >= 0)
                        strFraction = strFraction[0..DigitsTrailing];
                }
                else if (DigitsTrailing > 0)
                    strFraction = new string('0', DigitsTrailing);

                if (string.IsNullOrEmpty(strFraction))
                    result = $"{prefix}{strIntegral}";
                else
                    result = $"{prefix}{strIntegral}.{strFraction}";
            }

            if (SubIndex >= 0 && SubIndex < result.Length)
            {
                if (SubLength >= 0 && SubIndex + SubLength <= result.Length)
                    result = result[SubIndex..SubLength];
                else
                    result = result[SubIndex..];
            }

            if (Mappings.Count > 0 && Mappings.TryGetValue(result, out string mappedValue))
                result = mappedValue;

            if (FormatString?.Contains(App.Configuration.StringReplace) == true)
                result = string.Format(CultureInfo.InvariantCulture, FormatString.Replace(App.Configuration.StringReplace, "{0}"), result);

            Logger.Verbose($"Formatted Value: '{result}'");
            return result;
        }
    }
}
