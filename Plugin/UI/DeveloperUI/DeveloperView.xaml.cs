using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
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
        protected static Dictionary<string, IView> Views { get; } = [];

        public DeveloperView(NotifyIconViewModel notifyModel)
        {
            NotifyModel = notifyModel;
            InitializeComponent();

            Title = $"{Title} ({VersionTools.GetEntryAssemblyVersion(3)}-{VersionTools.GetEntryAssemblyTimestamp()})";
            Views.Add(nameof(ViewState), new ViewState());
            Views.Add(nameof(ViewReference), new ViewReference());
            Views.Add(nameof(ViewSettings), new ViewSettings());
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

                    if (App.IsUpdateVersion)
                        LabelVersionCheck.Inlines.Add("New Plugin Version ");
                    else
                        LabelVersionCheck.Inlines.Add("New Plugin Build ");
                    var run = new Run($"{App.OnlineVersion}");

                    Hyperlink hyperlink = new(run)
                    {
                        NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck/releases/latest")
                    };
                    LabelVersionCheck.Inlines.Add(hyperlink);
                    LabelVersionCheck.Inlines.Add(" available!");
                    this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
                    LabelVersionCheck.Visibility = Visibility.Visible;

                    this.Icon = App.GetIcon().ToImageSource();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected void LoadView(string viewName)
        {
            Logger.Debug($"Loading View '{viewName}' ...");
            if (CurrentView != null)
            {
                CurrentView?.Stop();
                CurrentView = null;
            }

            CheckVersion();

            CurrentView = Views[viewName];
            PanelView.Content = CurrentView as UserControl;
            CurrentView.Start();

            SetMenuButton(ButtonMonitor, CurrentView is ViewState);
            SetMenuButton(ButtonReference, CurrentView is ViewReference);
            SetMenuButton(ButtonSettings, CurrentView is ViewSettings);
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
                LoadView(nameof(ViewReference));
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
            LoadView(nameof(ViewState));
        }

        private void ButtonReference_Click(object sender, RoutedEventArgs e)
        {
            LoadView(nameof(ViewReference));
        }

        private void ButtonProfileManager_Click(object sender, RoutedEventArgs e)
        {
            NotifyIconViewModel.OpenProfileManager();
            NotifyIconViewModel.ToggleWindow();
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadView(nameof(ViewSettings));
        }
    }
}
