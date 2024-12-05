using PilotsDeck.Tools;
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
    public partial class ViewGauge : UserControl
    {
        public ViewModelElementGauge ModelGauge { get; set; }
        protected bool Refreshing { get; set; } = false;
        protected System.Drawing.Color RangeColor { get; set; }
        protected System.Drawing.Color MarkerColor { get; set; } = System.Drawing.Color.White;
        protected DispatcherTimer RefreshTimer { get; set; }

        public ViewGauge(ViewModelElementGauge model)
        {
            InitializeComponent();
            ModelGauge = model;
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

            InputValueMin.Text = ModelGauge.ValueMin;
            InputValueMax.Text = ModelGauge.ValueMax;
            InputValueScale.Text = ModelGauge.ValueScale;

            if (ModelGauge.IsArc)
            {
                ComboGaugeType.SelectedIndex = 1;
                LabelArcAngles.Visibility = Visibility.Visible;
                GridArcAngles.Visibility = Visibility.Visible;
                InputAngleStart.Text = ModelGauge.ArcAngleStart;
                InputAngleSweep.Text = ModelGauge.ArcAngleSweep;
            }
            else
            {
                ComboGaugeType.SelectedIndex = 0;
                LabelArcAngles.Visibility = Visibility.Collapsed;
                GridArcAngles.Visibility = Visibility.Collapsed;
            }

            if (ModelGauge.GaugeElement.Settings.GaugeColorRanges.Count > 0)
            {
                var colors = ModelGauge.GetRangeColors();
                var values = ModelGauge.GetRangeValues();

                for (int i = 0; i < colors.Count; i++)
                {
                    ListBoxItem item = new()
                    {
                        BorderBrush = new SolidColorBrush(colors[i]),
                        IsHitTestVisible = false,
                        Content = values[i]
                    };
                    ListRanges.Items.Add(item);
                }
            }

            if (ModelGauge.GaugeElement.Settings.GaugeMarkers.Count > 0 || ModelGauge.GaugeElement.Settings.GaugeRangeMarkers.Count > 0)
            {
                var items = ModelGauge.GetMarkerListBoxItems();
                
                foreach (var item in items)
                    ListMarker.Items.Add(item);
            }

            if (ModelGauge.UseDynamicSize)
            {
                CheckDynamicSize.IsChecked = ModelGauge.UseDynamicSize;
                CheckReverseDirection.Visibility = Visibility.Visible;
                LabelReverse.Visibility = Visibility.Visible;
                CheckReverseDirection.IsChecked = ModelGauge.RevereseDirection;
                LabelAddress.Visibility = Visibility.Visible;
                InputAddress.Visibility = Visibility.Visible;
                LabelSyntaxCheck.Visibility = Visibility.Visible;
                InputAddress.BorderBrush = new SolidColorBrush(ModelGauge.IsValidAddress ? Colors.Green : Colors.Red);
                InputAddress.BorderThickness = new Thickness(1.5);
                InputAddress.Text = ModelGauge.SizeAddress;
                LabelVariable.Content = $"Type: {ModelGauge.GaugeElement.GaugeSizeVariable?.Type} | Current Value: {ModelGauge.GaugeElement.GaugeSizeVariable?.Value}";
                LabelVariable.Visibility = Visibility.Visible;
                LabelFixedRanges.Visibility = Visibility.Visible;
                CheckFixedRanges.Visibility = Visibility.Visible;
                CheckFixedRanges.IsChecked = ModelGauge.FixedRanges;
                LabelFixedMarker.Visibility = Visibility.Visible;
                CheckFixedMarker.Visibility = Visibility.Visible;
                CheckFixedMarker.IsChecked = ModelGauge.FixedMarkers;
            }
            else
            {
                CheckDynamicSize.IsChecked = ModelGauge.UseDynamicSize;
                LabelReverse.Visibility = Visibility.Collapsed;
                CheckReverseDirection.Visibility = Visibility.Collapsed;
                LabelAddress.Visibility = Visibility.Collapsed;
                InputAddress.Visibility = Visibility.Collapsed;
                LabelSyntaxCheck.Visibility = Visibility.Collapsed;
                LabelVariable.Visibility = Visibility.Collapsed;
                LabelFixedRanges.Visibility = Visibility.Collapsed;
                CheckFixedRanges.Visibility = Visibility.Collapsed;
                LabelFixedMarker.Visibility = Visibility.Collapsed;
                CheckFixedMarker.Visibility = Visibility.Collapsed;
            }

            Refreshing = false;
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

            if (ModelGauge.UseDynamicSize)
                LabelVariable.Content = $"Type: {ModelGauge.GaugeElement.GaugeSizeVariable?.Type} | Current Value: {ModelGauge.GaugeElement.GaugeSizeVariable?.Value}";

            SettingItem.SetButton(ButtonCopyPasteRanges, SettingType.GAUGEMAP);
            SettingItem.SetButton(ButtonCopyPasteMarker, SettingType.GAUGEMARKER);
        }

        private void ComboGaugeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if ((ComboGaugeType.SelectedValue as ComboBoxItem).Content as string == "Arc")
                ModelGauge.SetArc(true);
            else if ((ComboGaugeType.SelectedValue as ComboBoxItem).Content as string == "Bar")
                ModelGauge.SetArc(false);
        }

        private void InputValueMin_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetValueMin(InputValueMin.Text);
        }

        private void InputValueMin_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelGauge.SetValueMin(InputValueMin.Text);
        }

        private void InputValueMax_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetValueMax(InputValueMax.Text);
        }

        private void InputValueMax_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelGauge.SetValueMax(InputValueMax.Text);
        }

        private void InputValueScale_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetValueScale(InputValueScale.Text);
        }

        private void InputValueScale_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelGauge.SetValueScale(InputValueScale.Text);
        }

        private void InputAngleStart_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetAngleStart(InputAngleStart.Text);
        }

        private void InputAngleStart_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelGauge.SetAngleStart(InputAngleStart.Text);
        }

        private void InputAngleSweep_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetAngleSweep(InputAngleSweep.Text);
        }

        private void InputAngleSweep_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelGauge.SetAngleSweep(InputAngleSweep.Text);
        }

        private void LabelRangeColor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new()
            {
                Color = RangeColor,
                CustomColors = ColorStore.ColorArray,
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ColorStore.AddDialogColors(colorDialog.CustomColors, colorDialog.Color);
                RangeColor = colorDialog.Color;
                LabelRangeColor.Background = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        private void ButtonCopyPasteRanges_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.GAUGEMAP, ButtonCopyPasteRanges, ModelGauge.CopyRanges, ModelGauge.GetColorRanges);
        }

        private void ButtonAddRange_Click(object sender, RoutedEventArgs e)
        {
            if (ListRanges.SelectedIndex == -1)
                ModelGauge.AddRange(InputRangeStart.Text, InputRangeEnd.Text, RangeColor);
            else
                ModelGauge.UpdateRange(ListRanges.SelectedIndex, InputRangeStart.Text, InputRangeEnd.Text, RangeColor);
            InputRangeStart.Text = "";
            InputRangeEnd.Text = "";
            RangeColor = System.Drawing.Color.White;
            LabelRangeColor.Background = Brushes.White;
            ListRanges.SelectedIndex = -1;
            ImageAddUpdateRange.SetButtonImage("plus-circle");
        }

        private void ButtonRemoveRange_Click(object sender, RoutedEventArgs e)
        {
            if (ListRanges?.SelectedIndex != -1)
                ModelGauge.RemoveRange(ListRanges.SelectedIndex);
        }

        private void CheckDynamicSize_Click(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetDynamicSize(CheckDynamicSize.IsChecked == true);
        }

        private void CheckReverseDirection_Click(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetReverseDirection(CheckReverseDirection.IsChecked == true);
        }

        private void InputAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetSizeAddress(InputAddress.Text);
        }

        private void InputAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelGauge.SetSizeAddress(InputAddress.Text);
            else
                ViewModel.SetSyntaxLabel(LabelSyntaxCheck, InputAddress);
        }

        private void CheckFixedRanges_Click(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetFixedRanges(CheckFixedRanges.IsChecked == true);
        }

        private void CheckFixedMarker_Click(object sender, RoutedEventArgs e)
        {
            ModelGauge.SetFixedMarkers(CheckFixedMarker.IsChecked == true);
        }

        private void ListRanges_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputRangeStart.Text) && string.IsNullOrWhiteSpace(InputRangeEnd.Text) && ListRanges.SelectedIndex != -1)
            {
                var range = ModelGauge.ColorRanges[ListRanges.SelectedIndex];
                InputRangeStart.Text = Conversion.ToString(range.Range[0]);
                InputRangeEnd.Text = Conversion.ToString(range.Range[1]);
                RangeColor = range.GetColor();
                LabelRangeColor.Background = new SolidColorBrush(Color.FromArgb(RangeColor.A, RangeColor.R, RangeColor.G, RangeColor.B));
                ImageAddUpdateRange.SetButtonImage("pencil");
            }
        }

        private void LabelMarkerColor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new()
            {
                Color = MarkerColor,
                CustomColors = ColorStore.ColorArray
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ColorStore.AddDialogColors(colorDialog.CustomColors, colorDialog.Color);
                MarkerColor = colorDialog.Color;
                LabelMarkerColor.Background = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        private void ListMarker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputMarkerPos.Text) && string.IsNullOrWhiteSpace(InputMarkerSize.Text) && ListMarker.SelectedIndex != -1 && ListMarker.SelectedItem is MarkerListBoxItem item) 
            {
                if (item?.IsRange == false)
                {
                    var marker = ModelGauge.GaugeMarkers[item.Index];
                    InputMarkerPos.Text = Conversion.ToString(marker.ValuePosition);
                    InputMarkerSize.Text = Conversion.ToString(marker.Size);
                    InputMarkerHeight.Text = Conversion.ToString(marker.Height);
                    InputMarkerOffset.Text = Conversion.ToString(marker.Offset);
                    MarkerColor = marker.GetColor();
                    LabelMarkerColor.Background = new SolidColorBrush(Color.FromArgb(MarkerColor.A, MarkerColor.R, MarkerColor.G, MarkerColor.B));
                    ImageAddUpdateMarker.SetButtonImage("pencil");
                }

                if (item?.IsRange == true)
                {
                    var range = ModelGauge.GaugeRangeMarkers[item.Index];
                    InputMarkerPos.Text = MarkerListBoxItem.GetRangeValueText(range);
                    InputMarkerSize.Text = Conversion.ToString(range.Size);
                    InputMarkerHeight.Text = Conversion.ToString(range.Height);
                    InputMarkerOffset.Text = Conversion.ToString(range.Offset);
                    MarkerColor = range.GetColor();
                    LabelMarkerColor.Background = new SolidColorBrush(Color.FromArgb(MarkerColor.A, MarkerColor.R, MarkerColor.G, MarkerColor.B));
                    ImageAddUpdateMarker.SetButtonImage("pencil");
                }
            }
        }

        private void ButtonAddMarker_Click(object sender, RoutedEventArgs e)
        {
            if (ListMarker.SelectedIndex == -1)
                ModelGauge.AddMarker(InputMarkerPos.Text, InputMarkerSize.Text, InputMarkerHeight.Text, InputMarkerOffset.Text, MarkerColor);
            else
                ModelGauge.UpdateMarker(ListMarker.SelectedItem as MarkerListBoxItem, InputMarkerPos.Text, InputMarkerSize.Text, InputMarkerHeight.Text, InputMarkerOffset.Text, MarkerColor);

            InputMarkerPos.Text = "";
            ListMarker.SelectedIndex = -1;
            ImageAddUpdateMarker.SetButtonImage("plus-circle");
        }

        private void ButtonRemoveMarker_Click(object sender, RoutedEventArgs e)
        {
            if (ListMarker?.SelectedIndex != -1)
                ModelGauge.RemoveMarker(ListMarker.SelectedItem as MarkerListBoxItem);
        }

        private void ButtonCopyPasteMarker_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.GAUGEMARKER, ButtonCopyPasteMarker, ModelGauge.CopyMarker, ModelGauge.GetGaugeMarkers);
        }
    }
}
