using CFIT.AppLogger;
using CFIT.AppTools;
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
        public static bool IsClosing { get; protected set; } = false;

        public DeveloperView(NotifyIconViewModel notifyModel)
        {
            NotifyModel = notifyModel;
            InitializeComponent();

            Title = $"{Title} ({VersionTools.GetEntryAssemblyVersion(3)}-{VersionTools.GetEntryAssemblyTimestamp()})";
            this.Closing += (_, _) => IsClosing = true;
        }

        protected void CheckVersion()
        {
            try
            {
                if (App.UpdateDetected)
                {
                    LabelVersionCheck.FontWeight = FontWeights.DemiBold;
                    LabelVersionCheck.FontSize = 14;
                    LabelVersionCheck.Text = "";
                    LabelVersionCheck.Inlines.Clear();
                    
                    if (App.UpdateIsDev)
                        LabelVersionCheck.Inlines.Add("New Develop Version ");
                    else
                        LabelVersionCheck.Inlines.Add("New Stable Version ");
                    var run = new Run($"{App.UpdateVersion}");

                    Hyperlink hyperlink;
                    if (App.UpdateIsDev)
                        hyperlink = new Hyperlink(run)
                        {
                            NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck/blob/master/PilotsDeck-Installer-latest.exe")
                        };
                    else
                        hyperlink = new Hyperlink(run)
                        {
                            NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck/releases/latest")
                        };
                    LabelVersionCheck.Inlines.Add(hyperlink);
                    LabelVersionCheck.Inlines.Add(" available!");
                    this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
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
            Logger.Debug($"Loading View '{newView.GetType().Name}' ...");
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
            Logger.Debug($"View loaded.");
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
                Logger.Debug($"GUI Hidden");
            }
            else
            {
                Logger.Debug($"GUI Visible");
                MaxHeight = SystemParameters.PrimaryScreenHeight;
                Topmost = true;
                Focus();
                Topmost = false;
                LoadView(new ViewReference());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NotifyIconViewModel.ToggleWindow();
            if (!App.CloseReceived)
                e.Cancel = true;
        }

        private void ButtonMonitor_Click(object sender, RoutedEventArgs e)
        {
            LoadView(new ViewState());
        }

        private void ButtonReference_Click(object sender, RoutedEventArgs e)
        {
            LoadView(new ViewReference());
        }

        private void ButtonProfileManager_Click(object sender, RoutedEventArgs e)
        {
            NotifyIconViewModel.OpenProfileManager();
            NotifyIconViewModel.ToggleWindow();
        }
    }
}
