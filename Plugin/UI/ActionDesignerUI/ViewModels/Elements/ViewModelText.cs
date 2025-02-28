using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewModelText(ViewModelElement viewModel) : ViewModelElement(viewModel.Source, viewModel.ModelAction)
    {
        protected virtual ViewModelElement ParentModel { get; } = viewModel;

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<string, string>(nameof(Text), new NoneConverter(), new ValidationRuleNull());
        }

        public virtual string Text { get => GetSourceValue<string>(); set { SetModelValue<string>(value); ModelAction.NotifyTreeRefresh(); } }
    }
}
