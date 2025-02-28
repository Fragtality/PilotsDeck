namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelSizePos(ViewModelManipulator viewModel) : ViewModelManipulator(viewModel.Source, viewModel.ModelAction)
    {
        protected virtual ViewModelManipulator ParentModel { get; } = viewModel;

        protected override void InitializeMemberBindings()
        {
            CreateMemberNumberBinding<float>(nameof(ValueX));
            CreateMemberNumberBinding<float>(nameof(ValueY));
            CreateMemberNumberBinding<float>(nameof(ValueW));
            CreateMemberNumberBinding<float>(nameof(ValueH));
            CreateMemberNumberBinding<float>(nameof(SizePosMinValue));
            CreateMemberNumberBinding<float>(nameof(SizePosMaxValue));
        }

        public virtual bool ChangeSizePosDynamic { get => GetSourceValue<bool>(); set { SetModelValue<bool>(value); NotifyPropertyChanged(nameof(StaticSizePos)); ModelAction.NotifyTreeRefresh(); } }
        public virtual bool StaticSizePos => !ChangeSizePosDynamic;
        public override string Address { get => Source.SizePosAddress; set => SetModelValue<string>(value, null, null, nameof(Source.SizePosAddress)); }
        public virtual bool ChangeX { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool ChangeY { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool ChangeW { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool ChangeH { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual float ValueX { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float ValueY { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float ValueW { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float ValueH { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float SizePosMinValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float SizePosMaxValue { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
    }
}
