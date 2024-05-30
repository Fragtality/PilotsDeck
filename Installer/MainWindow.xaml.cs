using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Installer
{
    public partial class MainWindow : Window
    {
        private readonly InstallerWorker worker;
        private readonly Queue<InstallerTask> taskQueue;
        private readonly List<InstallerActionControl> actionControls;
        private readonly DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

            descLabel.Text = "This Tool will install the PilotsDeck StreamDeck Plugin on your System.\r\nYour StreamDeck Software will be stopped during the Installation-Process.\r\nAdded/Changed Profiles and added Images will stay intact.\r\n\r\nNote: PilotsDeck is 100% free and Open-Source. The Software and the Developer do not have any Affiliation to Flight Panels.\r\nIt is the actual Plugin allowing the StreamDeck to interface with the Simulator and allowing the Creation of StreamDeck Profiles for Airplanes.";
            Hyperlink link = new Hyperlink(new Run("\r\nPilotsDeck on GitHub"))
            {
                NavigateUri = new Uri("https://github.com/Fragtality/PilotsDeck")
            };
            descLabel.Inlines.Add(link);
            descLabel.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(RequestNavigateHandler));
            Title += $" ({Parameters.pilotsDeckVersion})";

            actionControls = new List<InstallerActionControl>();
            taskQueue = new Queue<InstallerTask>();
            worker = new InstallerWorker(taskQueue);
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            timer.Tick += OnTick;
        }

        protected void OnTick(object sender, EventArgs e)
        {
            foreach (var control in actionControls)
                control.UpdateTask();

            foreach (var newTask in taskQueue)
            {
                actionControls.Add(AddActionControl(newTask));
            }
            taskQueue.Clear();

            if (!worker.Running && worker.Completed)
            {
                InstallButton.Content = "Close";
                InstallButton.IsEnabled = true;
                timer.Stop();
                if (worker.Success)
                {
                    var label = new Label()
                    {
                        Content = "Please start the StreamDeck Software again!\r\nThe Plugin might be blocked by Security-Software, make sure it will not be blocked!",
                        FontSize = 12,
                        FontWeight = FontWeights.DemiBold,
                        Margin = new Thickness(24),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                    };
                    actionPanel.Children.Add(label);
                }
            }
        }

        private InstallerActionControl AddActionControl(InstallerTask task)
        {
            var component = new InstallerActionControl(task);
            actionPanel.Children.Add(component);

            component.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(RequestNavigateHandler));

            return component;
        }

        private void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            if (!e.Uri.ToString().Contains(Parameters.importBinary))
                Process.Start(e.Uri.ToString());
            else
            {
                var pProcess = new Process();
                pProcess.StartInfo.FileName = e.Uri.AbsolutePath;
                pProcess.StartInfo.UseShellExecute = true;
                pProcess.StartInfo.WorkingDirectory = Parameters.pluginDir;
                pProcess.Start();
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (!worker.Running && !worker.Completed)
            {
                InstallButton.IsEnabled = false;
                Task.Run(worker.Run);
                timer.Start();
                descLabel.Visibility = Visibility.Collapsed | Visibility.Hidden;
                ViewGrid.RowDefinitions[0].MaxHeight = 48;
            }

            if (!worker.Running && worker.Completed)
            {
                this.Close();
            }
        }
    }
}
