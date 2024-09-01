using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.UI.ViewModels.Element
{
    public class MappingListItem
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public string Display { get { return $"{Key}={Value}"; } }
    }

    public class ViewModelElementValue(ElementValue element, ViewModelAction action)
    {
        public ViewModelAction ModelAction { get; set; } = action;
        public ElementValue ValueElement { get; set; } = element;
        public ManagedVariable Variable { get { return ValueElement.Variable; } }
        public ValueFormat Format { get; set; } = element.Settings.ValueFormat;
        public DisplayValueType Preferrence { get { return Format.TypePreferrence; } }
        public string Address { get { return ValueElement.Settings.ValueAddress; } }
        public bool IsValidAddress { get { return ValueElement?.Variable?.Type != SimValueType.NONE; } }
        public bool DecodeBCD { get { return Format.DecodeBCD; } }
        public bool UseAbsoluteValue { get { return Format.UseAbsoluteValue; } }
        public string Scalar { get { return Conversion.ToString(Format.Scalar); } }
        public string Offset { get { return Conversion.ToString(Format.Offset); } }
        public bool OffsetFirst { get { return Format.OffsetFirst; } }
        public string Digits { get { return Conversion.ToString(Format.Digits); } }
        public bool InsertSign { get { return Format.InsertSign; } }
        public string Round { get { return Conversion.ToString(Format.Round); } }
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

        public void SetAddress(string input)
        {
            ValueElement.Settings.ValueAddress = input;
            ModelAction.UpdateAction();
        }

        public void SetPreferrence(DisplayValueType input)
        {
            ValueElement.Settings.ValueFormat.TypePreferrence = input;
            ModelAction.UpdateAction();
        }

        public void SetBCD(bool value)
        {
            ValueElement.Settings.ValueFormat.DecodeBCD = value;
            ModelAction.UpdateAction();
        }

        public void SetScalar(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                ValueElement.Settings.ValueFormat.Scalar = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetOffset(string input)
        {
            if (Conversion.IsNumberF(input, out float num))
            {
                ValueElement.Settings.ValueFormat.Offset = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetOffsetFirst(bool value)
        {
            ValueElement.Settings.ValueFormat.OffsetFirst = value;
            ModelAction.UpdateAction();
        }

        public void SetDigits(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                ValueElement.Settings.ValueFormat.Digits = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetInsertSign(bool value)
        {
            ValueElement.Settings.ValueFormat.InsertSign = value;
            ModelAction.UpdateAction();
        }

        public void SetRound(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                ValueElement.Settings.ValueFormat.Round = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetSubIndex(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                ValueElement.Settings.ValueFormat.SubIndex = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetSubLength(string input)
        {
            if (Conversion.IsNumberI(input, out int num))
            {
                ValueElement.Settings.ValueFormat.SubLength = num;
                ModelAction.UpdateAction();
            }
        }

        public void SetFormatString(string input)
        {
            ValueElement.Settings.ValueFormat.FormatString = input;
            ModelAction.UpdateAction();
        }

        public void CopyMappings(Dictionary<string, string> source)
        {
            if (source?.Count < 1)
                return;

            ValueElement.Settings.ValueFormat.Mappings.Clear();
            foreach (var mapping in source)
                ValueElement.Settings.ValueFormat.Mappings.TryAdd(mapping.Key, mapping.Value);
            ModelAction.UpdateAction();
        }

        public bool AddMapping(string value, string replace)
        {
            if (string.IsNullOrWhiteSpace(value) || replace == null)
                return false;

            if (ValueElement.Settings.ValueFormat.Mappings.TryAdd(value, replace))
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

            if (ValueElement.Settings.ValueFormat.Mappings.Remove(item.Key))
            {
                ValueElement.Settings.ValueFormat.Mappings.TryAdd(value, replace);
                ModelAction.UpdateAction();
                return true;
            }
            else
                return false;
        }

        public bool RemoveMapping(string key)
        {
            if (ValueElement.Settings.ValueFormat.Mappings.Remove(key))
            {
                ModelAction.UpdateAction();
                return true;
            }
            else
                return false;
        }
    }
}
