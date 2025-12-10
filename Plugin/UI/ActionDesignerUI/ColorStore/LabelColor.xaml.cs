using ColorPicker;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.ColorStore
{
    public partial class LabelColor : UserControl
    {
        public static readonly double DefaultWidth = 20;
        public static readonly double DefaultHeight = 20;
        public static readonly Thickness DefaultMargin = new(1);
        public static readonly Thickness DefaultBorderThickness = new(1);
        public static readonly Thickness SelectedBorderThickness = new(1.5);
        public static readonly SolidColorBrush DefaultBorderColor = new(Colors.Black);
        public static readonly SolidColorBrush DefaultSelectedColor = SystemColors.HighlightBrush;

        protected virtual PickerControlBase PickerControl { get; }
        protected virtual ColorGrid Grid { get; }
        public virtual Color Color { get; }

        public LabelColor(Color labelColor, PickerControlBase pickerControl, ColorGrid grid)
        {
            InitializeComponent();
            Color = labelColor;
            PickerControl = pickerControl;
            Grid = grid;

            Label.Background = new SolidColorBrush(Color);
            Label.Width = DefaultWidth;
            Label.MinWidth = DefaultWidth;
            Label.MaxWidth = DefaultWidth;
            Label.Height = DefaultHeight;
            Label.MinHeight = DefaultHeight;
            Label.MaxHeight = DefaultHeight;
            Label.BorderThickness = DefaultBorderThickness;
            Label.BorderBrush = DefaultBorderColor;
            Label.Margin = DefaultMargin;
            Label.MouseLeftButtonDown += (s, e) =>
            {
                PickerControl.SelectedColor = Color;
                Grid.SetSelection(this);
            };
            Grid = grid;
        }

        public void SetSelection()
        {
            Label.BorderBrush = DefaultSelectedColor;
            Label.BorderThickness = SelectedBorderThickness;
        }

        public void ClearSelection()
        {
            Label.BorderBrush = DefaultBorderColor;
            Label.BorderThickness = DefaultBorderThickness;
        }
    }
}
