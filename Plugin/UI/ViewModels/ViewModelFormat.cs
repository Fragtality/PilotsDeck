using PilotsDeck.Actions.Advanced;
using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels.Element;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.UI.ViewModels
{
    public class ViewModelFormat(ValueFormat format, ViewModelAction action)
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ValueFormat Format { get; set; } = format;
        public DisplayValueType Preferrence { get { return Format.TypePreferrence; } }
        public bool DecodeBCD { get { return Format.DecodeBCD; } }
        public bool UseAbsoluteValue { get { return Format.UseAbsoluteValue; } }
        public string Scalar { get { return Conversion.ToString(Format.Scalar); } }
        public string Offset { get { return Conversion.ToString(Format.Offset); } }
        public bool OffsetFirst { get { return Format.OffsetFirst; } }
        public string Digits { get { return Conversion.ToString(Format.Digits); } }
        public string DigitsTrailing { get { return Conversion.ToString(Format.DigitsTrailing); } }
        public bool LimitDigits { get { return Format.LimitDigits; } }
        public bool InsertSign { get { return Format.InsertSign; } }
        public bool InsertSpace { get { return Format.InsertSpace; } }
        public string Round { get { return Conversion.ToString(Format.Round); } }
        public bool RoundFloor { get { return Format.RoundFloor; } }
        public bool RoundCeiling { get { return Format.RoundCeiling; } }
        public string SubIndex { get { return Conversion.ToString(Format.SubIndex); } }
        public string SubLength { get { return Conversion.ToString(Format.SubLength); } }
        public string FormatString { get { return Format.FormatString; } }
        public List<MappingListItem> ValueMappings
        {
            get
            {
                List<MappingListItem> mappings = [];

                foreach (var mapping in Format.Mappings)
                    mappings.Add(new() { Key = mapping.Key, Value = mapping.Value });

                return mappings;
            }
        }

        public Dictionary<string, string> GetDictionary()
        {
            return Format.Mappings.ToDictionary();
        }

        public void SetPreferrence(DisplayValueType input)
        {
            Format.TypePreferrence = input;
            ModelAction.UpdateAction();
        }

        public void SetBCD(bool value)
        {
            Format.DecodeBCD = value;
            ModelAction.UpdateAction();
        }

        public void SetAbsolute(bool value)
        {
            Format.UseAbsoluteValue = value;
            ModelAction.UpdateAction();
        }

        public void SetScalar(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Format.Scalar = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetOffset(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                Format.Offset = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetOffsetFirst(bool value)
        {
            Format.OffsetFirst = value;
            ModelAction.UpdateAction();
        }

        public void SetDigits(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                Format.Digits = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetDigitsTrailing(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                Format.DigitsTrailing = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetLimitDigits(bool value)
        {
            Format.LimitDigits = value;
            ModelAction.UpdateAction();
        }

        public void SetInsertSign(bool value)
        {
            Format.InsertSign = value;
            ModelAction.UpdateAction();
        }

        public void SetInsertSpace(bool value)
        {
            Format.InsertSpace = value;
            ModelAction.UpdateAction();
        }

        public void SetRound(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                Format.Round = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetRoundFloor(bool value)
        {
            Format.RoundFloor = value;
            if (value)
                Format.RoundCeiling = false;
            ModelAction.UpdateAction();
        }

        public void SetRoundCeiling(bool value)
        {
            Format.RoundCeiling = value;
            if (value)
                Format.RoundFloor = false;
            ModelAction.UpdateAction();
        }

        public void SetSubIndex(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                Format.SubIndex = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetSubLength(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                Format.SubLength = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetFormatString(string input)
        {
            Format.FormatString = input;
            ModelAction.UpdateAction();
        }

        public void CopyMappings(Dictionary<string, string> source)
        {
            if (source?.Count < 1)
                return;

            Format.Mappings.Clear();
            foreach (var mapping in source)
                Format.Mappings.TryAdd(mapping.Key, mapping.Value);
            ModelAction.UpdateAction();
        }

        public bool AddMapping(string value, string replace)
        {
            if (string.IsNullOrWhiteSpace(value) || replace == null)
                return false;

            if (Format.Mappings.TryAdd(value, replace))
            {
                ModelAction.UpdateAction();
                return true;
            }
            else
                return false;
        }

        public bool UpdateMapping(MappingListItem item, string value, string replace)
        {
            if (string.IsNullOrWhiteSpace(value) || replace == null || string.IsNullOrWhiteSpace(item?.Key))
                return false;

            if (Format.Mappings.Remove(item.Key))
            {
                Format.Mappings.TryAdd(value, replace);
                ModelAction.UpdateAction();
                return true;
            }
            else
                return false;
        }

        public bool RemoveMapping(string key)
        {
            if (Format.Mappings.Remove(key))
            {
                ModelAction.UpdateAction();
                return true;
            }
            else
                return false;
        }
    }
}
