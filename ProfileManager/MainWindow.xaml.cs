using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Tasks;
using CFIT.Installer.UI.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProfileManager
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public static string AppTitle { get; protected set; }

        protected Brush BrushDefault { get; set; }
        protected Brush BrushHighlight { get; } = SystemColors.HighlightBrush;
        protected bool StoppedStreamDeck { get; set; } = false;

        public MainWindow()
        {
            try
            {
                Instance = this;
                InitializeComponent();
                BrushDefault = ButtonProfileInstaller.BorderBrush;
                FuncStreamDeck.PluginBinary = Config.PluginBinary;

                Title = $"{Title} ({VersionTools.GetEntryAssemblyVersion(3)}-{VersionTools.GetEntryAssemblyTimestamp()})";
                AppTitle = Title;

                ExecuteCommandLine();
                ButtonProfileInstaller_Click(null, null);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void ExecuteCommandLine()
        {
            foreach (var pair in App.CommandLineArgs)
            {
                if (pair.Value == null && !string.IsNullOrEmpty(Path.GetExtension(pair.Key)))
                    ShowInstallerView(pair.Key);
                else if (pair.Key == "cleanprofiles")
                {
                    ExecuteCleanProfiles();
                    App.Current.Shutdown();
                }
            }
        }

        private void ExecuteCleanProfiles()
        {
            if (ProfileController.AppsRunning)
            {
                Logger.Error($"Profile Mapper requested while Apps still running!");
                MessageBox.Show("The StreamDeck Software is still running:\r\nCan not clean Profile Flags while StreamDeck Software is active.", "StreamDeck Running", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileController tempController = new();
            tempController.CleanProfileManifestFlag();

            ButtonProfileMapper_Click(null, null);
        }

        public static void SetForeground()
        {
            Sys.SetForegroundWindow(AppTitle);
        }

        private void ShowInstallerView(string filename = null)
        {
            ButtonEnable(ButtonProfileMapper);
            ButtonDisable(ButtonProfileInstaller);
            var view = new ViewProfileInstaller();
            ContentArea.CanContentScroll = true;
            ContentArea.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            ContentArea.Content = view;
            
            if (filename != null)
                view.OpenPackageFile(filename);
        }

        private void ButtonEnable(Button button)
        {
            button.IsHitTestVisible = true;
            button.BorderBrush = BrushDefault;
            button.BorderThickness = new Thickness(1);
            button.Opacity = 1.0;
        }

        private void ButtonDisable(Button button)
        {
            button.IsHitTestVisible = false;
            button.BorderBrush = BrushHighlight;
            button.BorderThickness = new Thickness(2);
            button.Opacity = 0.5;
        }

        private async void ButtonProfileInstaller_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LabelDesc.Text = "";
                LabelDesc.Visibility = Visibility.Collapsed;

                if (ContentArea.Content is ViewProfileMapper && (ContentArea.Content as ViewProfileMapper).ProfileController.HasChanges && (ContentArea.Content as ViewProfileMapper).IsSaveStateValid())
                {
                    Logger.Warning($"Close requested with unsaved Changes");
                    var result = MessageBox.Show("There are unsaved Changes to your Profiles!\r\nSave before closing?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        (ContentArea.Content as ViewProfileMapper).ProfileController.SaveChanges();
                }

                if (StoppedStreamDeck)
                {
                    Logger.Debug($"Starting StreamDeck after Stop");
                    await StartStopStreamDeck(DeckProcessOperation.START);
                }

                ShowInstallerView();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private async void ButtonProfileMapper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LabelDesc.Text = "";
                LabelDesc.Visibility = Visibility.Collapsed;

                if (ContentArea.Content is ViewProfileInstaller && (ContentArea.Content as ViewProfileInstaller).IsPackageActive)
                {
                    Logger.Warning($"Profile Mapper requested while Profile Installation in Progress!");
                    var result = MessageBox.Show("A Profile Package is currently opened for Installation!\r\nCancel Installation?", "Profile Package Loaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;
                    else
                        (ContentArea.Content as ViewProfileInstaller).SetStateOpenPackage();
                }

                if (ProfileController.AppsRunning)
                {
                    Logger.Debug($"Profile Mapper requested while Apps still running!");
                    var result = MessageBox.Show("Can not edit Profiles while StreamDeck is running:\r\nKill StreamDeck Software now?", "StreamDeck Running", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        Logger.Debug($"Stopping StreamDeck ...");
                        await StartStopStreamDeck(DeckProcessOperation.STOP);
                    }
                    else
                        Logger.Warning($"Continued with StreamDeck running");
                }

                ButtonEnable(ButtonProfileInstaller);
                ButtonDisable(ButtonProfileMapper);
                ContentArea.CanContentScroll = false;
                ContentArea.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                ContentArea.Content = new ViewProfileMapper();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected async Task StartStopStreamDeck(DeckProcessOperation operation)
        {
            TaskStore.Clear();
            var tempTaskPanel = new TaskViewPanel();
            ContentArea.Content = tempTaskPanel;
            tempTaskPanel.Activate();

            var worker = new WorkerStreamDeckStartStop<Config>(Config.Instance, operation);
            if (operation == DeckProcessOperation.START)
            {
                worker.RefocusWindow = true;
                worker.RefocusWindowTitle = MainWindow.AppTitle;
            }

            await worker.Run(System.Threading.CancellationToken.None);
            if (operation == DeckProcessOperation.STOP)
                StoppedStreamDeck = true;
            else
                StoppedStreamDeck = false;

            tempTaskPanel.Deactivate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (StoppedStreamDeck)
            {
                Logger.Information($"Starting StreamDeck Software");
                (new FuncStreamDeck()).StartSoftware();
            }

            if (ContentArea.Content is ViewProfileMapper)
                (ContentArea.Content as ViewProfileMapper)?.Window_Closing();
            if (ContentArea.Content is ViewProfileInstaller)
                (ContentArea.Content as ViewProfileInstaller)?.Dispose();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            MaxHeight = SystemParameters.FullPrimaryScreenHeight - 128;
        }
    }
}