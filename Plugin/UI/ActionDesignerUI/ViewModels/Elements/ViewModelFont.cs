using CFIT.AppFramework.UI.ViewModels.Commands;
using CommunityToolkit.Mvvm.Input;
using PilotsDeck.UI.ActionDesignerUI.Clipboard;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.ViewModels.Elements
{
    public partial class ViewModelFont : ViewModelElement
    {
        protected virtual ViewModelElement ParentModel { get; }

        public virtual CommandWrapper IncreaseFontSizeCommand { get; }
        public virtual CommandWrapper DecreaseFontSizeCommand { get; }
        public virtual CommandWrapper SelectFontCommand { get; }
        public virtual float StepFontSize { get; } = 1.0f;

        public ViewModelFont(ViewModelElement viewModel) : base(viewModel.Source, viewModel.ModelAction)
        {
            ParentModel = viewModel;

            IncreaseFontSizeCommand = new(() =>
            {
                StepModelProperty<float>(StepFontSize, [], (value) => MathF.Round(value, 1), nameof(FontSize));
                NotifyFontNotification();
            });
            DecreaseFontSizeCommand = new(() =>
            {
                StepModelProperty<float>(StepFontSize * -1.0f, [], (value) => MathF.Round(value, 1), nameof(FontSize));
                NotifyFontNotification();
            });

            SelectFontCommand = new(() => SelectFont());
            ParentModel.SubscribeProperty(nameof(ParentModel.Color), () => NotifyFontNotification());

            CopyPasteInterface.BindProperty(nameof(FontSettings), SettingType.FONT);
        }

        protected virtual void NotifyAlignmentNotification()
        {
            OnPropertyChanged(nameof(ThicknessHorizontalLeft));
            OnPropertyChanged(nameof(ThicknessHorizontalCenter));
            OnPropertyChanged(nameof(ThicknessHorizontalRight));
            OnPropertyChanged(nameof(BorderBrushHorizontalLeft));
            OnPropertyChanged(nameof(BorderBrushHorizontalCenter));
            OnPropertyChanged(nameof(BorderBrushHorizontalRight));

            OnPropertyChanged(nameof(ThicknessVerticalTop));
            OnPropertyChanged(nameof(ThicknessVerticalCenter));
            OnPropertyChanged(nameof(ThicknessVerticalBottom));
            OnPropertyChanged(nameof(BorderBrushVerticalTop));
            OnPropertyChanged(nameof(BorderBrushVerticalCenter));
            OnPropertyChanged(nameof(BorderBrushVerticalBottom));
        }

        protected virtual void NotifyFontNotification()
        {
            OnPropertyChanged(nameof(Font));
            OnPropertyChanged(nameof(FontName));
            OnPropertyChanged(nameof(FontSize));
            OnPropertyChanged(nameof(FontStyle));
            OnPropertyChanged(nameof(FontInfo));
        }

        [RelayCommand]
        protected virtual void SetHorizontalAlignment(object parameter)
        {
            SetModelEnum<StringAlignment>(parameter, false, nameof(TextHorizontalAlignment));
        }

        [RelayCommand]
        protected virtual void SetVerticalAlignment(object parameter)
        {
            SetModelEnum<StringAlignment>(parameter, false, nameof(TextVerticalAlignment));
        }

        protected virtual void SelectFont()
        {
            System.Windows.Forms.FontDialog fontDialog = new()
            {
                Font = Font,
                FontMustExist = true
            };

            if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Source.SetFont(fontDialog.Font);
                UpdateAction();

                NotifyFontNotification();
            }
        }

        protected virtual FontSetting GetFontSettings()
        {
            return new FontSetting()
            {
                Font = Font,
                HorizontalAlignment = Source.TextHorizontalAlignment,
                VerticalAlignment = Source.TextVerticalAlignment,
            };
        }

        protected virtual void SetFontSettings(FontSetting setting)
        {
            if (setting == null)
                return;

            Source.SetFont(setting.Font);
            Source.TextHorizontalAlignment = setting.HorizontalAlignment;
            Source.TextVerticalAlignment = setting.VerticalAlignment;

            NotifyFontNotification();
            NotifyAlignmentNotification();
        }

        public virtual FontSetting FontSettings { get => GetFontSettings(); set => SetFontSettings(value); }
        public virtual System.Drawing.Font Font { get => Source.GetFont(); set { Source.SetFont(value); UpdateAction(); } }
        public virtual string FontName { get => GetSourceValue<string>(); set => SetModelValue<string>(value); }
        public virtual float FontSize { get => GetSourceValue<float>(); set => SetModelValue<float>(value); }
        public virtual System.Drawing.FontStyle FontStyle { get => GetSourceValue<System.Drawing.FontStyle>(); set => SetModelValue<System.Drawing.FontStyle>(value); }
        public virtual StringAlignment TextHorizontalAlignment { get => GetSourceValue<StringAlignment>(); set { SetModelValue<StringAlignment>(value); NotifyAlignmentNotification(); } }
        public virtual StringAlignment TextVerticalAlignment { get => GetSourceValue<StringAlignment>(); set { SetModelValue<StringAlignment>(value); NotifyAlignmentNotification(); } }
        public virtual string FontInfo { get => $"{Source.FontName}, {Source.FontSize:F0}, {Source.FontStyle}"; }
        public virtual System.Windows.Media.FontFamily FontFamily { get => new(Source.FontName); }

        public virtual Thickness DefaultThicknessSelected { get; } = new(1.5);
        public virtual Thickness DefaultThicknessUnselected { get; } = new(1);
        public virtual SolidColorBrush DefaultBrushSelected { get; } = System.Windows.SystemColors.HighlightBrush;
        public virtual SolidColorBrush DefaultBrushUnselected { get; } = System.Windows.SystemColors.WindowFrameBrush;

        public virtual bool IsHorizontalLeft { get => Source.TextHorizontalAlignment == StringAlignment.Near; }
        public virtual bool IsHorizontalCenter { get => Source.TextHorizontalAlignment == StringAlignment.Center; }
        public virtual bool IsHorizontalRight { get => Source.TextHorizontalAlignment == StringAlignment.Far; }
        public virtual Thickness ThicknessHorizontalLeft { get => IsHorizontalLeft ? DefaultThicknessSelected : DefaultThicknessUnselected; }
        public virtual Thickness ThicknessHorizontalCenter { get => IsHorizontalCenter ? DefaultThicknessSelected : DefaultThicknessUnselected; }
        public virtual Thickness ThicknessHorizontalRight { get => IsHorizontalRight ? DefaultThicknessSelected : DefaultThicknessUnselected; }
        public virtual SolidColorBrush BorderBrushHorizontalLeft { get => IsHorizontalLeft ? DefaultBrushSelected : DefaultBrushUnselected; }
        public virtual SolidColorBrush BorderBrushHorizontalCenter { get => IsHorizontalCenter ? DefaultBrushSelected : DefaultBrushUnselected; }
        public virtual SolidColorBrush BorderBrushHorizontalRight { get => IsHorizontalRight ? DefaultBrushSelected : DefaultBrushUnselected; }

        public virtual bool IsVerticalTop { get => Source.TextVerticalAlignment == StringAlignment.Near; }
        public virtual bool IsVerticalCenter { get => Source.TextVerticalAlignment == StringAlignment.Center; }
        public virtual bool IsVerticalBottom { get => Source.TextVerticalAlignment == StringAlignment.Far; }
        public virtual Thickness ThicknessVerticalTop { get => IsVerticalTop ? DefaultThicknessSelected : DefaultThicknessUnselected; }
        public virtual Thickness ThicknessVerticalCenter { get => IsVerticalCenter ? DefaultThicknessSelected : DefaultThicknessUnselected; }
        public virtual Thickness ThicknessVerticalBottom { get => IsVerticalBottom ? DefaultThicknessSelected : DefaultThicknessUnselected; }
        public virtual SolidColorBrush BorderBrushVerticalTop { get => IsVerticalTop ? DefaultBrushSelected : DefaultBrushUnselected; }
        public virtual SolidColorBrush BorderBrushVerticalCenter { get => IsVerticalCenter ? DefaultBrushSelected : DefaultBrushUnselected; }
        public virtual SolidColorBrush BorderBrushVerticalBottom { get => IsVerticalBottom ? DefaultBrushSelected : DefaultBrushUnselected; }
    }
}
