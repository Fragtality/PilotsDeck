using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelVisible(ViewModelManipulator viewModel) : ViewModelManipulator(viewModel.Source, viewModel.ModelAction)
    {
        protected virtual ViewModelManipulator ParentModel { get; } = viewModel;

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<int, string>(nameof(ResetDelay), new RealInvariantConverter(), new ValidationRuleRange<int>(App.Configuration.IntervalDeckRefresh, 10 * 60 * 1000));
        }

        public virtual bool ResetVisibility { get => GetSourceValue<bool>(); set { SetModelValue<bool>(value); ModelAction.NotifyTreeRefresh(); } }
        public virtual int ResetDelay { get => GetSourceValue<int>(); set => SetModelValue<int>(value); }
    }
}
