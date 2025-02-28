using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.Actions;
using PilotsDeck.Tools;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Manipulators
{
    public partial class ViewModelIndicator : ViewModelManipulator
    {
        protected virtual ViewModelManipulator ParentModel { get; }
        public virtual CommandWrapper SetImageCommand { get; }
        public virtual RelayCommand IncreaseLineCommand { get; }
        public virtual RelayCommand DecreaseLineCommand { get; }
        public virtual RelayCommand IncreaseSizeCommand { get; }
        public virtual RelayCommand DecreaseSizeCommand { get; }
        public virtual RelayCommand IncreaseOffsetCommand { get; }
        public virtual RelayCommand DecreaseOffsetCommand { get; }

        public ViewModelIndicator(ViewModelManipulator viewModel) : base(viewModel.Source, viewModel.ModelAction)
        {
            ParentModel = viewModel;

            SetImageCommand = new(ShowImageDialog);

            IncreaseLineCommand = new RelayCommand(() => StepModelProperty<float>(1, [0.1f, 255.0f], null, nameof(IndicatorLineSize)));
            DecreaseLineCommand = new RelayCommand(() => StepModelProperty<float>(-1, [0.1f, 255.0f], null, nameof(IndicatorLineSize)));

            IncreaseSizeCommand = new RelayCommand(() => StepModelProperty<float>(1, [0.1f, 255.0f], null, nameof(IndicatorSize)));
            DecreaseSizeCommand = new RelayCommand(() => StepModelProperty<float>(-1, [0.1f, 255.0f], null, nameof(IndicatorSize)));

            IncreaseOffsetCommand = new RelayCommand(() => StepModelProperty<float>(1, [], null, nameof(IndicatorOffset)));
            DecreaseOffsetCommand = new RelayCommand(() => StepModelProperty<float>(-1, [], null, nameof(IndicatorOffset)));
        }

        protected override void InitializeModel()
        {
            CopyPasteInterface.BindProperty(nameof(IndicatorColor), SettingType.COLOR);
        }

        protected override void InitializeMemberBindings()
        {
            CreateMemberNumberBinding<float>(nameof(IndicatorScale));
            CreateMemberBinding<float, string>(nameof(IndicatorLineSize), new RealInvariantConverter("1"), new ValidationRuleRange<float>(0.1f, 255.0f));
            CreateMemberBinding<float, string>(nameof(IndicatorSize), new RealInvariantConverter("20"), new ValidationRuleRange<float>(0.1f, 255.0f));
            CreateMemberNumberBinding<float>(nameof(IndicatorOffset));
        }

        public virtual float IndicatorScale { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual bool IndicatorReverse { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual Dictionary<IndicatorType, string> IndicatorTypes => ViewModelHelper.IndicatorTypes;
        public virtual IndicatorType IndicatorType { get => GetSourceValue<IndicatorType>(); set { SetModelValue<IndicatorType>(value); NotifyTypeChange(); } }
        public virtual bool IsImage => IndicatorType == IndicatorType.IMAGE;
        public virtual bool HasColor => IndicatorType != IndicatorType.IMAGE;
        public virtual Color IndicatorColor { get => Source.GetIndicatorColor(); set => SetIndicatorColor(value); }
        public virtual string HtmlColor => Source.IndicatorColor;
        public virtual bool CanFlip => IndicatorType == IndicatorType.IMAGE || IndicatorType == IndicatorType.TRIANGLE;
        public virtual bool IndicatorFlip { get => GetSourceValue<bool>(); set => SetModelValue<bool>(value); }
        public virtual bool HasLineSize => IndicatorType == IndicatorType.CIRCLE || IndicatorType == IndicatorType.LINE;
        public virtual float IndicatorLineSize { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float IndicatorSize { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual float IndicatorOffset { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public override string Address { get => Source.IndicatorAddress; set => SetModelValue<string>(value, null, null, nameof(Source.IndicatorAddress)); }

        protected virtual void NotifyTypeChange()
        {
            NotifyPropertyChanged(nameof(IsImage));
            NotifyPropertyChanged(nameof(HasColor));
            NotifyPropertyChanged(nameof(CanFlip));
            NotifyPropertyChanged(nameof(HasLineSize));
            ModelAction.NotifyTreeRefresh();
        }

        protected virtual void SetIndicatorColor(Color color)
        {
            Source.SetIndicatorColor(color);
            UpdateAction();
            OnPropertyChanged(nameof(IndicatorColor));
            OnPropertyChanged(nameof(HtmlColor));
        }

        public virtual string ImageFile => GetSourceValue<string>(nameof(Source.IndicatorImage));
        public virtual BitmapImage ImageSource
        {
            get
            {
                var img = Img.GetBitmapFromFile(Source.IndicatorImage) ?? Img.GetBitmapFromFile(AppConfiguration.WaitImage);
                return img;
            }
        }

        protected virtual void ShowImageDialog()
        {
            DialogImage dialog = new(Source.IndicatorImage ?? "", ModelAction.WindowInstance);
            if (dialog.ShowDialog() == true)
            {
                Source.IndicatorImage = dialog.ImageResult;
                UpdateAction();
                OnPropertyChanged(nameof(ImageSource));
                OnPropertyChanged(nameof(ImageFile));
            }
        }
    }
}
