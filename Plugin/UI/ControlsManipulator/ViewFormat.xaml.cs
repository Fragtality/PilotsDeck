using PilotsDeck.Actions.Advanced;
using PilotsDeck.Tools;
using PilotsDeck.UI.ViewModels;
using PilotsDeck.UI.ViewModels.Element;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PilotsDeck.UI.ControlsManipulator
{
    public partial class ViewFormat : UserControl
    {
        public ViewModelFormat ModelFormat { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }
        protected MappingListItem LastMapping { get; set; } = null;

        public ViewFormat(ViewModelFormat model)
        {
            InitializeComponent();
            ModelFormat = model;
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeControls()
        {
            CheckboxBCD.IsChecked = ModelFormat.DecodeBCD;
            CheckboxAbsoluteValue.IsChecked = ModelFormat.UseAbsoluteValue;
            InputScalar.Text = ModelFormat.Scalar;
            InputOffset.Text = ModelFormat.Offset;
            CheckboxOffsetFirst.IsChecked = ModelFormat.OffsetFirst;
            InputDigits.Text = ModelFormat.Digits;
            InputDigitsTrailing.Text = ModelFormat.DigitsTrailing;
            CheckboxInsertSign.IsChecked = ModelFormat.InsertSign;
            CheckboxInsertSpace.IsChecked = ModelFormat.InsertSpace;
            CheckboxLimitDigits.IsChecked = ModelFormat.LimitDigits;
            InputRound.Text = ModelFormat.Round;
            InputRound.IsEnabled = !ModelFormat.RoundCeiling && !ModelFormat.RoundFloor;
            CheckboxRoundCeiling.IsChecked = ModelFormat.RoundCeiling;
            CheckboxRoundFloor.IsChecked = ModelFormat.RoundFloor;
            InputSubIndex.Text = ModelFormat.SubIndex;
            InputSubLength.Text = ModelFormat.SubLength;
            InputFormat.Text = ModelFormat.FormatString;

            var list = ModelFormat.ValueMappings;
            ListMappings.Items.Clear();
            ListMappings.SelectedValuePath = "Key";
            ListMappings.DisplayMemberPath = "Display";
            ListMappings.ItemsSource = list.ToList();
            SetRadioButtons();
        }

        private void SetRadioButtons()
        {
            CheckboxTypeNumber.IsChecked = false;
            CheckboxTypeString.IsChecked = false;

            if (ModelFormat.Preferrence == DisplayValueType.NUMBER)
                CheckboxTypeNumber.IsChecked = true;
            if (ModelFormat.Preferrence == DisplayValueType.STRING)
                CheckboxTypeString.IsChecked = true;
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

            SettingItem.SetButton(ButtonCopyPasteMapping, SettingType.VALUEMAP);
        }

        private void CheckboxBCD_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetBCD(CheckboxBCD.IsChecked == true);
        }

        private void CheckboxAbsoluteValue_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetAbsolute(CheckboxAbsoluteValue.IsChecked == true);
        }

        private void InputScalar_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetScalar(InputScalar.Text);
        }

        private void InputScalar_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetScalar(InputScalar.Text);
        }

        private void InputOffset_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetOffset(InputOffset.Text);
        }

        private void InputOffset_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetOffset(InputOffset.Text);
        }

        private void CheckboxOffsetFirst_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetOffsetFirst(CheckboxOffsetFirst.IsChecked == true);
        }

        private void InputRound_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetRound(InputRound.Text);
        }

        private void InputRound_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetRound(InputRound.Text);
        }

        private void CheckboxRoundCeiling_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetRoundCeiling(CheckboxRoundCeiling.IsChecked == true);
        }

        private void CheckboxRoundFloor_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetRoundFloor(CheckboxRoundFloor.IsChecked == true);
        }

        private void InputDigits_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetDigits(InputDigits.Text);
        }

        private void InputDigits_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetDigits(InputDigits.Text);
        }

        private void InputDigitsTrailing_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetDigitsTrailing(InputDigitsTrailing.Text);
        }

        private void InputDigitsTrailing_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetDigitsTrailing(InputDigitsTrailing.Text);
        }

        private void CheckboxInsertSign_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetInsertSign(CheckboxInsertSign.IsChecked == true);
        }

        private void CheckboxInsertSpace_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetInsertSpace(CheckboxInsertSpace.IsChecked == true);
        }

        private void CheckboxLimitDigits_Click(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetLimitDigits(CheckboxLimitDigits.IsChecked == true);
        }

        private void InputSubIndex_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetSubIndex(InputSubIndex.Text);
        }

        private void InputSubIndex_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetSubIndex(InputSubIndex.Text);
        }

        private void InputSubLength_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetSubLength(InputSubLength.Text);
        }

        private void InputSubLength_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetSubLength(InputSubLength.Text);
        }

        private void InputFormat_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelFormat.SetFormatString(InputFormat.Text);
        }

        private void InputFormat_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelFormat.SetFormatString(InputFormat.Text);
        }

        private void ButtonCopyPasteMapping_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.VALUEMAP, ButtonCopyPasteMapping, ModelFormat.CopyMappings, ModelFormat.GetDictionary);
        }

        private void ButtonAddMapping_Click(object sender, RoutedEventArgs e)
        {
            if (ListMappings.SelectedIndex == -1)
                ModelFormat.AddMapping(InputMappingValue.Text, InputMappingString.Text);
            else
                ModelFormat.UpdateMapping(LastMapping, InputMappingValue.Text, InputMappingString.Text);

            LastMapping = null;
            InputMappingValue.Text = "";
            InputMappingString.Text = "";
            ListMappings.SelectedIndex = -1;
            ImageAddUpdateMapping.SetButtonImage("plus-circle");
        }

        private void InputMappingString_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e) && !string.IsNullOrWhiteSpace(InputMappingString.Text) && !string.IsNullOrWhiteSpace(InputMappingValue.Text))
                ButtonAddMapping_Click(null, null);
        }

        private void InputMappingString_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(InputMappingString.Text) && !string.IsNullOrWhiteSpace(InputMappingValue.Text))
                ButtonAddMapping_Click(null, null);
        }

        private void ButtonRemoveMapping_Click(object sender, RoutedEventArgs e)
        {
            if (ListMappings.SelectedItem is MappingListItem mapping && mapping?.Key != null)
                ModelFormat.RemoveMapping(mapping.Key);
        }

        private void ListMappings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListMappings.SelectedItem is MappingListItem mapping && mapping?.Key != null)
            {
                InputMappingValue.Text = mapping.Key;
                InputMappingString.Text = mapping.Value;
                LastMapping = mapping;
                ImageAddUpdateMapping.SetButtonImage("pencil");
            }
        }

        private void CheckboxType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox radio)
            {
                if (radio.Name == "CheckboxTypeString")
                    ModelFormat.SetPreferrence(DisplayValueType.STRING);
                else if (radio.Name == "CheckboxTypeNumber")
                    ModelFormat.SetPreferrence(DisplayValueType.NUMBER);
            }
        }
    }
}
