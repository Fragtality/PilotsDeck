using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PilotsDeck.Actions.Simple
{
    public class ValueState
    {
        public ManagedVariable Variable { get; set; }
        public ActionCommand ParentCommand { get; set; } = null;
        public string StringValue { get { return CalcValue(); } }
        public bool IsNumericValue { get { return Variable?.IsNumericValue == true; } }
        public double NumericValue { get { return CalcNumValue(); } }
        public string FormattedValue { get { return FormatValue(); } }
        public bool DecodeBCD { get; protected set; } = false;
        public double Scalar { get; protected set; } = 1.0;
        public string Format { get; protected set; } = "";

        public string OnState { get; protected set; } = "";
        public string OffState { get; protected set; } = "";
        public bool IsCode { get; protected set; } = false;
        public string Code { get; protected set; } = "";
        public bool IsCounter { get; protected set; } = false;
        public bool IsSequence { get; protected set; } = false;


        public ValueState(ManagedVariable variable, string onState, string offState, ActionCommand parent = null)
        {
            Variable = variable;
            OnState = onState;
            OffState = !string.IsNullOrWhiteSpace(offState) ? offState : "0";
            IsCode = OnState?.StartsWith('$') == true;
            if (IsCode)
            {
                Code = OnState?.Trim()[1..];
                IsCounter = !Code?.Contains(',') == true && Conversion.IsNumber(Code?.Split(':')?[0]);
                IsSequence = !IsCounter && ((Code?.Contains(',') == true && Code?.Length >= 3) || (Code?.Contains(',') == false && Code?.StartsWith('=') == true));
            }
            ParentCommand = parent;
        }

        public ValueState(ManagedVariable variable, string onState, bool decodeBCD = false, double scalar = 1, string format = "")
        {
            Variable = variable;
            OnState = onState;
            OffState = "0";
            DecodeBCD = decodeBCD;
            Scalar = scalar;
            Format = format;
            IsCode = OnState?.StartsWith('$') == true;
            if (IsCode)
            {
                Code = OnState?.Trim()[1..];
                IsCounter = !Code?.Contains(',') == true && Conversion.IsNumber(Code);
                IsSequence = !IsCounter && Code?.Contains(',') == true && Code?.Length >= 3;
            }
        }

        protected string CalcValue()
        {
            string value = Variable?.Value ?? "0";

            if (IsNumericValue)
            {
                if (DecodeBCD)
                    value = Conversion.ConvertFromBCD(value);
                value = ScaleValue(value);
                value = RoundValue(value);
            }

            return value;
        }

        public string ScaledValue()
        {
            string value = Variable?.Value ?? "0";

            if (IsNumericValue)
            {
                if (DecodeBCD)
                    value = Conversion.ConvertFromBCD(value);
                value = ScaleValue(value);
            }

            return value;
        }

        protected double CalcNumValue()
        {
            string value = CalcValue();

            return Conversion.ToDouble(value);
        }

        public string ScaleValue(string strValue)
        {
            if (Scalar != 1.0 && Conversion.IsNumber(strValue, out double value))
                return Conversion.ToString(value * Scalar);
            else
                return strValue;
        }

        public string RoundValue(string value)
        {
            string[] parts = Format.Split(':');
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
                }
            }
            else if (parts.Length >= 1 && int.TryParse(parts[0], out signsTrailing))
                canRound = true;

            if (canRound && Conversion.IsNumber(value, out double dbl))
            {
                if (signsLeading > 0)
                {
                    string prefix = "";
                    string result;
                    if (dbl >= 0)
                        prefix = " ";

                    if (signsTrailing >= 0)
                        result = Math.Round(dbl, signsTrailing).ToString($"{new string('0', signsLeading)}.{new string('0', signsTrailing)}", CultureInfo.InvariantCulture);
                    else
                        result = dbl.ToString($"{new string('0', signsLeading)}", CultureInfo.InvariantCulture);

                    return $"{prefix}{result}";
                }
                else if (signsTrailing >= 0)
                    return Math.Round(dbl, signsTrailing).ToString($"0.{new string('0', signsTrailing)}", CultureInfo.InvariantCulture);
                else
                    return value;
            }
            else
                return value;
        }

        protected string FormatValue()
        {
            string value = CalcValue();
            try
            {
                if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(Format))
                    return value;

                string[] parts = Format.Split(':');
                string replaceFrom = App.Configuration.StringReplace;

                if (parts.Length >= 2)
                {
                    if (parts[1].Contains(replaceFrom))
                        return string.Format(CultureInfo.InvariantCulture, parts[1].Replace(replaceFrom, "{0}"), value);
                    else if (parts[1].Contains(".."))
                        return GetSubString(value, parts[1]);
                    else
                        return value;
                }
                else if (parts.Length == 1)
                {
                    if (parts[0].Contains(replaceFrom))
                        return string.Format(CultureInfo.InvariantCulture, parts[0].Replace(replaceFrom, "{0}"), value);
                    else if (parts[0].Contains(".."))
                        return GetSubString(value, parts[0]);
                    else
                        return value;
                }
                else
                    return value;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return value;
            }
        }

        public static string GetSubString(string value, string format)
        {
            string[] nums = format.Trim().Split("..");
            if (nums.Length != 2 || !int.TryParse(nums[0], out int start) || !int.TryParse(nums[1], out int len))
                return value;

            if (start >= 0 && len >= 1 && start < value.Length && start + len <= value.Length)
                return value.Substring(start, len);
            else
                return value;
        }

        public bool Compares()
        {
            if (IsSequence)
                return CompareSequence();

            return CompareValues(OnState);
        }

        public bool ComparesOff()
        {
            if (IsSequence)
                return CompareSequence();

            return CompareValues(OffState);
        }

        public bool ValueNonZero()
        {
            bool result = false;

            if (!string.IsNullOrEmpty(Variable?.Value))
            {
                if (Conversion.IsNumber(Variable.Value, out double num))
                {
                    if (num != 0.0f)
                        result = true;
                }
                else
                    result = true;
            }

            return result;
        }

        protected bool CompareValues(string cmp)
        {
            if (!string.IsNullOrWhiteSpace(cmp) && !string.IsNullOrWhiteSpace(StringValue) && (cmp?.StartsWith('<') == true || cmp?.StartsWith('>') == true))
            {
                bool greater = cmp.Contains('>');
                bool withEquality = cmp.Contains(">=") || cmp.Contains("<=");
                cmp = cmp.Replace("=", "").Replace("<", "").Replace(">", "");

                double fa = Conversion.ToDouble(cmp, 0.0f);
                double fb = NumericValue;

                if (withEquality)
                {
                    if (greater)
                        return fb >= fa;
                    else
                        return fb <= fa;
                }
                else
                {
                    if (greater)
                        return fb > fa;
                    else
                        return fb < fa;
                }
            }
            else if (Conversion.IsNumber(cmp, out double numCmp) && IsNumericValue)
            {
                return numCmp == NumericValue;
            }
            else
            {
                return cmp == StringValue;
            }
        }

        protected bool CompareSequence()
        {
            if (!IsNumericValue)
                return false;

            string[] parts = Code.Replace("=", "").Replace("<", "").Split(',');
            foreach (string part in parts)
                if (Conversion.IsNumber(part, out double partNum) && partNum == NumericValue)
                    return true;

            return false;
        }

        public string GetSwitchValue(int ticks, bool keyUp = true)
        {
            string result = "";

            if (ParentCommand?.HoldSwitch == true)
            {
                if (keyUp)
                    result = OffState;
                else
                    result = OnState;
            }
            else if (ParentCommand?.ResetSwitch == true && !IsCode)
            {
                result = OnState;
            }
            else if (IsCode && IsNumericValue)
            {
                if (IsCounter)
                    result = Counter(ticks);
                else if (IsSequence)
                    result = Sequence();
                else
                    Logger.Warning($"Could not calculate a Value from Code '{OnState}'");
            }
            else if (ParentCommand?.ToggleSwitch == false)
                result = Toggle();

            if (Variable?.Type == SimValueType.BVAR && string.IsNullOrWhiteSpace(result))
                result = "1";

            return result;
        }

        protected string Toggle()
        {
            return ToolsValueState.Toggle(StringValue, OnState, OffState);
        }

        protected string Sequence()
        {
            return ToolsValueState.GetSequence(Code, NumericValue, StringValue, IsNumericValue);
        }

        protected string Counter(int ticks)
        {
            return ToolsValueState.GetCounter(Code, NumericValue, StringValue, ticks);
        }

        public static string GetValueMapped(string strValue, string strMap)
        {
            string result = strValue;
            if (!string.IsNullOrEmpty(strMap) && strMap.Contains('='))
            {
                var dict = GetValueMap(strMap);
                foreach (var mapping in dict)
                {
                    if (mapping.Key.Contains('<') || mapping.Key.Contains('>'))
                    {
                        bool greater = mapping.Key.Contains('>');
                        string key = mapping.Key.Replace(">", "").Replace("<", "");
                        double val = Conversion.ToDouble(strValue);
                        double limit = Conversion.ToDouble(key, 0);

                        if (greater)
                        {
                            if (limit >= val)
                            {
                                result = mapping.Value;
                                break;
                            }
                        }
                        else
                        {
                            if (limit <= val)
                            {
                                result = mapping.Value;
                                break;
                            }
                        }
                    }
                    else if (mapping.Key == strValue)
                    {
                        result = mapping.Value;
                        break;
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, string> GetValueMap(string valueMap)
        {
            var dict = new Dictionary<string, string>();

            string[] parts = valueMap.Split(':');

            string[] pair;
            foreach (var p in parts)
            {
                pair = p.Split('=');
                if (pair.Length == 2 && !dict.ContainsKey(pair[0]))
                    dict.Add(pair[0], pair[1]);
            }

            return dict;
        }
    }
}
