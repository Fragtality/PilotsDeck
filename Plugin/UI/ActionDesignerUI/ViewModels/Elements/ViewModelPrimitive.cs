using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions.Advanced.SettingsModel;
using System.Collections.Generic;
using System.Windows;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewModelPrimitive : ViewModelElement
    {
        protected virtual ViewModelElement ParentModel { get; }

        public ViewModelPrimitive(ViewModelElement viewModel) : base(viewModel.Source, viewModel.ModelAction)
        {
            ParentModel = viewModel;
            IncreaseCommand = new RelayCommand<string>((propertyName) => StepModelProperty<float>(1, [0.1f, 255.0f], null, propertyName));
            DecreaseCommand = new RelayCommand<string>((propertyName) => StepModelProperty<float>(-1, [0.1f, 255.0f], null, propertyName));
        }

        protected override void InitializeMemberBindings()
        {
            CreateMemberBinding<float, string>(nameof(LineSize), new RealInvariantConverter("1"), new ValidationRuleRange<float>(0.1f, 255.0f));
        }

        public virtual bool PrimitiveUsesLineSize()
        {
            return Source.PrimitiveType == PrimitiveType.LINE ||
                   Source.PrimitiveType == PrimitiveType.RECTANGLE ||
                   Source.PrimitiveType == PrimitiveType.CIRCLE;
        }

        public virtual PrimitiveType PrimitiveType
        {
            get { return GetSourceValue<PrimitiveType>(); }
            set
            { 
                SetModelValue<PrimitiveType>(value);
                NotifyPropertyChanged(nameof(VisibilityLineSize));
                ParentModel.NotifyPropertyChanged(nameof(VisibilityPosition));
                ParentModel.NotifyPropertyChanged(nameof(VisibilityStartEnd));
                ModelAction.NotifyTreeRefresh();
            }
        }

        public virtual Dictionary<PrimitiveType, string> PrimitiveTypes { get; } = ViewModelHelper.PrimitiveTypes;

        public virtual Visibility VisibilityLineSize { get => PrimitiveUsesLineSize() ? Visibility.Visible : Visibility.Collapsed; }

        public virtual float LineSize { get => GetSourceValue<float>(); set { SetModelValue<float>(value); } }
    }
}
