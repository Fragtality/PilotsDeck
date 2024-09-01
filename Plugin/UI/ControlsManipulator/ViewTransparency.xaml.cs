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
    public partial class ViewTransparency : UserControl
    {
        public ViewModelManipulatorTransparency ModelManipulator { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }

        public ViewTransparency(ViewModelManipulatorTransparency model)
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
            CheckboxDynamicTransparency.IsChecked = ModelManipulator.DynamicTransparency;

            if (ModelManipulator.DynamicTransparency)
            {
                LabelTransparencySetValue.Visibility = Visibility.Collapsed;
                InputTransparencySetValue.Visibility = Visibility.Collapsed;
                LabelTransparencyAddress.Visibility = Visibility.Visible;
                PanelVariable.Visibility = Visibility.Visible;
                LabelTransparencyMinMaxValue.Visibility = Visibility.Visible;
                GridTransparencyMinMaxValue.Visibility = Visibility.Visible;

                InputTransparencyAddress.Text = ModelManipulator.TransparencyAddress;
                InputTransparencyAddress.BorderBrush = new SolidColorBrush(ModelManipulator.IsValidTransparencyAddress ? Colors.Green : Colors.Red);
                InputTransparencyAddress.BorderThickness = new Thickness(1.5);

                InputTransparencyMinValue.Text = ModelManipulator.TransparencyMinValue;
                InputTransparencyMaxValue.Text = ModelManipulator.TransparencyMaxValue;
            }
            else
            {
                LabelTransparencySetValue.Visibility = Visibility.Visible;
                InputTransparencySetValue.Visibility = Visibility.Visible;
                LabelTransparencyAddress.Visibility = Visibility.Collapsed;
                PanelVariable.Visibility = Visibility.Collapsed;
                LabelTransparencyMinMaxValue.Visibility = Visibility.Collapsed;
                GridTransparencyMinMaxValue.Visibility = Visibility.Collapsed;

                InputTransparencySetValue.Text = ModelManipulator.TransparencySetValue;
            }

            if (ModelManipulator.DynamicTransparency)
                LabelTransparencyVariable.Content = $"Type: {ModelManipulator.TransparencyVariable?.Type} | Current Value: {ModelManipulator.TransparencyVariable?.Value}";
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ModelManipulator.DynamicTransparency)
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

            if (ModelManipulator.DynamicTransparency)
                LabelTransparencyVariable.Content = $"Type: {ModelManipulator.TransparencyVariable?.Type} | Current Value: {ModelManipulator.TransparencyVariable?.Value}";
        }

        private void InputTransparencySetValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetTransparencySetValue(InputTransparencySetValue.Text);
        }

        private void InputTransparencySetValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetTransparencySetValue(InputTransparencySetValue.Text);
        }

        private void CheckboxDynamicTransparency_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetDynamicTransparency(CheckboxDynamicTransparency.IsChecked == true);
        }

        private void InputTransparencyAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetTransparencyAddress(InputTransparencyAddress.Text);
        }

        private void InputTransparencyAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetTransparencyAddress(InputTransparencyAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputTransparencyAddress);
        }

        private void InputTransparencyMinValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetTransparencyMinValue(InputTransparencyMinValue.Text);
        }

        private void InputTransparencyMinValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetTransparencyMinValue(InputTransparencyMinValue.Text);
        }

        private void InputTransparencyMaxValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetTransparencyMaxValue(InputTransparencyMaxValue.Text);
        }

        private void InputTransparencyMaxValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetTransparencyMaxValue(InputTransparencyMaxValue.Text);
        }
    }
}
