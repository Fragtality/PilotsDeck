using PilotsDeck.Actions;
using PilotsDeck.Actions.Advanced.Manipulators;
using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels;
using PilotsDeck.UI.ViewModels.Manipulator;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI.ControlsManipulator
{
    public partial class ViewIndicator : UserControl
    {
        public ViewModelManipulatorIndicator ModelIndicator { get; set; }
        public Window ParentWindow { get; set; }
        protected bool Refreshing { get; set; } = false;
        protected DispatcherTimer RefreshTimer { get; set; }

        public ViewIndicator(ViewModelManipulatorIndicator model, Window parent)
        {
            InitializeComponent();
            ModelIndicator = model;
            ParentWindow = parent;
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeControls()
        {
            Refreshing = true;

            InputAddress.Text = ModelIndicator.IndicatorAddress;
            InputAddress.BorderBrush = new SolidColorBrush(ModelIndicator.IsValidAddress ? Colors.Green : Colors.Red);
            InputAddress.BorderThickness = new Thickness(1.5);
            LabelVariable.Content = $"Type: {(ModelIndicator.Manipulator as ManipulatorIndicator)?.IndicatorVariable?.Type} | Current Value: {(ModelIndicator.Manipulator as ManipulatorIndicator)?.IndicatorVariable?.Value}";
            InputScale.Text = ModelIndicator.IndicatorScale;

            if (ModelIndicator.IndicatorType == IndicatorType.IMAGE)
            {
                LabelImage.Visibility = Visibility.Visible;
                InputImage.Visibility = Visibility.Visible;
                GridImage.Visibility = Visibility.Visible;
                LabelColor.Visibility = Visibility.Collapsed;
                LabelColorSelect.Visibility = Visibility.Collapsed;
                var bitmap = ModelIndicator.ImageSource;
                if (bitmap != null)
                {
                    if (bitmap.Width > 36)
                        InputImage.Width = 36;
                    InputImage.Source = bitmap;
                }
                LabelImageFile.Content = ModelIndicator.IndicatorImage;
                ButtonIndicatorColorClipboard.Visibility = Visibility.Collapsed;
            }
            else
            {
                LabelImage.Visibility = Visibility.Collapsed;
                InputImage.Visibility = Visibility.Collapsed;
                GridImage.Visibility = Visibility.Collapsed;
                LabelColor.Visibility = Visibility.Visible;
                LabelColorSelect.Visibility = Visibility.Visible;
                ButtonIndicatorColorClipboard.Visibility = Visibility.Visible;
                LabelColorSelect.Background = new SolidColorBrush(ModelIndicator.IndicatorColor);
                SettingItem.SetButton(ButtonIndicatorColorClipboard, SettingType.COLOR);
            }

            if (ModelIndicator.IndicatorType == IndicatorType.IMAGE || ModelIndicator.IndicatorType == IndicatorType.TRIANGLE)
            {
                LabelFlip.Visibility = Visibility.Visible;
                CheckFlip.Visibility = Visibility.Visible;
                CheckFlip.IsChecked = ModelIndicator.IndicatorFlip;
            }
            else
            {
                LabelFlip.Visibility = Visibility.Collapsed;
                CheckFlip.Visibility = Visibility.Collapsed;
            }

            if (ModelIndicator.IndicatorType == IndicatorType.CIRCLE || ModelIndicator.IndicatorType == IndicatorType.LINE)
            {
                LabelLineSize.Visibility = Visibility.Visible;
                GridLineSize.Visibility = Visibility.Visible;
                InputLineSize.Text = ModelIndicator.IndicatorLineSize;
            }
            else
            {
                LabelLineSize.Visibility = Visibility.Collapsed;
                GridLineSize.Visibility = Visibility.Collapsed;
            }

            InputSize.Text = ModelIndicator.IndicatorSize;
            InputOffset.Text = ModelIndicator.IndicatorOffset;
            CheckReverseDirection.IsChecked = ModelIndicator.IndicatorReverse;

            Refreshing = false;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refreshing = true;
            ViewModel.SetComboBox(ComboType, ViewModel.GetIndicatorTypes(), ModelIndicator.IndicatorType);
            Refreshing = false;

            RefreshTimer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            RefreshTimer?.Stop();
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
                RefreshTimer.Stop();

            LabelVariable.Content = $"Type: {(ModelIndicator.Manipulator as ManipulatorIndicator)?.IndicatorVariable?.Type} | Current Value: {(ModelIndicator.Manipulator as ManipulatorIndicator)?.IndicatorVariable?.Value}";
            if (ModelIndicator.IndicatorType != IndicatorType.IMAGE)
                SettingItem.SetButton(ButtonIndicatorColorClipboard, SettingType.COLOR);
        }

        private void InputAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetAddress(InputAddress.Text);
        }

        private void InputAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelIndicator.SetAddress(InputAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputAddress);
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboType.SelectedValue is IndicatorType type)
                ModelIndicator.SetType(type);
        }

        private void InputImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DialogImage dialog = new(ModelIndicator.IndicatorImage, ParentWindow);
            if (dialog.ShowDialog() == true)
                ModelIndicator.SetImage(dialog.ImageResult);
        }

        private void LabelColorSelect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new()
            {
                Color = ModelIndicator.ColorForms,
                CustomColors = ColorStore.ColorArray
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ColorStore.AddDialogColors(colorDialog.CustomColors, colorDialog.Color);
                ModelIndicator.SetColor(colorDialog.Color);
            }
        }

        private void InputSize_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetSize(InputSize.Text);
        }

        private void InputSize_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelIndicator.SetSize(InputSize.Text);
        }

        private void InputOffset_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetOffset(InputOffset.Text);
        }

        private void InputOffset_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelIndicator.SetOffset(InputOffset.Text);
        }

        private void CheckReverseDirection_Click(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetReverse(CheckReverseDirection.IsChecked == true);
        }

        private void CheckFlip_Click(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetFlip(CheckFlip.IsChecked == true);
        }

        private void ButtonIndicatorColorClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (ModelIndicator.IndicatorType != IndicatorType.IMAGE)
                SettingItem.Clipboard_Click(SettingType.COLOR, ButtonIndicatorColorClipboard, ModelIndicator.SetColor, ModelIndicator.Manipulator.Settings.GetIndicatorColor);
            else
                SettingItem.SetButton(ButtonIndicatorColorClipboard, SettingType.COLOR);
        }

        private void InputLineSize_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetLineSize(InputLineSize.Text);
        }

        private void InputLineSize_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelIndicator.SetLineSize(InputLineSize.Text);
        }

        private void InputScale_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelIndicator.SetScale(InputScale.Text);
        }

        private void InputScale_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelIndicator.SetScale(InputScale.Text);
        }
    }
}
