using PilotsDeck.Tools;
using PilotsDeck.UI.ControlsManipulator;
using PilotsDeck.UI.ViewModels;
using PilotsDeck.UI.ViewModels.Element;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI.ControlsElement
{
    public partial class ViewValue : UserControl
    {
        public ViewModelElementValue ModelValue { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }
        protected MappingListItem LastMapping { get; set; } = null;

        public ViewValue(ViewModelElementValue model)
        {
            InitializeComponent();
            ModelValue = model;
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeControls()
        {
            InputAddress.Text = ModelValue.Address;
            InputAddress.BorderBrush = new SolidColorBrush(ModelValue.IsValidAddress ? Colors.Green : Colors.Red);
            InputAddress.BorderThickness = new Thickness(1.5);
            LabelVariable.Content = $"Type: {ModelValue?.Variable?.Type} | Current Value: {ModelValue?.Variable?.Value}";
            FormatView.Content = new ViewFormat(new ViewModelFormat(ModelValue.ValueElement.Settings.ValueFormat, ModelValue.ModelAction));
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

            LabelVariable.Content = $"Type: {ModelValue?.Variable?.Type} | Current Value: {ModelValue?.Variable?.Value}";
        }

        private void InputAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelValue.SetAddress(InputAddress.Text);
        }

        private void InputAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelValue.SetAddress(InputAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputAddress);
        }
    }
}
