namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelRotate(ViewModelManipulator viewModel) : ViewModelManipulator(viewModel.Source, viewModel.ModelAction)
    {
        protected virtual ViewModelManipulator ParentModel { get; } = viewModel;

        protected override void InitializeMemberBindings()
        {
            CreateMemberNumberBinding<float>(nameof(RotateToValue));
            CreateMemberNumberBinding<float>(nameof(RotateMinValue));
            CreateMemberNumberBinding<float>(nameof(RotateMaxValue));
            CreateMemberNumberBinding<float>(nameof(RotateAngleStart));
            CreateMemberNumberBinding<float>(nameof(RotateAngleSweep));
        }

        public virtual bool RotateContinous { get => GetSourceValue<bool>(); set { SetModelValue<bool>(value); NotifyPropertyChanged(nameof(RotateStatic)); ModelAction.NotifyTreeRefresh(); } }
        public virtual bool RotateStatic => !RotateContinous;
        public override string Address { get => Source.RotateAddress; set => SetModelValue<string>(value, null, null, nameof(Source.RotateAddress)); }
        public virtual float RotateToValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float RotateMinValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float RotateMaxValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float RotateAngleStart { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float RotateAngleSweep { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
    }
}
