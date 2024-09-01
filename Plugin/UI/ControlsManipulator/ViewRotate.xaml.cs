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
    public partial class ViewRotate : UserControl
    {
        public ViewModelManipulatorRotate ModelManipulator { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }
        public ViewRotate(ViewModelManipulatorRotate model)
        {
            InitializeComponent();
            ModelManipulator = model;
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeControls()
        {
            CheckRotateContinous.IsChecked = ModelManipulator.RotateContinous;
            if (ModelManipulator.RotateContinous)
            {
                LabelRotateTo.Visibility = Visibility.Collapsed;
                InputRotateToValue.Visibility = Visibility.Collapsed;
                LabelRotateAddress.Visibility = Visibility.Visible;
                InputRotateAddress.Visibility = Visibility.Visible;
                LabelRotateMin.Visibility = Visibility.Visible;
                InputRotateMinValue.Visibility = Visibility.Visible;
                LabelRotateMax.Visibility = Visibility.Visible;
                InputRotateMaxValue.Visibility = Visibility.Visible;
                LabelRotateAngleStart.Visibility = Visibility.Visible;
                InputRotateAngleStart.Visibility = Visibility.Visible;
                LabelRotateAngleSweep.Visibility = Visibility.Visible;
                InputRotateAngleSweep.Visibility = Visibility.Visible;


                InputRotateAddress.Text = ModelManipulator.RotateAddress;
                InputRotateAddress.BorderBrush = new SolidColorBrush(ModelManipulator.IsValidRotateAddress ? Colors.Green : Colors.Red);
                InputRotateAddress.BorderThickness = new Thickness(1.5);
                LabelRotateVariable.Visibility = Visibility.Visible;
                LabelSyntaxCheck.Visibility = Visibility.Visible;
                InputRotateMinValue.Text = ModelManipulator.RotateMinValue;
                InputRotateMaxValue.Text = ModelManipulator.RotateMaxValue;
                InputRotateAngleStart.Text = ModelManipulator.RotateAngleStart;
                InputRotateAngleSweep.Text = ModelManipulator.RotateAngleSweep;
            }
            else
            {
                LabelRotateTo.Visibility = Visibility.Visible;
                InputRotateToValue.Visibility = Visibility.Visible;
                LabelRotateAddress.Visibility = Visibility.Collapsed;
                InputRotateAddress.Visibility = Visibility.Collapsed;
                LabelSyntaxCheck.Visibility = Visibility.Collapsed;
                LabelRotateMin.Visibility = Visibility.Collapsed;
                InputRotateMinValue.Visibility = Visibility.Collapsed;
                LabelRotateMax.Visibility = Visibility.Collapsed;
                InputRotateMaxValue.Visibility = Visibility.Collapsed;
                LabelRotateVariable.Visibility = Visibility.Collapsed;
                LabelRotateAngleStart.Visibility = Visibility.Collapsed;
                InputRotateAngleStart.Visibility = Visibility.Collapsed;
                LabelRotateAngleSweep.Visibility = Visibility.Collapsed;
                InputRotateAngleSweep.Visibility = Visibility.Collapsed;

                InputRotateToValue.Text = ModelManipulator.RotateToValue;
            }

            if (ModelManipulator.RotateContinous)
                LabelRotateVariable.Content = $"Type: {ModelManipulator.RotateVariable?.Type} | Current Value: {ModelManipulator.RotateVariable?.Value}";
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
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

            if (ModelManipulator.RotateContinous)
                LabelRotateVariable.Content = $"Type: {ModelManipulator.RotateVariable?.Type} | Current Value: {ModelManipulator.RotateVariable?.Value}";
        }

        private void InputRotateToValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateToValue(InputRotateToValue.Text);
        }

        private void InputRotateToValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetRotateToValue(InputRotateToValue.Text);
        }

        private void InputRotateAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateAddress(InputRotateAddress.Text);
        }

        private void InputRotateAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetRotateAddress(InputRotateAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputRotateAddress);
        }

        private void CheckRotateContinous_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateContinous(CheckRotateContinous.IsChecked == true);
        }

        private void InputRotateMinValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateMinValue(InputRotateMinValue.Text);
        }

        private void InputRotateMinValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetRotateMinValue(InputRotateMinValue.Text);
        }

        private void InputRotateMaxValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateMaxValue(InputRotateMaxValue.Text);
        }

        private void InputRotateMaxValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetRotateMaxValue(InputRotateMaxValue.Text);
        }

        private void InputRotateAngleStart_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateAngleStart(InputRotateAngleStart.Text);
        }

        private void InputRotateAngleStart_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetRotateAngleStart(InputRotateAngleStart.Text);
        }

        private void InputRotateAngleSweep_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetRotateAngleSweep(InputRotateAngleSweep.Text);
        }

        private void InputRotateAngleSweep_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetRotateAngleSweep(InputRotateAngleSweep.Text);
        }
    }
}
