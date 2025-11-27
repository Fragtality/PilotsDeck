using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
{
    public partial class ViewModelFormat : ViewModelBaseExtension<ValueFormat>
    {
        public const int RangeNumMax = 15;
        public const int RangeStringMax = 1024;


        public ViewModelFormat(ValueFormat source, ViewModelAction modelAction) : base(source, modelAction)
        {
            ValueMappings = new(source.Mappings);
            SubscribeCollection(ValueMappings);


            CopyPasteInterface.BindProperty(nameof(ValueMappingsCopy), SettingType.VALUEMAP);
        }

        protected override void InitializeModel()
        {
            IsTypeString = Source.TypePreferrence == DisplayValueType.STRING;
            IsTypeNumber = Source.TypePreferrence == DisplayValueType.NUMBER;
        }

        protected override void InitializeMemberBindings()
        {
            CreateMemberNumberBinding<double>(nameof(Scalar), "1");
            CreateMemberNumberBinding<double>(nameof(Offset), "0");
            CreateMemberBinding<int, string>(nameof(Round), new RealInvariantConverter("-1"), new ValidationRuleRange<int>(-1, RangeNumMax));
            CreateMemberBinding<int, string>(nameof(Digits), new RealInvariantConverter("-1"), new ValidationRuleRange<int>(-1, RangeNumMax));
            CreateMemberBinding<int, string>(nameof(DigitsTrailing), new RealInvariantConverter("-1"), new ValidationRuleRange<int>(-1, RangeNumMax));
            CreateMemberBinding<int, string>(nameof(SubIndex), new RealInvariantConverter("-1"), new ValidationRuleRange<int>(-1, RangeStringMax));
            CreateMemberBinding<int, string>(nameof(SubLength), new RealInvariantConverter("0"), new ValidationRuleRange<int>(0, RangeStringMax));
            CreateMemberBinding<string, string>(nameof(FormatString), new NoneConverter(), new ValidationRuleFormatString());
        }

        [RelayCommand]
        protected virtual void SetPreferrence(object type)
        {
            SetModelEnum<DisplayValueType>(type, false, nameof(TypePreferrence));
        }

        protected virtual void SetPreferrenceType(DisplayValueType type)
        {
            SetModelValue<DisplayValueType>(type, null, null, nameof(Source.TypePreferrence));
            IsTypeString = type == DisplayValueType.STRING;
            IsTypeNumber = type == DisplayValueType.NUMBER;
        }

        [ObservableProperty]
        protected bool _IsTypeString = false;

        [ObservableProperty]
        protected bool _IsTypeNumber = false;

        public virtual DisplayValueType TypePreferrence { get => GetSourceValue<DisplayValueType>(); set => SetPreferrenceType(value); }
        public virtual bool DecodeBCD { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool UseAbsoluteValue { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual double Scalar { get => GetSourceValue<double>(); set => SetModelValue<double>(value); }
        public virtual double Offset { get => GetSourceValue<double>(); set => SetModelValue<double>(value); }
        public virtual bool OffsetFirst { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual int Digits { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
        public virtual int DigitsTrailing { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
        public virtual bool LimitDigits { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool InsertSign { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool InsertSpace { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual int Round { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
        public virtual bool RoundFloor { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool RoundCeiling { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual int SubIndex { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
        public virtual int SubLength { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
        public virtual string FormatString { get => GetSourceValue<string>(); set => SetModelValue<string>(value); }
        public virtual ViewModelMappings ValueMappings { get; }
        public virtual ICollection<KeyValuePair<string, string>> ValueMappingsCopy
        {
            get { return Source.Mappings.ToList() as ICollection<KeyValuePair<string, string>>; }
            set
            {
                CopyToModelList<KeyValuePair<string, string>>(value, null, nameof(Source.Mappings));
                NotifyPropertyChanged(nameof(ValueMappings));
                ValueMappings.NotifyCollectionChanged();
            }
        }

        public override string DisplayName => Name;
        public override string Name { get => ""; set { } }
    }

    public class ValidationRuleFormatString : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return BaseRule.Validate(() =>
            {
                return value is string text && (text.Contains(App.Configuration.StringReplace) || string.IsNullOrWhiteSpace(text));
            }
            , "Not a valid Format String!");
        }
    }

    public class ViewModelMappings(ICollection<KeyValuePair<string, string>> source) : ViewModelDictionary<string, string, string>(source, ValueConverter.Convert, (kv) => !string.IsNullOrWhiteSpace(kv.Key))
    {
        public static KeyValueStringConverter<string, string> ValueConverter { get; } = new KeyValueStringConverter<string, string>();

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<string, string>("Key", new NoneConverter(), new ValidationRuleNull());
            CreateMemberBinding<string, string>("Value", new NoneConverter(), new ValidationRuleNull());
        }

        public override KeyValuePair<string, string> BuildItemFromBindings()
        {
            if (!HasBindingErrors()
                && HasBinding("Key", out var bindingKey)
                && HasBinding("Value", out var bindingValue))
                return new(bindingKey.ConvertFromTarget<string>() ?? "", bindingValue.ConvertFromTarget<string>() ?? "");
            else
                return default;
        }
    }
}
