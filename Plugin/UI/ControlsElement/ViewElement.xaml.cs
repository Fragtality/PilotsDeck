using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
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
    public partial class ViewElement : UserControl
    {
        public ViewModelElement ModelElement { get; set; }
        public Window ParentWindow { get; set; }
        protected DispatcherTimer CopyMonitor { get; set; }
        protected bool Refreshing { get; set; } = false;
        protected SolidColorBrush AlignmentBorderSelected { get; set; } = SystemColors.HighlightBrush;
        protected SolidColorBrush AlignmentBorder { get; set; } = SystemColors.WindowFrameBrush;

        public ViewElement(ViewModelElement model, Window parent)
        {
            InitializeComponent();
            ModelElement = model;
            ParentWindow = parent;
            InitializeControls();
            CopyMonitor = new()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            CopyMonitor.Tick += CheckCopy;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refreshing = true;
            ViewModel.SetComboBox(ComboCenter, ViewModel.GetCenterTypes(), ModelElement.Center);
            ViewModel.SetComboBox(ComboScale, ViewModel.GetScaleTypes(), ModelElement.Scale);
            if (ModelElement.IsPrimitive)
                ViewModel.SetComboBox(ComboPrimitiveType, ViewModel.GetPrimitiveTypes(), ModelElement.Primitive);
            Refreshing = false;
            CopyMonitor.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CopyMonitor?.Stop();
        }

        public void CheckCopy(object sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
            {
                CopyMonitor.Stop();
            }
            else
            {
                SettingItem.SetButton(ButtonColorClipboard, SettingType.COLOR);
                SettingItem.SetButton(ButtonFontClipboard, SettingType.FONT);
                SettingItem.SetButton(ButtonPosClipboard, SettingType.POS);
                SettingItem.SetButton(ButtonSizeClipboard, SettingType.SIZE);
            }
        }

        private void InitializeControls()
        {
            InputName.Text = ModelElement.RawName;
            InputPosX.Text = ModelElement.PosX;
            InputPosY.Text = ModelElement.PosY;
            InputWidth.Text = ModelElement.Width;
            InputHeight.Text = ModelElement.Height;
            SettingItem.SetButton(ButtonPosClipboard, SettingType.POS);
            SettingItem.SetButton(ButtonSizeClipboard, SettingType.SIZE);
            LabelCanvasSize.Content = $"Canvas Size: {ModelElement.CanvasX} x {ModelElement.CanvasY}";
            InputRotation.Text = ModelElement.Rotation;

            if (ModelElement.IsText || ModelElement.IsValue || ModelElement.IsGauge || ModelElement.IsPrimitive || ModelElement.IsImage)
            {
                LabelColor.Visibility = Visibility.Visible;
                LabelColorSelect.Visibility = Visibility.Visible;
                LabelColorSelect.Background = new SolidColorBrush(ModelElement.Color);
                ButtonColorClipboard.Visibility = Visibility.Visible;
                SettingItem.SetButton(ButtonColorClipboard, SettingType.COLOR);
            }
            else
            {
                LabelColor.Visibility = Visibility.Collapsed;
                LabelColorSelect.Visibility = Visibility.Collapsed;
                ButtonColorClipboard.Visibility = Visibility.Collapsed;
            }

            if (ModelElement.IsText || ModelElement.IsValue)
            {
                LabelFont.Visibility = Visibility.Visible;
                LabelFontSelect.Content = ModelElement.FontInfo;
                LabelFontSelect.FontFamily = ModelElement.FontFamily;
                PanelFont.Visibility = Visibility.Visible;
                SettingItem.SetButton(ButtonFontClipboard, SettingType.FONT);
                SetTextAlignButtons();
            }
            else
            {
                LabelFont.Visibility = Visibility.Collapsed;
                PanelFont.Visibility = Visibility.Collapsed;
            }

            if (ModelElement.IsText)
            {
                LabelElementText.Visibility = Visibility.Visible;
                InputElementText.Visibility = Visibility.Visible;
                InputElementText.Text = ModelElement.ElementText;
            }
            else
            {
                LabelElementText.Visibility = Visibility.Collapsed;
                InputElementText.Visibility = Visibility.Collapsed;
            }

            if (ModelElement.IsValue)
            {
                OptionView.Content = new ViewValue(new ViewModelElementValue(ModelElement.Element as ElementValue, ModelElement.ModelAction));
                OptionView.Visibility = Visibility.Visible;
            }
            else if (ModelElement.IsGauge)
            {
                OptionView.Content = new ViewGauge(new ViewModelElementGauge(ModelElement.Element as ElementGauge, ModelElement.ModelAction));
                OptionView.Visibility = Visibility.Visible;
                LabelCenter.Visibility = Visibility.Collapsed;
                LabelScale.Visibility = Visibility.Collapsed;
                ComboCenter.Visibility = Visibility.Collapsed;
                ComboScale.Visibility = Visibility.Collapsed;
            }
            else
            {
                OptionView.Content = null;
                OptionView.Visibility = Visibility.Collapsed;
                LabelCenter.Visibility = Visibility.Visible;
                LabelScale.Visibility = Visibility.Visible;
                ComboCenter.Visibility = Visibility.Visible;
                ComboScale.Visibility = Visibility.Visible;
            }

            if (ModelElement.IsImage)
            {
                LabelImage.Visibility = Visibility.Visible;
                GridImage.Visibility = Visibility.Visible;
                var bitmap = ModelElement.ImageSource;
                if (bitmap != null)
                {
                    if (bitmap.Width == bitmap.Height)
                        InputImage.Width = 72;
                    else
                        InputImage.Width = 100;
                    InputImage.Source = bitmap;
                   
                }
                LabelImageFile.Content = ModelElement.GetImageInfo();
                CheckboxImageBackground.IsChecked = ModelElement.DrawImageBackground;
                LabelColor.Visibility = ModelElement.DrawImageBackground ? Visibility.Visible : Visibility.Collapsed;
                LabelColorSelect.Visibility = ModelElement.DrawImageBackground ? Visibility.Visible : Visibility.Collapsed;
                ButtonColorClipboard.Visibility = ModelElement.DrawImageBackground ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                LabelImage.Visibility = Visibility.Collapsed;
                GridImage.Visibility = Visibility.Collapsed;
            }

            if (ModelElement.IsPrimitive)
            {
                LabelElementPrimitiveType.Visibility = Visibility.Visible;
                ComboPrimitiveType.Visibility = Visibility.Visible;

                if (ModelElement.Primitive != PrimitiveType.RECTANGLE_FILLED && ModelElement.Primitive != PrimitiveType.CIRCLE_FILLED)
                {
                    LabelLineSize.Visibility = Visibility.Visible;
                    PanelLineSize.Visibility = Visibility.Visible;
                    InputLineSize.Text = ModelElement.LineSize;
                }
                else
                {
                    LabelLineSize.Visibility = Visibility.Collapsed;
                    PanelLineSize.Visibility = Visibility.Collapsed;
                }

                if (ModelElement.Primitive == PrimitiveType.LINE)
                {
                    LabelStart.Visibility = Visibility.Visible;
                    LabelEnd.Visibility = Visibility.Visible;
                    LabelSize.Visibility = Visibility.Collapsed;
                    LabelPosition.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LabelStart.Visibility = Visibility.Collapsed;
                    LabelEnd.Visibility = Visibility.Collapsed;
                    LabelSize.Visibility = Visibility.Visible;
                    LabelPosition.Visibility = Visibility.Visible;
                }
            }
            else
            {
                LabelElementPrimitiveType.Visibility = Visibility.Collapsed;
                ComboPrimitiveType.Visibility = Visibility.Collapsed;
                LabelStart.Visibility = Visibility.Collapsed;
                LabelEnd.Visibility = Visibility.Collapsed;
                LabelSize.Visibility = Visibility.Visible;
                LabelPosition.Visibility = Visibility.Visible;
                LabelLineSize.Visibility = Visibility.Collapsed;
                PanelLineSize.Visibility = Visibility.Collapsed;
            }

            InputTransparency.Text = ModelElement.Transparency;
        }

        private void SetTextAlignButtons()
        {
            ButtonTextAlignmentHorizontalLeft.IsHitTestVisible = !ModelElement.IsHorizontalLeft;
            SetAlignBorder(ButtonTextAlignmentHorizontalLeft, ModelElement.IsHorizontalLeft);
            ButtonTextAlignmentHorizontalCenter.IsHitTestVisible = !ModelElement.IsHorizontalCenter;
            SetAlignBorder(ButtonTextAlignmentHorizontalCenter, ModelElement.IsHorizontalCenter);
            ButtonTextAlignmentHorizontalRight.IsHitTestVisible = !ModelElement.IsHorizontalRight;
            SetAlignBorder(ButtonTextAlignmentHorizontalRight, ModelElement.IsHorizontalRight);

            ButtonTextAlignmentVerticalTop.IsHitTestVisible = !ModelElement.IsVerticalTop;
            SetAlignBorder(ButtonTextAlignmentVerticalTop, ModelElement.IsVerticalTop);
            ButtonTextAlignmentVerticalCenter.IsHitTestVisible = !ModelElement.IsVerticalCenter;
            SetAlignBorder(ButtonTextAlignmentVerticalCenter, ModelElement.IsVerticalCenter);
            ButtonTextAlignmentVerticalBottom.IsHitTestVisible = !ModelElement.IsVerticalBottom;
            SetAlignBorder(ButtonTextAlignmentVerticalBottom, ModelElement.IsVerticalBottom);
        }
        
        private void SetAlignBorder(Button button, bool state)
        {
            if (state)
            {
                button.BorderBrush = AlignmentBorderSelected;
                button.BorderThickness = new Thickness(1.5);
            }
            else
                button.BorderBrush = AlignmentBorder;
        }

        private void InputName_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetName(InputName.Text);
        }

        private void InputName_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetName(InputName.Text);
        }

        private void InputPosX_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPosX(InputPosX.Text);
        }

        private void InputPosX_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetPosX(InputPosX.Text);
        }

        private void InputPosY_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPosY(InputPosY.Text);
        }

        private void InputPosY_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetPosY(InputPosY.Text);
        }

        private void InputWidth_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetWidth(InputWidth.Text);
        }

        private void InputWidth_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetWidth(InputWidth.Text);
        }

        private void InputHeight_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetHeight(InputHeight.Text);
        }

        private void InputHeight_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetHeight(InputHeight.Text);
        }

        private void ComboCenter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboCenter.SelectedValue is CenterType type)
                ModelElement.SetCenter(type);
        }

        private void ComboScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboScale.SelectedValue is ScaleType type)
                ModelElement.SetScale(type);
        }        

        private void InputRotation_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetRotation(InputRotation.Text);
        }

        private void InputRotation_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetRotation(InputRotation.Text);
        }

        private void LabelColorSelect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.ColorDialog colorDialog = new()
            {
                Color = ModelElement.ColorForms,
                CustomColors = ColorStore.ColorArray
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ColorStore.AddDialogColors(colorDialog.CustomColors, colorDialog.Color);
                ModelElement.SetColor(colorDialog.Color);
            }
        }

        private void LabelFontSelect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FontDialog fontDialog = new()
            {
                Font = ModelElement.FontForms
            };

            if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                ModelElement.SetFont(fontDialog.Font);
        }

        private void InputImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DialogImage dialog = new(ModelElement.ImageFile ?? "", ParentWindow);
            if (dialog.ShowDialog() == true)
                ModelElement.SetImage(dialog.ImageResult);
        }

        private void InputElementText_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetText(InputElementText.Text);
        }

        private void InputElementText_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetText(InputElementText.Text);
        }

        private void ButtonPosReset_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPos(new Point(0, 0));
        }

        private void ButtonPosLeft_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPos(new Point(ModelElement.Pos.X - 1, ModelElement.Pos.Y));
        }

        private void ButtonPosRight_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPos(new Point(ModelElement.Pos.X + 1, ModelElement.Pos.Y));
        }

        private void ButtonPosUp_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPos(new Point(ModelElement.Pos.X, ModelElement.Pos.Y - 1));
        }

        private void ButtonPosDown_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetPos(new Point(ModelElement.Pos.X, ModelElement.Pos.Y + 1));
        }

        private void ButtonSizeWidthInc_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetSize(new Point(ModelElement.Size.X + 1, ModelElement.Size.Y));
        }

        private void ButtonSizeWidthDec_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetSize(new Point(ModelElement.Size.X - 1, ModelElement.Size.Y));
        }

        private void ButtonSizeHeightDec_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetSize(new Point(ModelElement.Size.X, ModelElement.Size.Y - 1));
        }

        private void ButtonSizeHeightInc_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetSize(new Point(ModelElement.Size.X, ModelElement.Size.Y + 1));
        }

        private void ButtonSizeReset_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetSize(new Point(0, 0));
        }

        private void ButtonRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetRotation(ModelElement.RotationNum - 1);
        }

        private void ButtonRotateRight_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetRotation(ModelElement.RotationNum + 1);
        }

        private void InputTransparency_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTransparency(InputTransparency.Text);
        }

        private void InputTransparency_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetTransparency(InputTransparency.Text);
        }

        private void ComboPrimitiveType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Refreshing)
                return;

            if (ComboPrimitiveType.SelectedValue is PrimitiveType type)
                ModelElement.SetPrimitiveType(type);
        }

        private void InputLineSize_LostFocus(object sender, RoutedEventArgs e)
        {
            ModelElement.SetLineSize(InputLineSize.Text);
        }

        private void InputLineSize_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ModelElement.SetLineSize(InputLineSize.Text);
        }

        private void ButtonColorClipboard_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.COLOR, ButtonColorClipboard, ModelElement.SetColor, ModelElement.Element.Settings.GetColor);
        }

        private void ButtonFontClipboard_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.FONT, ButtonFontClipboard, ModelElement.SetFontSettings, ModelElement.GetFontSettings);
        }

        private void ButtonPosClipboard_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.POS, ButtonPosClipboard, ModelElement.SetPosF, ModelElement.Element.Settings.GetPosition);
        }

        private void ButtonSizeClipboard_Click(object sender, RoutedEventArgs e)
        {
            SettingItem.Clipboard_Click(SettingType.SIZE, ButtonSizeClipboard, ModelElement.SetSizeF, ModelElement.Element.Settings.GetSize);
        }

        private void ButtonFontInc_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetFontSize(ModelElement.FontSize + 1);
        }

        private void ButtonFontDec_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetFontSize(ModelElement.FontSize - 1);
        }

        private void ButtonLineDec_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetLineSize(ModelElement.LineSizeNum - 1);
        }

        private void ButtonLineInc_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetLineSize(ModelElement.LineSizeNum + 1);
        }

        private void CheckboxImageBackground_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetImageBackground(CheckboxImageBackground.IsChecked == true);
        }

        private void ButtonTextAlignmentHorizontalLeft_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTextHorizontalAlignment(System.Drawing.StringAlignment.Near);
        }

        private void ButtonTextAlignmentHorizontalCenter_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTextHorizontalAlignment(System.Drawing.StringAlignment.Center);
        }

        private void ButtonTextAlignmentHorizontalRight_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTextHorizontalAlignment(System.Drawing.StringAlignment.Far);
        }

        private void ButtonTextAlignmentVerticalTop_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTextVerticalAlignment(System.Drawing.StringAlignment.Near);
        }

        private void ButtonTextAlignmentVerticalCenter_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTextVerticalAlignment(System.Drawing.StringAlignment.Center);
        }

        private void ButtonTextAlignmentVerticalBottom_Click(object sender, RoutedEventArgs e)
        {
            ModelElement.SetTextVerticalAlignment(System.Drawing.StringAlignment.Far);
        }
    }
}
