using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppTools;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using System.Collections.Generic;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels
{
    public partial class ViewModelCondition(ConditionHandler source, ViewModelAction parent) : ViewModelBaseExtension<ConditionHandler>(source, parent), IModelAddress
    {
        protected override void InitializeModel()
        {
            CreateMemberBinding<string, string>(nameof(Value), new NoneConverter(), new ValidationRuleNull());
            CreateMemberBinding<string, string>(nameof(Name), new NoneConverter(), new ValidationRuleNull());            
        }

        public virtual Dictionary<Comparison, string> ComparisonTypes => ViewModelHelper.ComparisonTypes;
        public virtual Comparison Comparison { get => GetSourceValue<Comparison>(); set { SetModelValue<Comparison>(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual string Value { get => GetSourceValue<string>(); set { SetModelValue<string>(value); ModelAction.NotifyTreeRefresh(); } }

        public virtual string SelectName()
        {
            if (!string.IsNullOrWhiteSpace(Source.Name))
                return Source.Name;
            else
                return $"'{Source.Address.Compact()}' {ViewModelHelper.ComparisonTypes[Source.Comparison]} '{Source.Value}'";
        }

        public override string DisplayName { get => SelectName(); }
        public override string Name { get => GetSourceValue<string>(); set { SetModelValue<string>(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual string Address { get => GetSourceValue<string>(); set { SetModelValue<string>(value); ModelAction.NotifyTreeRefresh(); } }
    }
}
