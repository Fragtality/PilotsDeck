using PilotsDeck.Actions.Advanced;
using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI
{
    public partial class ViewCondition : UserControl
    {
        public ViewModelCondition ModelCondition { get; set; }
        protected bool Refreshing { get; set; } = false;
        protected DispatcherTimer RefreshTimer { get; set; }
        public ViewCondition(ViewModelCondition model)
        {
            InitializeComponent();
            ModelCondition = model;
            Dictionary<Comparison, string> dict = [];
            foreach (Comparison item in Enum.GetValues(typeof(Comparison)))
                dict.Add(item, ViewModelCondition.TranslateComparison(item));
            ComboComparison.ItemsSource = dict;
            ComboComparison.SelectedValuePath = "Key";
            ComboComparison.DisplayMemberPath = "Value";
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeControls()
        {
            InputAddress.Text = ModelCondition.Address;
            InputAddress.BorderBrush = new SolidColorBrush(ModelCondition.IsValidAddress ? Colors.Green : Colors.Red);
            InputAddress.BorderThickness = new Thickness(1.5);
            LabelVariable.Content = $"Type: {ModelCondition?.Condition?.Variable?.Type} | Current Value: {ModelCondition?.Condition?.Variable?.Value}";
            Refreshing = true;
            ComboComparison.SelectedValue = ModelCondition.Compare;
            Refreshing = false;
            InputValue.Text = ModelCondition.Value;
            if (ModelCondition.Compare == Comparison.HAS_CHANGED)
            {
                LabelValue.IsEnabled = false;
                InputValue.IsEnabled = false;
            }
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

            LabelVariable.Content = $"Type: {ModelCondition?.Condition?.Variable?.Type} | Current Value: {ModelCondition?.Condition?.Variable?.Value}";
        }

        private void InputAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCondition.SetAddress(InputAddress.Text);
        }

        private void InputAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCondition.SetAddress(InputAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputAddress);
        }

        private void ComboComparison_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboComparison.SelectedValue is Comparison comparison)
                ModelCondition.SetComparison(comparison);
        }

        private void InputValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCondition.SetValue(InputValue.Text);
        }

        private void InputValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCondition.SetValue(InputValue.Text);
        }
    }
}
