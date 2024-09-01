using PilotsDeck.Actions;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI
{
    public partial class ViewCommand : UserControl
    {
        public ViewModelCommand ModelCommand { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }
        protected bool Refreshing { get; set; } = false;

        public ViewCommand(ViewModelCommand command)
        {
            InitializeComponent();
            ModelCommand = command;
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refreshing = true;
            ViewModel.SetComboBox(ComboType, ViewModel.GetSimTypes(), ModelCommand.CommandType);
            Refreshing = false;

            if (ModelCommand.IsValueType)
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

            if (ModelCommand.IsValueType && !InputAddress.IsFocused)
                LabelVariable.Content = $"Type: {ModelCommand?.Command?.Variable?.Type} | Current Value: {ModelCommand?.Command?.Variable?.Value}";
        }

        private void InitializeControls()
        {
            InputName.Text = ModelCommand.Command.Name ?? "";
            InputAddress.Text = ModelCommand.Address ?? "";
            InputAddress.BorderBrush = new SolidColorBrush(ModelCommand.IsValidCommand ? Colors.Green : Colors.Red);
            InputAddress.BorderThickness = new Thickness(1.5);

            if (ModelCommand.IsValueType)
            {
                LabelVariable.Visibility = Visibility.Visible;
                LabelVariable.Content = $"Type: {ModelCommand?.Command?.Variable?.Type} | Current Value: {ModelCommand?.Command?.Variable?.Value}";
            }
            else
                LabelVariable.Visibility = Visibility.Collapsed;

            if (ModelCommand.CommandType == SimCommandType.BVAR)
            {
                LabelBvar.Visibility = Visibility.Visible;
                CheckboxDoNotRequestBvar.Visibility = Visibility.Visible;
                CheckboxDoNotRequestBvar.IsChecked = ModelCommand.DoNotRequestBvar;
            }
            else
            {
                LabelBvar.Visibility = Visibility.Collapsed;
                CheckboxDoNotRequestBvar.Visibility = Visibility.Collapsed;
            }

            if (ModelCommand.DeckCommandType == StreamDeckCommand.KEY_UP || ModelCommand.DeckCommandType == StreamDeckCommand.DIAL_UP)
            {
                LabelTimeAfter.Visibility = Visibility.Visible;
                GridLongPress.Visibility = Visibility.Visible;
                InputTimeAfter.Text = ModelCommand.TimeAfterLastDown;
            }
            else
            {
                LabelTimeAfter.Visibility = Visibility.Collapsed;
                GridLongPress.Visibility = Visibility.Collapsed;
            }

            if (ModelCommand.DeckCommandType == StreamDeckCommand.DIAL_LEFT || ModelCommand.DeckCommandType == StreamDeckCommand.DIAL_RIGHT)
            {
                LabelTickDelay.Visibility = Visibility.Visible;
                GridTickDelay.Visibility = Visibility.Visible;
                InputTickDelay.Text = ModelCommand.TickDelay;
            }
            else
            {
                LabelTickDelay.Visibility = Visibility.Collapsed;
                GridTickDelay.Visibility = Visibility.Collapsed;
            }

            if (ModelCommand.IsValueType && !ModelCommand.IsCode)
            {
                LabelResetSwitch.Visibility = Visibility.Visible;
                GridResetDelay.Visibility = Visibility.Visible;
                CheckboxResetSwitch.IsChecked = ModelCommand.ResetSwitch;
                LabelResetValue.Visibility = Visibility.Visible;
                InputResetValue.Visibility = Visibility.Visible;
                InputResetValue.Text = ModelCommand.ResetValue;
                InputResetDelay.Text = ModelCommand.ResetDelay;
            }
            else
            {
                LabelResetSwitch.Visibility = Visibility.Collapsed;
                GridResetDelay.Visibility = Visibility.Collapsed;
                LabelResetValue.Visibility = Visibility.Collapsed;
                InputResetValue.Visibility = Visibility.Collapsed;
            }

            if (ModelCommand.HasCommandDelay)
            {
                LabelCommandDelay.Visibility = Visibility.Visible;
                GridCommandDelay.Visibility = Visibility.Visible;
                CheckBoxUseDelay.IsChecked = ModelCommand.UseCommandDelay;
                InputCommandDelay.Text = ModelCommand.CommandDelay;
            }
            else
            {
                LabelCommandDelay.Visibility = Visibility.Collapsed;
                GridCommandDelay.Visibility = Visibility.Collapsed;
            }

            if (ModelCommand.IsValueType)
            {
                LabelWriteValue.Visibility = Visibility.Visible;
                InputWriteValue.Visibility = Visibility.Visible;
                InputWriteValue.Text = ModelCommand.WriteValue;
            }
            else
            {
                LabelWriteValue.Visibility = Visibility.Collapsed;
                InputWriteValue.Visibility = Visibility.Collapsed;
            }

            if (ModelCommand.ConditionCount > 0)
            {
                LabelAnyCondition.Visibility = Visibility.Visible;
                CheckboxAnyCondition.Visibility = Visibility.Visible;
                CheckboxAnyCondition.IsChecked = ModelCommand.AnyCondition;
            }
            else
            {
                LabelAnyCondition.Visibility = Visibility.Collapsed;
                CheckboxAnyCondition.Visibility = Visibility.Collapsed;
            }
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboType.SelectedValue is SimCommandType type)
                ModelCommand.SetType(type);
        }

        private void InputAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetAddress(InputAddress.Text);
        }

        private void InputAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetAddress(InputAddress.Text);
            else
            {
                bool valid = SimCommand.IsValidAddressForType(InputAddress.Text, ModelCommand.CommandType, ModelCommand.DoNotRequestBvar);
                LabelSyntaxCheck.Foreground = new SolidColorBrush(valid ? Colors.Green: Colors.Red);
                LabelSyntaxCheck.Content = valid ? "Valid Syntax" : "Invalid Syntax";
            }
        }

        private void CheckboxDoNotRequestBvar_Click(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetDoNotRequestBvar(CheckboxDoNotRequestBvar.IsChecked == true);
        }

        private void InputTimeAfter_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetTimeAfter(InputTimeAfter.Text);
        }

        private void InputTimeAfter_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetTimeAfter(InputTimeAfter.Text);
        }

        private void CheckboxResetSwitch_Click(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetResetSwitch(CheckboxResetSwitch.IsChecked == true);
        }

        private void InputResetDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetResetDelay(InputResetDelay.Text);
        }

        private void InputResetDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetResetDelay(InputResetDelay.Text);
        }

        private void CheckBoxUseDelay_Click(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetUseDelay(CheckBoxUseDelay.IsChecked == true);
        }

        private void InputCommandDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetCommandDelay(InputCommandDelay.Text);
        }

        private void InputCommandDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetCommandDelay(InputCommandDelay.Text);
        }

        private void InputWriteValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetWriteValue(InputWriteValue.Text);
        }

        private void InputWriteValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetWriteValue(InputWriteValue.Text);
        }

        private void InputResetValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetResetValue(InputResetValue.Text);
        }

        private void InputResetValue_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetResetValue(InputResetValue.Text);
        }

        private void CheckboxAnyCondition_Click(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetAnyCondition(CheckboxAnyCondition.IsChecked == true);
        }

        private void InputTickDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetTickDelay(InputTickDelay.Text);
        }

        private void InputTickDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetTickDelay(InputTickDelay.Text);
        }

        private void InputName_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelCommand.SetName(InputName.Text);
        }

        private void InputName_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelCommand.SetName(InputName.Text);
        }
    }
}
