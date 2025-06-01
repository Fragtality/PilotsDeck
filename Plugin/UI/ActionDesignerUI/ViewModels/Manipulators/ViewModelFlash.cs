using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelFlash(ViewModelManipulator viewModel) : ViewModelManipulator(viewModel.Source, viewModel.ModelAction)
    {
        protected virtual ViewModelManipulator ParentModel { get; } = viewModel;

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<int, string>(nameof(FlashInterval), new RealInvariantConverter(), new ValidationRuleRange<int>(App.Configuration.IntervalDeckRefresh, 10 * 60 * 1000));
        }

        public virtual int FlashInterval { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
        public virtual bool FlashResetOnInteraction { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool FlashDoNotHideOnStop { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
    }
}
