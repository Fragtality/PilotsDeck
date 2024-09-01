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
    public partial class ViewSizePos : UserControl
    {
        public ViewModelManipulatorSizePos ModelManipulator { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }

        public ViewSizePos(ViewModelManipulatorSizePos model)
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
            CheckboxChangeX.IsChecked = ModelManipulator.ChangeX;
            CheckboxChangeY.IsChecked = ModelManipulator.ChangeY;
            CheckboxChangeW.IsChecked = ModelManipulator.ChangeW;
            CheckboxChangeH.IsChecked = ModelManipulator.ChangeH;

            InputValueX.Text = ModelManipulator.ValueX;
            InputValueY.Text = ModelManipulator.ValueY;
            InputValueW.Text = ModelManipulator.ValueW;
            InputValueH.Text = ModelManipulator.ValueH;

            CheckboxDynamic.IsChecked = ModelManipulator.ChangeSizePosDynamic;

            if (ModelManipulator.ChangeSizePosDynamic)
            {
                InputValueX.IsEnabled = false;
                InputValueY.IsEnabled = false;
                InputValueW.IsEnabled = false;
                InputValueH.IsEnabled = false;

                LabelSizePosAddress.Visibility = Visibility.Visible;
                PanelVariable.Visibility = Visibility.Visible;
                LabelSizePosVariable.Visibility = Visibility.Visible;
                LabelSizePosMinMaxValue.Visibility = Visibility.Visible;
                GridSizePosMinMaxValue.Visibility = Visibility.Visible;

                InputSizePosAddress.Text = ModelManipulator.SizePosAddress;
                InputSizePosAddress.BorderBrush = new SolidColorBrush(ModelManipulator.IsValidSizePosAddress ? Colors.Green : Colors.Red);
                InputSizePosAddress.BorderThickness = new Thickness(1.5);
                InputSizePosMinValue.Text = ModelManipulator.SizePosMinValue;
                InputSizePosMaxValue.Text = ModelManipulator.SizePosMaxValue;
            }
            else
            {
                InputValueX.IsEnabled = true;
                InputValueY.IsEnabled = true;
                InputValueW.IsEnabled = true;
                InputValueH.IsEnabled = true;

                LabelSizePosAddress.Visibility = Visibility.Collapsed;
                PanelVariable.Visibility = Visibility.Collapsed;
                LabelSizePosVariable.Visibility = Visibility.Collapsed;
                LabelSizePosMinMaxValue.Visibility = Visibility.Collapsed;
                GridSizePosMinMaxValue.Visibility = Visibility.Collapsed;
            }

            if (ModelManipulator.ChangeSizePosDynamic)
                LabelSizePosVariable.Content = $"Type: {ModelManipulator.SizePosVariable?.Type} | Current Value: {ModelManipulator.SizePosVariable?.Value}";
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ModelManipulator.ChangeSizePosDynamic)
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

            if (ModelManipulator.ChangeSizePosDynamic)
                LabelSizePosVariable.Content = $"Type: {ModelManipulator.SizePosVariable?.Type} | Current Value: {ModelManipulator.SizePosVariable?.Value}";
        }

        private void CheckboxChangeX_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetChange("X", CheckboxChangeX.IsChecked == true);
        }

        private void InputValueX_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetValue("X", InputValueX.Text);
        }

        private void InputValueX_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetValue("X", InputValueX.Text);
        }

        private void CheckboxChangeY_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetChange("Y", CheckboxChangeY.IsChecked == true);
        }

        private void InputValueY_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetValue("Y", InputValueY.Text);
        }

        private void InputValueY_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetValue("Y", InputValueY.Text);
        }

        private void CheckboxChangeW_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetChange("W", CheckboxChangeW.IsChecked == true);
        }

        private void InputValueW_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetValue("W", InputValueW.Text);
        }

        private void InputValueW_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetValue("W", InputValueW.Text);
        }

        private void CheckboxChangeH_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetChange("H", CheckboxChangeH.IsChecked == true);
        }

        private void InputValueH_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetValue("H", InputValueH.Text);
        }

        private void InputValueH_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetValue("H", InputValueH.Text);
        }

        private void CheckboxDynamic_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetDynamic(CheckboxDynamic.IsChecked == true);
        }

        private void InputSizePosAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetAddress(InputSizePosAddress.Text);
        }

        private void InputSizePosAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetAddress(InputSizePosAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputSizePosAddress);
        }

        private void InputSizePosMinValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetMinValue(InputSizePosMinValue.Text);
        }

        private void InputSizePosMinValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetMinValue(InputSizePosMinValue.Text);
        }

        private void InputSizePosMaxValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetMaxValue(InputSizePosMaxValue.Text);
        }

        private void InputSizePosMaxValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelManipulator.SetMaxValue(InputSizePosMaxValue.Text);
        }
    }
}
