using CFIT.AppTools;
using PilotsDeck.UI.ActionDesignerUI.ViewModels.Address;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PilotsDeck.UI.ActionDesignerUI.Views.Controls
{
    public partial class ControlAddress : UserControl
    {
        public virtual SolidColorBrush BrushValid { get; } = new(Colors.Green);
        public virtual SolidColorBrush BrushInvalid { get; } = new(Colors.Red);
        public virtual ViewModelAddress ViewModel { get; }
        protected virtual DispatcherTimer RefreshTimer { get; set; }

        public ControlAddress(ViewModelAddress model)
        {
            InitializeComponent();
            ViewModel = model;
            this.DataContext = ViewModel;
            ViewModel.SubscribeProperty(nameof(ViewModel.Address), CheckAddress);

            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalUiRefresh)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
            this.Loaded += (_, _) => RefreshTimer?.Start();
            this.Unloaded += (_, _) => RefreshTimer?.Stop();
            
            InitializeControls();
        }

        protected virtual void InitializeControls()
        {
            InputAddress.Text = ViewModel.Address;
            InputAddress.BorderThickness = new Thickness(1);
            CheckAddress();

            InputAddress.GotFocus += InputAddress_GotFocus;
            InputAddress.LostFocus += InputAddress_LostFocus;
            InputAddress.KeyUp += InputAddress_KeyUp;
        }

        protected virtual void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
                RefreshTimer.Stop();

            UpdateValue();
        }

        protected virtual void UpdateValue()
        {
            if (ViewModel.HasValue && ViewModel.CheckAddressValid())
                ViewModel.LabelValue = $"Current Value: {ViewModel?.Value ?? "0"}";
            else
                ViewModel.LabelValue = "";
        }

        protected virtual void CheckAddress()
        {
            if (ViewModel.CheckAddressValid())
            {
                ViewModel.LabelInputCheck = "";
                ViewModel.LabelType = $"Type: {ViewModel?.DisplayType}";
                InputAddress.BorderBrush = BrushValid;
            }
            else
            {
                ViewModel.LabelInputCheck = "Invalid Syntax";
                ViewModel.LabelType = "";
                InputAddress.BorderBrush = BrushInvalid;
            }

            UpdateValue();
        }

        protected virtual void CheckInput()
        {
            if (ViewModel.CheckInputValid(InputAddress?.Text, out Enum detectedType))
            {
                ViewModel.LabelInputCheck = $"Valid {detectedType}";
                InputAddress.BorderBrush = BrushValid;
            }
            else
            {
                ViewModel.LabelInputCheck = "Invalid Syntax";
                InputAddress.BorderBrush = BrushInvalid;
            }
            ViewModel.LabelType = "";
        }

        protected virtual void SetInput()
        {
            ViewModel.Address = InputAddress.Text;
            CheckAddress();
        }

        protected virtual void InputAddress_GotFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.IsTyping = true;
            CheckInput();
        }

        protected virtual void InputAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            if (InputAddress?.Text != null)
                SetInput();
        }

        protected virtual void InputAddress_KeyUp(object sender, KeyEventArgs e)
        {
            if (Sys.IsEnter(e) && InputAddress?.Text != null)
                SetInput();
            else
            {
                ViewModel.IsTyping = true;
                CheckInput();
            }
        }
    }
}
