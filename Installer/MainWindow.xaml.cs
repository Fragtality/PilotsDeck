using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Installer
{
    public partial class MainWindow : Window
    {
        private InstallerWorker InstallWorker { get; set; } = new InstallerWorker();
        private DispatcherTimer TimerWorkerCheck { get; } = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            this.Top = 42;

            var run = new Run("This Tool will install the PilotsDeck Plugin to your StreamDeck Software.\r\nThe Software will be stopped/started during the Installation-Process.\r\nAdded/Changed Profiles, Images and Scripts will stay intact.")
            {
                FontWeight = FontWeights.DemiBold
            };
            descLabel.Inlines.Add(run);
            run = new Run("\r\n\r\n\r\nNote: PilotsDeck is 100% free and Open-Source. The Software and the Developer do not have any Affiliation to Flight Panels.\r\nIt is the actual Plugin allowing the StreamDeck to interface with the Simulator and allowing the Creation of StreamDeck Profiles for Airplanes.");
            descLabel.Inlines.Add(run);


            Hyperlink link = new Hyperlink(new Run("\r\nPilotsDeck on GitHub"))
            {
                NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck")
            };
            descLabel.Inlines.Add(link);
            descLabel.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Tools.OpenUri));

            Title += $" ({Parameters.pilotsDeckVersion})";

            TaskPanel.Visibility = Visibility.Collapsed;

            bottomLabel.Visibility = Visibility.Collapsed;
            bottomLabel.Text = "The Plugin might be blocked by Security-/Anti-Virus-Software!\r\nTry to add an Exclusion on the Plugin Binary or Directory if you have Problems!";

            if (!InstallWorker.IsExistingInstallation)
                ButtonRemove.Visibility = Visibility.Collapsed;
            else
                ButtonRemove.Visibility = Visibility.Visible;

            TimerWorkerCheck.Interval = TimeSpan.FromMilliseconds(200);
            TimerWorkerCheck.Tick += TimerWorkerCheckTick;
            TaskPanel.DontStopOnError = true;
        }

        protected void SetButtonState(bool success, bool hideInstall, bool hideRemove = false, string caption = null, string file = null, string tooltip = null)
        {
            ButtonInstall.IsEnabled = success;

            if (hideInstall)
                ButtonInstall.Visibility = Visibility.Collapsed;
            else
                ButtonInstall.Visibility = Visibility.Visible;

            if (hideRemove)
                ButtonRemove.Visibility = Visibility.Collapsed;
            else
                ButtonRemove.Visibility = Visibility.Visible;

            if (caption != null)
                LabelButtonInstall.Text = caption;

            if (tooltip != null)
                ButtonInstall.ToolTip = tooltip;

            if (ImageButtonInstall != null && !string.IsNullOrEmpty(file))
                Tools.SetButtonImage(ImageButtonInstall, file);
        }

        protected void TimerWorkerCheckTick(object sender, EventArgs e)
        {
            if (!InstallWorker.IsRunning && InstallWorker.IsCompleted)
            {
                TimerWorkerCheck.Stop();
                if (InstallWorker.IsSuccess)
                    bottomLabel.Visibility= Visibility.Visible;
                else if (InstallWorker.IsRemoved)
                {
                    bottomLabel.Text = "Note: Profiles in your StreamDeck Software related to PilotsDeck have to be removed manually via the GUI!";
                    bottomLabel.Visibility = Visibility.Visible;
                }

                if (InstallWorker.IsSuccess || InstallWorker.IsRemoved)
                    SetButtonState(true, false, true, "Close", "check-square", "Close Installer");
                else
                    SetButtonState(true, false, true, "Close", "x-square", "Close Installer");
            }
        }

        private async void ButtonInstall_Click(object sender, RoutedEventArgs e)
        {
            if (!InstallWorker.IsRunning && !InstallWorker.IsCompleted)
            {
                ButtonInstall.IsEnabled = false;
                ButtonRemove.IsEnabled = false;
                descLabel.Visibility = Visibility.Collapsed;
                TaskPanel.Visibility = Visibility.Visible;
                TimerWorkerCheck.Start();
                TaskPanel.Activate(null, null);
                await Task.Run(InstallWorker.Run);
            }
            
            if (ButtonInstall.IsEnabled && !InstallWorker.IsRunning && InstallWorker.IsCompleted)
            {
                this.Close();
            }
        }

        private async void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonRemove.IsEnabled && ButtonRemove.Visibility == Visibility.Visible && !InstallWorker.IsRunning)
            {
                if (MessageBox.Show("The complete Plugin Folder with all custom Content (Profiles, Images, Scripts) will be permanently deleted!\r\n" +
                    "Are you sure to continue?", "Remove Plugin", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                    return;

                ButtonInstall.IsEnabled = false;
                ButtonRemove.IsEnabled = false;
                descLabel.Visibility = Visibility.Collapsed;
                TaskPanel.Visibility = Visibility.Visible;
                TimerWorkerCheck.Start();
                TaskPanel.Activate(null, null);
                await Task.Run(InstallWorker.RemovePlugin);
            }
        }
    }
}
