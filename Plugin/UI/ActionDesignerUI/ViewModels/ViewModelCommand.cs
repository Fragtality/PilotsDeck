using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppTools;
using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Simulator;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
{
    public partial class ViewModelCommand(ModelCommand source, ViewModelAction parent) : ViewModelBaseExtension<ModelCommand>(source, parent), IModelAddress
    {
        public virtual StreamDeckCommand DeckCommandType => Source.DeckCommandType;
        public virtual ViewModelCommandAddress ModelAddress { get; protected set; }

        protected override void InitializeModel()
        {
            ModelAddress = new ViewModelCommandAddress(this, "Command Address");
        }

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<int, string>(nameof(TimeAfterLastDown), new RealInvariantConverter(), new ValidationRuleRange<int>(0, 15 * 1000));
            CreateMemberBinding<int, string>(nameof(TickDelay), new RealInvariantConverter(), new ValidationRuleRange<int>(0, 1000));
            CreateMemberBinding<int, string>(nameof(ResetDelay), new RealInvariantConverter(), new ValidationRuleRange<int>(0, 15 * 1000));
            CreateMemberBinding<int, string>(nameof(CommandDelay), new RealInvariantConverter(), new ValidationRuleRange<int>(0, 1000));
            CreateMemberBinding<string, string>(nameof(WriteValue), new NoneConverter(), new ValidationRuleNull());
            CreateMemberBinding<string, string>(nameof(ResetValue), new NoneConverter(), new ValidationRuleNull());
            CreateMemberBinding<string, string>(nameof(Name), new NoneConverter(), new ValidationRuleNull());
        }

        protected virtual string SelectName()
        {
            return string.IsNullOrWhiteSpace(Source.Name) ? $"{CommandType}: {Source.Address.Compact()}" : Source.Name;
        }

        protected virtual void NotifyTypeChange()
        {
            NotifyPropertyChanged(nameof(Address));
            NotifyPropertyChanged(nameof(IsBvar));
            NotifyPropertyChanged(nameof(IsResettable));
            NotifyPropertyChanged(nameof(HasCommandDelay));
            NotifyPropertyChanged(nameof(IsValueType));
            ModelAddress.NotifyAddressChange();
        }

        public virtual SimCommandType CommandType { get => GetSourceValue<SimCommandType>(); set { SetModelValue(value); NotifyTypeChange(); } }
        public virtual Dictionary<SimCommandType, string> CommandTypes => ViewModelHelper.GetSimTypes();
        public virtual bool IsBvar => CommandType == SimCommandType.BVAR;
        public virtual bool DoNotRequestBvar { get => GetSourceValue<bool>(); set { SetModelValue(value); NotifyTypeChange(); } }
        public virtual bool CanLongPress => Source.DeckCommandType == StreamDeckCommand.KEY_UP || Source.DeckCommandType == StreamDeckCommand.DIAL_UP;
        public virtual int TimeAfterLastDown { get => GetSourceValue<int>(); set => SetModelValue(value); }
        public virtual bool IsRotary => Source.DeckCommandType == StreamDeckCommand.DIAL_LEFT || Source.DeckCommandType == StreamDeckCommand.DIAL_RIGHT;
        public virtual int TickDelay { get => GetSourceValue<int>(); set => SetModelValue(value); }
        public virtual bool IsResettable => SimCommand.IsResetableValue(Source.CommandType, Source.DoNotRequestBvar);
        public virtual bool ResetSwitch { get => GetSourceValue<bool>(); set => SetModelValue(value); }
        public virtual int ResetDelay { get => GetSourceValue<int>(); set => SetModelValue(value); }
        public virtual bool HasCommandDelay => SimCommand.CommandTypeUsesDelay(Source.CommandType, Source.DoNotRequestBvar);
        public virtual bool UseCommandDelay { get => GetSourceValue<bool>(); set => SetModelValue(value); }
        public virtual int CommandDelay { get => GetSourceValue<int>(); set => SetModelValue(value); }
        public virtual bool IsValueType => SimCommand.IsValueCommand(Source.CommandType, Source.DoNotRequestBvar);
        public virtual string WriteValue { get => GetSourceValue<string>(); set => SetModelValue(value); }
        public virtual string ResetValue { get => GetSourceValue<string>(); set => SetModelValue(value); }
        public virtual bool AnyCondition { get => GetSourceValue<bool>(); set => SetModelValue(value); }

        public override string DisplayName { get => SelectName(); }
        public override string Name { get => GetSourceValue<string>(); set { SetModelValue(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual string Address { get => GetSourceValue<string>(); set { SetModelValue(value); ModelAction.NotifyTreeRefresh(); } }
    }
}
