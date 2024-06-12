using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProfileManager
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        protected Brush BrushDefault { get; set; }
        protected Brush BrushHighlight { get; } = SystemColors.HighlightBrush;

        public static string AppTitle { get; protected set; }

        public MainWindow()
        {
            try
            {
                Instance = this;
                InitializeComponent();
                BrushDefault = ButtonProfileInstaller.BorderBrush;

                string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                assemblyVersion = assemblyVersion[0..assemblyVersion.LastIndexOf('.')];
                Title += " (" + assemblyVersion + ")";
                AppTitle = Title;

                ExecuteCommandLine();
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
                Logger.Log(LogLevel.Error, $"Profile Mapper requested while Apps still running!");
                MessageBox.Show("The StreamDeck Software is still running:\r\nCan not clean Profile Flags while StreamDeck Software is active.", "StreamDeck Running", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ProfileController tempController = new();
            tempController.CleanProfileManifestFlag();

            ButtonProfileMapper_Click(null, null);
        }

        private void ShowInstallerView(string filename = null)
        {
            ButtonEnable(ButtonProfileMapper);
            ButtonDisable(ButtonProfileInstaller);
            var view = new ViewProfileInstaller();
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

        private void ButtonProfileInstaller_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LabelDesc.Text = "";
                LabelDesc.Visibility = Visibility.Collapsed;

                if (ContentArea.Content is ViewProfileMapper && (ContentArea.Content as ViewProfileMapper).ProfileController.HasChanges && (ContentArea.Content as ViewProfileMapper).IsSaveStateValid())
                {
                    Logger.Log(LogLevel.Warning, $"Close requested with unsaved Changes");
                    var result = MessageBox.Show("There are unsaved Changes to your Profiles!\r\nSave before closing?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        (ContentArea.Content as ViewProfileMapper).ProfileController.SaveChanges();
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
                    Logger.Log(LogLevel.Warning, $"Profile Mapper requested while Profile Installation in Progress!");
                    var result = MessageBox.Show("A Profile Package is currently opened for Installation!\r\nCancel Installation?", "Profile Package Loaded", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                        return;
                    else
                        (ContentArea.Content as ViewProfileInstaller).SetStateOpenPackage();
                }

                if (ProfileController.AppsRunning)
                {
                    Logger.Log(LogLevel.Warning, $"Profile Mapper requested while Apps still running!");
                    var result = MessageBox.Show("Can not edit Profiles while StreamDeck is running:\r\nKill StreamDeck Software now?", "StreamDeck Running", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        var tempTaskPanel = new InstallerTaskPanel();
                        ContentArea.Content = tempTaskPanel;
                        tempTaskPanel.Activate(null, null);
                        var task = InstallerTask.AddTask("Stop StreamDeck Software", "");

                        await Tools.StopStreamDeckSoftware();
                        await Tools.WaitOnTask(task, "Stop StreamDeck Software and wait {0}s", 3);
                        tempTaskPanel.Deactivate();
                    }
                    else
                        Logger.Log(LogLevel.Warning, $"Continued with StreamDeck running");
                }

                ButtonEnable(ButtonProfileInstaller);
                ButtonDisable(ButtonProfileMapper);
                ContentArea.Content = new ViewProfileMapper();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ContentArea.Content is ViewProfileMapper)
                (ContentArea.Content as ViewProfileMapper)?.Window_Closing();
            if (ContentArea.Content is ViewProfileInstaller)
                (ContentArea.Content as ViewProfileInstaller)?.Dispose();
        }
    }
}