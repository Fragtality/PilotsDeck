using PilotsDeck.Actions;
using PilotsDeck.Plugin;
using PilotsDeck.Resources;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PilotsDeck.UI.DeveloperUI
{
    public partial class ViewState : UserControl, IView
    {
        protected DispatcherTimer RefreshTimer { get; set; }
        protected int LineCounter { get; set; } = 0;
        protected static SimController SimController { get { return App.SimController; } }
        protected static PluginController PluginController { get { return App.PluginController; } }
        protected static ActionManager ActionManager { get { return App.PluginController.ActionManager; } }
        protected static VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        protected static ImageManager ImageManager { get { return App.PluginController.ImageManager; } }
        protected static ScriptManager ScriptManager { get { return App.PluginController.ScriptManager; } }

        public ViewState()
        {
            InitializeComponent();
            RefreshTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            RefreshTimer.Tick += RefreshTimer_Tick;
        }

        protected void UpdateControls()
        {
            LabelPluginState.Content = PluginController.State;
            var query = SimController.ActiveConnectors.Where(c => c.Value.IsPrimary).Select(c => c.Key);
            LabelConnectorsPrimary.Content = string.Join(',', query);
            query = SimController.ActiveConnectors.Where(c => !c.Value.IsPrimary).Select(c => c.Key);
            LabelConnectorsSecondary.Content = string.Join(',', query);

            LabelSimState.Content = SimController.SimState;
            LabelSimLoading.Content = SimController.IsLoading;
            LabelReadyProcess.Content = SimController.IsReadyProcess;
            
            LabelSessionRunning.Content = SimController.IsReadySession;
            LabelSimPaused.Content = SimController.IsPaused;
            LabelReadyCommands.Content = SimController.IsReadyCommand;

            
            LabelMappingsCount.Content = ActionManager.ProfileSwitcherManager.ProfileMappings.Count;
            LabelProfileSwitch.Content = ActionManager.ProfileSwitcherManager.GlobalSettings.EnableSwitching;
            LabelProfileSwitchback.Content = ActionManager.ProfileSwitcherManager.GlobalSettings.SwitchBack;
            
            LabelAircraftString.Content = SimController.AircraftString;

           
            LabelActions.Content = ActionManager.Count;
            LabelVariables.Content = $"{VariableManager.ManagedVariables.Count} / {VariableManager.ManagedVariables.Values.Count(v => v.Registrations > 0)} / {VariableManager.ManagedVariables.Values.Count(v => v.IsSubscribed)}";
            LabelImages.Content = ImageManager.Count;
            
            LabelScripts.Content = ScriptManager.Count;
            LabelScriptsGlobal.Content = ScriptManager.CountGlobal;
            LabelScriptsImage.Content = ScriptManager.CountImages;

            
            LabelRedraws.Content = StatisticManager.Redraws;
            StringBuilder sb = new();
            sb.AppendLine(string.Format("Timer {0:00}/{1:00}s", (DateTime.Now - StatisticManager.LastTick).TotalSeconds, App.Configuration.IntervalUnusedRessources/1000.0));
            foreach (var tracker in StatisticManager.Tracker.Values)
                    sb.AppendLine(tracker.GetStatistics());
            LabelStatistics.Content = sb.ToString();


            while (!Logger.Messages.IsEmpty)
            {

                if (LineCounter > 14)
                    LabelLog.Text = LabelLog.Text[(LabelLog.Text.IndexOf('\n') + 1)..];
                LabelLog.Text += (LineCounter > 0 ? "\n" : "") + Logger.Messages.Dequeue().ToString();
                LineCounter++;
            }
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (App.CancellationToken.IsCancellationRequested)
                RefreshTimer.Stop();

            UpdateControls();
        }

        public void Start()
        {
            RefreshTimer.Start();
        }

        public void Stop()
        {
            RefreshTimer?.Stop();
        }

        private static void RessourceWindow(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var scrollView = new ScrollViewer
            {
                Content = content,
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };
            var window = new Window
            {
                Title = title,
                Content = scrollView,
                Top = App.DeveloperView.Top + (App.DeveloperView.ActualHeight / 2.0) - 160,
                Left = App.DeveloperView.Left + (App.DeveloperView.ActualWidth / 2.0) - 160,
                SizeToContent = SizeToContent.WidthAndHeight,
                MaxHeight = SystemParameters.PrimaryScreenHeight - 256,
                MinWidth = 256,
            };

            window.ShowDialog();
        }

        private void LabelVariables_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StringBuilder sb = new();
            sb.AppendLine(string.Format("{0,-6} | {1,-5} | {2,-6} | {3}", "Sub", "Reg", "Abnrm", "Name"));
            foreach (var variable in VariableManager.VariableList)
            {
                bool abnorm = variable.IsSubscribed && variable.Registrations <= 0;
                sb.AppendLine(string.Format("{0,-6} | {1,-5} | {2,-6} | {3}", variable.IsSubscribed, variable.Registrations, (abnorm ? "*" : ""), variable.Address));
            }
            RessourceWindow("Variables", sb.ToString());
        }

        private void LabelImages_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RessourceWindow("Images", string.Join(Environment.NewLine, ImageManager.ImageCache.Values));
        }

        private void LabelScripts_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RessourceWindow("Scripts", string.Join(Environment.NewLine, ScriptManager.ManagedScripts.Values));
        }

        private void LabelScriptsGlobal_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RessourceWindow("Global Scripts", string.Join(Environment.NewLine, ScriptManager.ManagedGlobalScripts.Values));
        }

        private void LabelScriptsImage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RessourceWindow("Image Scripts", string.Join(Environment.NewLine, ScriptManager.ManagedImageScripts.Values));
        }
    }
}
