using ProfileManager.json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProfileManager
{
    public partial class ProfileListItem : UserControl
    {
        public class TransferSettings
        {
            public bool DefaultProfile;
            public bool AircraftProfile;
            public SimulatorType DefaultSimulator;
            public List<string> AircraftStrings;
        }

        private static TransferSettings SettingClipboard = null;

        protected Action ActionUpdateAll;

        public ProfileViewModel ViewModel { get; protected set; }

        protected Brush BrushDefaultBackground { get; set; } = new SolidColorBrush(Colors.DarkGray);
        protected Brush BrushDefault { get; } = new SolidColorBrush(Colors.DarkGray);
        protected Brush BrushRed { get; } = new SolidColorBrush(Colors.Red);
        protected Brush BrushGreen { get; } = new SolidColorBrush(Colors.Green);
        protected Brush BrushBlue { get; } = SystemColors.HighlightBrush;
        protected Brush BrushOrange { get; } = new SolidColorBrush(Colors.Orange);
        protected Thickness ThicknessDefault { get; } = new Thickness(1);
        protected Thickness ThicknessMarked { get; } = new Thickness(1);

        public ProfileListItem(ProfileManifest manifest, Action refreshAll)
        {
            InitializeComponent();

            SelectDefaultSimulator.ItemsSource = new Collection<KeyValuePair<int, string>>(ProfileMapping.SimulatorSelections);

            BrushDefaultBackground = ButtonCopyPasteSettings.Background;

            ViewModel = new (manifest);
            ActionUpdateAll = refreshAll;
            UpdateView();
        }

        public void UpdateView()
        {
            LabelProfileName.Text = ViewModel.ProfileName;
            LabelProfileName.IsEnabled = false;

            LabelDeviceInfo.Text = ViewModel.DeviceInfo;
            LabelProfileDirectory.Text = ViewModel.ProfileDirectory;

            CheckboxIsPreparedForSwitching.IsChecked = ViewModel.IsPreparedForSwitching;
            CheckboxIsPreparedForSwitching.IsEnabled = !ViewModel.IsPreparedForSwitching;
            bool enabled = ViewModel.IsPreparedForSwitching;

            CheckboxProfileNever.IsChecked = ViewModel.ProfileNever;
            CheckboxProfileNever.IsEnabled = enabled;

            CheckboxProfileDefault.IsChecked = ViewModel.ProfileDefault;
            CheckboxProfileDefault.IsEnabled = enabled;
            SelectDefaultSimulator.IsEnabled = CheckboxProfileDefault.IsChecked == true && enabled;
            SelectDefaultSimulator.SelectedIndex = ViewModel.DefaultSimulator;

            CheckboxProfileAircraft.IsChecked = ViewModel.ProfileAircraft;
            CheckboxProfileAircraft.IsEnabled = enabled;
            ListAircraft.IsEnabled = CheckboxProfileAircraft.IsChecked == true && enabled;
            ButtonAircraftAdd.IsEnabled = CheckboxProfileAircraft.IsChecked == true && enabled;
            ButtonAircraftAdd.IsEnabled = CheckboxProfileAircraft.IsChecked == true && enabled;
            ButtonAircraftRemove.IsEnabled = CheckboxProfileAircraft.IsChecked == true && enabled;
            ListAircraft.ItemsSource = ViewModel.AircraftCollection;
            InputAircraftNew.IsEnabled = CheckboxProfileAircraft.IsChecked == true && enabled;

            if (SettingClipboard == null && ButtonCopyPasteSettings.Background != BrushDefaultBackground)
            {
                ButtonCopyPasteSettings.IsHitTestVisible = true;
                ButtonCopyPasteSettings.Background = BrushDefaultBackground;
            }
            if (SettingClipboard != null && ButtonCopyPasteSettings.Background == BrushDefaultBackground
                && ButtonCopyPasteSettings.IsHitTestVisible && ButtonCopyPasteSettings.IsEnabled)
            {
                ButtonCopyPasteSettings.Background = BrushBlue;
            }

            ButtonCopyPasteSettings.IsEnabled = ViewModel.IsPreparedForSwitching;

            UpdateBorder();
        }

        public void UpdateBorder()
        {
            if (ViewModel.IsChanged)
                BorderChanged();
            else if (ViewModel.IsMappedProfile)
                BorderMapped();
            else
                BorderDefault();
        }

        public void BorderChanged()
        {
            BorderThickness = ThicknessMarked;
            BorderBrush = BrushOrange;
        }

        public void BorderMapped()
        {
            BorderThickness = ThicknessMarked;
            BorderBrush = BrushGreen;
        }

        public void BorderDefault()
        {
            BorderThickness = ThicknessMarked;
            BorderBrush = BrushBlue;
        }

        private void ButtonAircraftAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InputAircraftNew.Text))
            {
                Logger.Log(LogLevel.Debug, $"Adding Aircraft '{InputAircraftNew.Text}' to List @ {ViewModel.ProfileName}");
                ViewModel.AircraftAdd(InputAircraftNew.Text);
                InputAircraftNew.Text = "";
                UpdateView();
            }
        }

        private void InputAircraftNew_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            ButtonAircraftAdd_Click(null, null);
        }

        private void ButtonAircraftRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ListAircraft.SelectedItem != null)
            {
                Logger.Log(LogLevel.Debug, $"Removing Aircraft '{ListAircraft.SelectedItem as string}' from List @ {ViewModel.ProfileName}");
                ViewModel.AircraftRemove(ListAircraft.SelectedItem as string);
                UpdateView();
            }
        }

        private void CheckboxIsPreparedForSwitching_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.Manifest.IsPreparedForSwitching)
            {
                Logger.Log(LogLevel.Debug, $"Enable Switching for Profile '{ViewModel.ProfileName}'");
                CheckboxIsPreparedForSwitching.IsEnabled = false;
                ViewModel.PrepareProfileForSwitching();
                UpdateView();
            }
        }

        private void SelectDefaultSimulator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SetDefaultSimulator(SelectDefaultSimulator.SelectedIndex);
        }

        private void ProfileCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (sender == CheckboxProfileNever && CheckboxProfileNever.IsChecked == true)
            {
                ViewModel.SetProfileNever(true);
                UpdateView();
            }
            if (sender == CheckboxProfileDefault && CheckboxProfileDefault.IsChecked == true)
            {
                ViewModel.SetProfileDefault(true);
                UpdateView();
            }
            if (sender == CheckboxProfileAircraft && CheckboxProfileAircraft.IsChecked == true)
            {
                ViewModel.SetProfileAircraft(true);
                UpdateView();
            }
        }

        private void ButtonEditProfileName_Click(object sender, RoutedEventArgs e)
        {
            if (InputProfileName.Visibility == Visibility.Collapsed)
            {
                LabelProfileName.Visibility = Visibility.Collapsed;
                InputProfileName.Visibility = Visibility.Visible;
                InputProfileName.Text = LabelProfileName.Text;
            }
            else
                SetProfileName(InputProfileName.Text);
        }

        private void SetProfileName(string name)
        {
            if (!string.IsNullOrEmpty(name) && name != ViewModel.ProfileName)
            {
                ViewModel.SetProfileName(name);
            }
            else
            {
                Logger.Log(LogLevel.Warning, "Empty or same Profile Name entered!");
            }

            InputProfileName.Text = "";
            LabelProfileName.Visibility = Visibility.Visible;
            InputProfileName.Visibility = Visibility.Collapsed;
            UpdateView();
        }

        private void InputProfileName_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || e.Key != System.Windows.Input.Key.Return)
                return;

            SetProfileName(InputProfileName.Text);
        }

        private void InputProfileName_LostFocus(object sender, RoutedEventArgs e)
        {
            SetProfileName(InputProfileName.Text);
        }

        private void ButtonDeleteManifest_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.Manifest.DeleteFlag)
            {
                var result = MessageBox.Show("This will delete the Profile from the StreamDeck and Filesystem (move to Trash)!\r\nContinue?", "Delete StreamDeck Profile", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Yes)
                    ViewModel.ToggleDeleteFlag();
            }
            else
                ViewModel.ToggleDeleteFlag();
            
            UpdateView();
        }

        private void ButtonCopyPasteSettings_Click(object sender, RoutedEventArgs e)
        {
            if (SettingClipboard == null)
            {
                Logger.Log(LogLevel.Debug, "Copy Settings to Clipboard");

                SettingClipboard = new()
                {
                    DefaultProfile = ViewModel.Mapping.DefaultProfile,
                    DefaultSimulator = ViewModel.Mapping.DefaultSimulator,
                    AircraftProfile = ViewModel.Mapping.AircraftProfile,
                    AircraftStrings = ViewModel.GetNewList(),
                };

                ButtonCopyPasteSettings.Background = BrushGreen;
                ButtonCopyPasteSettings.IsHitTestVisible = false;
                ActionUpdateAll();
            }
            else
            {
                Logger.Log(LogLevel.Debug, "Paste Settings from Clipboard");

                if (!ViewModel.HasMapping)
                    ViewModel.PrepareProfileForSwitching();

                ViewModel.SetProfileDefault(SettingClipboard.DefaultProfile);
                ViewModel.SetProfileAircraft(SettingClipboard.AircraftProfile);
                ViewModel.SetDefaultSimulator(SettingClipboard.DefaultSimulator);
                ViewModel.CopyAircraftList(SettingClipboard.AircraftStrings);
                ViewModel.Mapping.IsChanged = true;

                SettingClipboard = null;
                ActionUpdateAll();
            }
        }
    }
}
