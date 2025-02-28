using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelManipulator(ModelManipulator source, ViewModelAction modelAction) : ViewModelBaseExtension<ModelManipulator>(source, modelAction), IModelAddress
    {
        public virtual ELEMENT_MANIPULATOR ManipulatorType { get => Source.ManipulatorType; }

        protected override void InitializeModel()
        {
            CreateMemberBinding<string, string>(nameof(Name), new NoneConverter(), new ValidationRuleNull());
        }

        public virtual string SelectName()
        {
            if (!string.IsNullOrWhiteSpace(Source.Name))
                return Source.Name;
            else if (ManipulatorType == ELEMENT_MANIPULATOR.INDICATOR)
                return $"Indicator ({ViewModelHelper.IndicatorTypes[Source.IndicatorType]})";
            else if (ManipulatorType == ELEMENT_MANIPULATOR.VISIBLE)
                return $"{ViewModelHelper.ManipulatorTypes[Source.ManipulatorType]}{(Source.ResetVisibility ? " (Reset)" : "")}";
            else if (ManipulatorType == ELEMENT_MANIPULATOR.ROTATE)
                return $"{ViewModelHelper.ManipulatorTypes[Source.ManipulatorType]}{(Source.RotateContinous ? " (Cont.)" : "")}";
            else if (ManipulatorType == ELEMENT_MANIPULATOR.TRANSPARENCY)
                return $"{ViewModelHelper.ManipulatorTypes[Source.ManipulatorType]}{(Source.DynamicTransparency ? " (Dyn.)" : "")}";
            else if (ManipulatorType == ELEMENT_MANIPULATOR.SIZEPOS)
                return $"{ViewModelHelper.ManipulatorTypes[Source.ManipulatorType]}{(Source.ChangeSizePosDynamic ? " (Dyn.)" : "")}";
            else if (ManipulatorType == ELEMENT_MANIPULATOR.COLOR)
                return $"{ViewModelHelper.ManipulatorTypes[Source.ManipulatorType]} ({Source.Color})";
            else
                return ViewModelHelper.ManipulatorTypes[Source.ManipulatorType];
        }

        public virtual bool AnyCondition { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }

        
        public override string DisplayName { get => SelectName(); }
        public override string Name { get => GetSourceValue<string>(); set { SetModelValue<string>(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual string Address { get => ""; set { } }
    }
}
