using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PilotsDeck.UI.ActionDesignerUI.ColorStore
{
    public partial class DialogColor : Window
    {
        public virtual Window ParentWindow { get; set; }
        public virtual ColorGrid StandardColors { get; }
        public virtual ColorGrid RecentColors { get; }
        public virtual ColorGrid CustomColors { get; }
        public virtual System.Drawing.Color SelectedColor => ColorPicker?.SelectedColor.Convert() ?? System.Drawing.Color.White;
        protected virtual Color InitialColor { get; }

        public DialogColor(Color current, Window parent = null)
        {
            InitializeComponent();
            ParentWindow = parent;
            InitialColor = current;
            this.Activated += OnActivated;

            StandardColors = new(GridStandardColors, ColorPicker);
            RecentColors = new(GridRecentColors, ColorPicker);
            CustomColors = new(GridUserColors, ColorPicker);

            ButtonAdd.Click += ButtonAddOnClick;
            ButtonRemove.Click += ButtonRemoveOnClick;
        }

        protected virtual void OnActivated(object? sender, System.EventArgs e)
        {
            ColorPicker?.SelectedColor = InitialColor;
            LabelPreview.Background = new SolidColorBrush(InitialColor);
            ColorPicker.ColorChanged += OnPickerColorChange;

            var colors = typeof(Colors)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(p => (Color?)p.GetValue(null) ?? Colors.White)
                .ToList();
            StandardColors.SetColors(colors);
            RecentColors.SetColors(ColorStoreManager.GetRecentColors());
            CustomColors.SetColors(ColorStoreManager.GetCustomColors());

            var grid = TextBoxHex.Content as Grid;
            var text = grid.Children[0] as TextBox;
            text.Height = 24;
            text.Width = 72;
            text.HorizontalContentAlignment = HorizontalAlignment.Center;
            text.VerticalContentAlignment = VerticalAlignment.Center;
            text.KeyUp += (sender, e) =>
            {
                if (Sys.IsEnter(e))
                {
                    ModelExtensions.UpdateBindingTextSource(text);
                    Keyboard.ClearFocus();
                    ButtonOK.Focus();
                }
            };

            if (ParentWindow != null)
            {
                this.Top = ParentWindow.Top + (ParentWindow.ActualHeight / 2.0) - (this.ActualHeight / 2.0);
                this.Left = ParentWindow.Left + (ParentWindow.ActualWidth / 2.0) - (this.ActualWidth / 2.0);
            }

            ButtonOK.Focus();
        }

        protected virtual void ButtonAddOnClick(object sender, RoutedEventArgs e)
        {
            ColorStoreManager.AddCustomColor(ColorPicker.SelectedColor);
            CustomColors.SetColors(ColorStoreManager.GetCustomColors());
        }

        protected virtual void ButtonRemoveOnClick(object sender, RoutedEventArgs e)
        {
            if (CustomColors.HasSelection)
            {
                ColorStoreManager.RemoveCustomColor(ColorPicker.SelectedColor);
                CustomColors.SetColors(ColorStoreManager.GetCustomColors());
            }
        }

        protected virtual void OnPickerColorChange(object sender, RoutedEventArgs e)
        {
            LabelPreview.Background = new SolidColorBrush(ColorPicker.SelectedColor);
            StandardColors.ClearSelection();
            RecentColors.ClearSelection();
            CustomColors.ClearSelection();
        }

        protected virtual void ButtonOkOnClick(object sender, RoutedEventArgs e)
        {
            if (ColorPicker?.SelectedColor != null)
            {
                DialogResult = true;
                ColorStoreManager.AddRecentColor(ColorPicker.SelectedColor);
            }
            else
                DialogResult = false;

            Close();
        }

        protected virtual void ButtonCancelOnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
