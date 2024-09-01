using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace ProfileManager
{
    public partial class ViewProfileInstaller : UserControl
    {
        protected InstallPackageWorker InstallWorker { get; set; }
        protected Action<ViewProfileInstaller> ActionButtonConfirmation { get; set; } = null;
        public bool IsPackageActive { get; protected set; } = false;
        public DispatcherTimer TimerClearTasks { get; protected set; }

        public ViewProfileInstaller()
        {
            InitializeComponent();

            InstallWorker = new();

            LabelNotes.Background = this.Background;
            LabelNotes.BorderThickness = new Thickness(0);

            TimerClearTasks = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.75) };
            TimerClearTasks.Tick += (sender, args) =>
            {
                AreaTaskStatus.Deactivate(true, InstallWorker.CountProfileUpdates > 0);
                TimerClearTasks.Stop();
            };

            SetInitialVisibilityState();
        }

        public void OpenPackageFile(string filename)
        {
            SetStateLoadingPackage(filename);
        }

        public void SetInitialVisibilityState()
        {
            AreaTaskStatus.Visibility = Visibility.Collapsed;
            AreaFileDrop.Visibility = Visibility.Visible;
            AreaPackageInfo.Visibility = Visibility.Collapsed;
            AreaButtons.Visibility = Visibility.Collapsed;
            LabelInstallerNotice.Visibility = Visibility.Collapsed;

            ButtonConfirmation.Visibility = Visibility.Visible;
        }

        public void SetStateOpenPackage()
        {
            SetInitialVisibilityState();
            CheckboxKeepContents.Visibility = Visibility.Visible;
            CheckboxRemoveOld.Visibility = Visibility.Visible;
            CheckboxKeepContents.IsEnabled = true;
            CheckboxRemoveOld.IsEnabled = true;

            IsPackageActive = false;

            TimerClearTasks.Stop();
            AreaTaskStatus.Deactivate(true);

            TreeFileContents.Items.Clear();
            InstallWorker?.Dispose();
            InstallWorker = new();

            ActionButtonConfirmation = null;
        }       

        protected void SetStateLoadingPackage(string filePath)
        {
            AreaTaskStatus.Visibility = Visibility.Visible;
            AreaFileDrop.Visibility = Visibility.Collapsed;
            AreaPackageInfo.Visibility = Visibility.Collapsed;
            AreaButtons.Visibility = Visibility.Collapsed;

            IsPackageActive = true;

            InstallWorker.SetFile(filePath);

            AreaTaskStatus.Activate(SetStateShowPackage, SetStateLoadFailed);
            
            Task.Run(InstallWorker.LoadPackage);
        }

        protected void SetStateLoadFailed()
        {
            AreaButtons.Visibility = Visibility.Visible;
            AreaTaskStatus.Deactivate();
            SetButtonState(false, true, false);
        }

        protected void SetStateShowPackage()
        {
            AreaTaskStatus.Visibility = Visibility.Visible;
            AreaFileDrop.Visibility = Visibility.Collapsed;
            AreaPackageInfo.Visibility = Visibility.Visible;
            AreaButtons.Visibility = Visibility.Visible;

            SetButtonState(InstallWorker.IsValid, !InstallWorker.IsValid, false, "Install", "box-arrow-in-right", "Start Installation!");
            ActionButtonConfirmation = v => v.SetStateInstallingPackage();

            TimerClearTasks.Start();

            ShowPackageInfo();
        }

        protected async void SetStateInstallingPackage()
        {
            AreaTaskStatus.Visibility = Visibility.Visible;
            AreaFileDrop.Visibility = Visibility.Collapsed;
            AreaPackageInfo.Visibility = Visibility.Collapsed;
            AreaButtons.Visibility = Visibility.Visible;
            LabelInstallerNotice.Visibility = Visibility.Visible;

            SetButtonState(false, true, false);

            TimerClearTasks.Stop();
            AreaTaskStatus.Activate(SetStateInstalledPackage, SetStateInstalledPackage);

            await InstallWorker.InstallPackageAsync();
        }

        protected void SetStateInstalledPackage()
        {
            AreaTaskStatus.Visibility = Visibility.Visible;
            AreaFileDrop.Visibility = Visibility.Collapsed;
            AreaPackageInfo.Visibility = Visibility.Collapsed;
            AreaButtons.Visibility = Visibility.Visible;
            LabelInstallerNotice.Visibility = Visibility.Collapsed;

            AreaTaskStatus.Deactivate();

            if (InstallWorker.IsInstalled)
            {
                ActionButtonConfirmation = v => v.SetStateOpenPackage();
                SetButtonState(true, false, true, "Close", "check-square", "Close this View");
            }
            else
            {
                SetButtonState(false, true, false);
            }

            IsPackageActive = false;
        }

        protected void SetButtonState(bool success, bool hideConfirmation, bool hideCandel = false, string caption = null, string file = null, string tooltip = null)
        {
            ButtonConfirmation.IsEnabled = success;

            if (hideConfirmation)
                ButtonConfirmation.Visibility = Visibility.Collapsed;
            else
                ButtonConfirmation.Visibility = Visibility.Visible;

            if (hideCandel)
                ButtonCancel.Visibility = Visibility.Collapsed;
            else
                ButtonCancel.Visibility = Visibility.Visible;

            if (caption != null)
                LabelButtonConfirmation.Text = caption;

            if (tooltip != null)
                ButtonConfirmation.ToolTip = tooltip;

            if (ImageButtonConfirmation != null && !string.IsNullOrEmpty(file))
                Tools.SetButtonImage(ImageButtonConfirmation, file);
        }

        protected void ShowPackageInfo()
        {
            try
            {
                Logger.Log(LogLevel.Debug, $"Showing Package Info for '{InstallWorker.PackageFile?.Title}'");

                var PackageFile = InstallWorker.PackageFile;

                LabelTitle.Text = PackageFile.Title ?? "";

                LabelPackageVersion.Text = PackageFile.VersionPackage ?? "";

                LabelAircraft.Text = PackageFile.Aircraft ?? "";

                LabelAuthor.Text = PackageFile.Author ?? "";

                LabelURL.Inlines.Clear();
                if (!string.IsNullOrWhiteSpace(PackageFile.URL) && Uri.TryCreate(PackageFile.URL, UriKind.RelativeOrAbsolute, out Uri urlResult))
                {
                    Hyperlink link = new(new Run(PackageFile.URL))
                    {
                        NavigateUri = urlResult,
                    };
                    LabelURL.Inlines.Add(link);
                    LabelURL.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(RequestNavigateHandler));
                }

                LabelNotes.Text = PackageFile.Notes ?? "";

                List<string> displayList = [];
                foreach (var profile in PackageFile.PackagedProfiles)
                {
                    if (profile.HasOldProfile)
                        displayList.Add($"Update: {profile.FileName}");
                    else
                        displayList.Add($"New: {profile.FileName}");
                }
                AddTreeItems($"{PackageFile.CountProfiles} Profiles", displayList, null, true);
                AddTreeItems($"{PackageFile.CountImages} Images", PackageFile.FilesImages);
                AddTreeItems($"{PackageFile.CountScripts} Scripts", PackageFile.FilesScripts);
                AddTreeItems($"{PackageFile.FilesUnknown.Count} Unknown", PackageFile.FilesUnknown, new SolidColorBrush(Colors.Orange));

                CheckboxRemoveOld.IsChecked = InstallWorker.OptionRemoveOldProfiles;
                if (InstallWorker.CountProfileUpdates > 0)
                {
                    LabelRemoveOld.Visibility = Visibility.Visible;
                    CheckboxRemoveOld.Visibility = Visibility.Visible;
                }
                else
                {
                    LabelRemoveOld.Visibility = Visibility.Collapsed;
                    CheckboxRemoveOld.Visibility = Visibility.Collapsed;
                }

                if (PackageFile.Manifest.KeepPackageContents)
                {
                    CheckboxKeepContents.IsEnabled = false;
                    CheckboxKeepContents.IsChecked = true;
                }
                else
                    CheckboxKeepContents.IsChecked = InstallWorker.OptionKeepContent;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"Error while displaying Package: {ex.Message}", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void AddTreeItems(string header, List<string> items, Brush brush = null, bool expand = false)
        {
            if (items.Count == 0)
                return;

            var treeitem = new TreeViewItem() { Header = header };
            if (brush != null)
                treeitem.Foreground = brush;

            foreach (var item in items)
            {
                treeitem.Items.Add(new TreeViewItem() { Header = item });
            }
            treeitem.IsExpanded = expand;
            TreeFileContents.Items.Add(treeitem);
        }

        protected void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Tools.OpenUri(sender, e);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"{ex.GetType()} - {ex.Message}", "Error loading URL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonOpenPackage_Drop(object sender, DragEventArgs e)
        {
            try
            {
                Logger.Log(LogLevel.Debug, $"Received File Drop");

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        SetStateLoadingPackage(files[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"Error while reading Package: {ex.Message}", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonOpenPackage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Log(LogLevel.Debug, $"Opening File Dialog");

                OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Profile Package ...",
                    Filter = $"{Parameters.PACKAGE_EXTENSION_NAME} (*{Parameters.PACKAGE_EXTENSION})|*{Parameters.PACKAGE_EXTENSION}|Zip File (*.zip)|*.zip|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SetStateLoadingPackage(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"Error while reading Package: {ex.Message}", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonConfirmation_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log(LogLevel.Debug, $"Confirming Installation (action {ActionButtonConfirmation?.Method})");

            ActionButtonConfirmation?.Invoke(this);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log(LogLevel.Debug, $"Cancel Installation");

            try
            {
                SetStateOpenPackage();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"Error resetting State: {ex.Message}", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            InstallWorker?.Dispose();
            AreaTaskStatus.Deactivate(true);
            TimerClearTasks.Stop();
        }

        private void CheckboxRemoveOld_Click(object sender, RoutedEventArgs e)
        {
            InstallWorker.OptionRemoveOldProfiles = !InstallWorker.OptionRemoveOldProfiles;
            CheckboxRemoveOld.IsChecked = InstallWorker.OptionRemoveOldProfiles;
        }

        private void CheckboxKeepContents_Click(object sender, RoutedEventArgs e)
        {
            if (CheckboxKeepContents.IsEnabled)
            {
                InstallWorker.OptionKeepContent = !InstallWorker.OptionKeepContent;
                CheckboxKeepContents.IsChecked = InstallWorker.OptionKeepContent;
            }
        }

        private void TreeFileContents_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
