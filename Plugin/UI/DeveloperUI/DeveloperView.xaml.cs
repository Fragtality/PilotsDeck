using PilotsDeck.Tools;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace PilotsDeck.UI.DeveloperUI
{
    public partial class DeveloperView : Window
    {
        protected NotifyIconViewModel NotifyModel { get; set; }
        protected IView CurrentView { get; set; } = null;
        protected static Thickness ThicknessSelected { get; set; } = new Thickness(1.5);
        protected static Thickness ThicknessDefault { get; set; } = new Thickness(1);

        public DeveloperView(NotifyIconViewModel notifyModel)
        {
            NotifyModel = notifyModel;
            InitializeComponent();
        }

        protected void CheckVersion()
        {
            try
            {
                if (App.UpdateDetected)
                {
                    LabelVersionCheck.FontWeight = FontWeights.DemiBold;
                    LabelVersionCheck.FontSize = 14;
                    LabelVersionCheck.Visibility = Visibility.Visible;
                    LabelVersionCheck.Inlines.Add("New Version ");
                    var run = new Run($"{App.UpdateVersion}");
                    var hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck/releases/latest")
                    };
                    LabelVersionCheck.Inlines.Add(hyperlink);
                    LabelVersionCheck.Inlines.Add(" available!");
                    this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Sys.RequestNavigateHandler));

                    LabelVersionCheck.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void LoadView(IView newView)
        {
            if (CurrentView != null)
            {
                CurrentView?.Stop();
                CurrentView = null;
            }
            
            CheckVersion();

            CurrentView = newView;
            PanelView.Content = CurrentView as UserControl;
            CurrentView.Start();

            SetMenuButton(ButtonMonitor, CurrentView is ViewState);
            SetMenuButton(ButtonReference, CurrentView is ViewReference);
        }

        protected static void SetMenuButton(Button button, bool selected)
        {
            button.IsHitTestVisible = !selected;
            button.BorderBrush = selected ? SystemColors.HighlightBrush : SystemColors.WindowFrameBrush;
            button.BorderThickness = selected ? ThicknessSelected : ThicknessDefault;
        }

        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                CurrentView?.Stop();
                CurrentView = null;
                PanelView.Content = null;
                Logger.Verbose($"GUI Hidden");
            }
            else
            {
                Logger.Verbose($"GUI Visible");
                MaxHeight = SystemParameters.PrimaryScreenHeight;
                Topmost = true;
                Focus();
                Topmost = false;
                LoadView(new ViewReference());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void ButtonMonitor_Click(object sender, RoutedEventArgs e)
        {
            LoadView(new ViewState());
        }

        private void ButtonReference_Click(object sender, RoutedEventArgs e)
        {
            LoadView(new ViewReference());
        }
    }
}
