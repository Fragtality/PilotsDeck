using PilotsDeck.Actions.Advanced.Manipulators;
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
    public partial class ViewManipulator : UserControl
    {
        public ViewModelManipulator ModelManipulator { get; set; }
        public Window ParentWindow { get; set; }
        protected DispatcherTimer RefreshTimer { get; set; }
        public ViewManipulator(ViewModelManipulator model, Window parentWindow)
        {
            InitializeComponent();
            ModelManipulator = model;
            ParentWindow = parentWindow;
            InitializeControls();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        private void InitializeControls()
        {
            CheckboxAnyCondition.IsChecked = ModelManipulator.AnyCondition;
            UserControl manipulatorControl = null;

            if (ModelManipulator.IsManipulatorColor)
            {
                LabelColor.Visibility = Visibility.Visible;
                InputColor.Visibility = Visibility.Visible;
                ButtonColorClipboard.Visibility = Visibility.Visible;
                InputColor.Background = new SolidColorBrush(ModelManipulator.Color);
                SettingItem.SetButton(ButtonColorClipboard, SettingType.COLOR);
            }
            else
            {
                LabelColor.Visibility = Visibility.Collapsed;
                InputColor.Visibility = Visibility.Collapsed;
                ButtonColorClipboard.Visibility = Visibility.Collapsed;
            }

            if (ModelManipulator.IsManipulatorIndicator)
                manipulatorControl = new ViewIndicator(new ViewModelManipulatorIndicator(ModelManipulator.Manipulator, ModelManipulator.ModelAction), ParentWindow);

            if (ModelManipulator.IsManipulatorVisible)
                manipulatorControl = new ViewVisible(ModelManipulator);

            if (ModelManipulator.IsManipulatorTransparency)
                manipulatorControl = new ViewTransparency(new ViewModelManipulatorTransparency(ModelManipulator.Manipulator as ManipulatorTransparency, ModelManipulator.ModelAction));

            if (ModelManipulator.IsManipulatorRotate)
                manipulatorControl = new ViewRotate(new ViewModelManipulatorRotate(ModelManipulator.Manipulator as ManipulatorRotate, ModelManipulator.ModelAction));

            if (ModelManipulator.IsManipulatorFormat)
                manipulatorControl = new ViewFormat(new ViewModelFormat(ModelManipulator.Manipulator.Settings.ConditionalFormat, ModelManipulator.ModelAction));

            if (ModelManipulator.IsManipulatorSizePos)
                manipulatorControl = new ViewSizePos(new ViewModelManipulatorSizePos(ModelManipulator.Manipulator as ManipulatorSizePos, ModelManipulator.ModelAction));

            if (manipulatorControl != null)
            {
                ManipulatorView.Visibility = Visibility.Visible;
                ManipulatorView.Content = manipulatorControl;
            }
            else
            {
                ManipulatorView.Visibility = Visibility.Collapsed;
                ManipulatorView.Content = null;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ModelManipulator.IsManipulatorColor)
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

            if (ModelManipulator.IsManipulatorColor)
                SettingItem.SetButton(ButtonColorClipboard, SettingType.COLOR);
        }

        private void CheckboxAnyCondition_Click(object sender, RoutedEventArgs e)
        {
            ModelManipulator.SetAnyCondition(CheckboxAnyCondition.IsChecked == true);
        }

        private void InputColor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new()
            {
                Color = ModelManipulator.ColorForms,
                CustomColors = ColorStore.ColorArray
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ColorStore.AddDialogColors(colorDialog.CustomColors, colorDialog.Color);
                ModelManipulator.SetColor(colorDialog.Color);
            }
        }

        private void ButtonColorClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (ModelManipulator.IsManipulatorColor)
                SettingItem.Clipboard_Click(SettingType.COLOR, ButtonColorClipboard, ModelManipulator.SetColor, ModelManipulator.Manipulator.Settings.GetColor);
            else
                SettingItem.SetButton(ButtonColorClipboard, SettingType.COLOR);
        }
    }
}
