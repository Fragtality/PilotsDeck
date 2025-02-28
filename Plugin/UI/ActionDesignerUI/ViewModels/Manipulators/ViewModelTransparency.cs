using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelTransparency(ViewModelManipulator viewModel) : ViewModelManipulator(viewModel.Source, viewModel.ModelAction)
    {
        protected virtual ViewModelManipulator ParentModel { get; } = viewModel;

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<float, string>(nameof(TransparencySetValue), new RealInvariantConverter("1"), new ValidationRuleRange<float>(0.0f, 1.0f));
            CreateMemberNumberBinding<float>(nameof(TransparencyMinValue));
            CreateMemberNumberBinding<float>(nameof(TransparencyMaxValue));
        }

        public virtual bool DynamicTransparency { get => GetSourceValue<bool>(); set { SetModelValue<bool>(value); NotifyPropertyChanged(nameof(StaticTransparency)); ModelAction.NotifyTreeRefresh(); } }
        public virtual bool StaticTransparency => !DynamicTransparency;
        public override string Address { get => Source.TransparencyAddress; set => SetModelValue<string>(value, null, null, nameof(Source.TransparencyAddress)); }
        public virtual float TransparencySetValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float TransparencyMinValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float TransparencyMaxValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
    }
}
