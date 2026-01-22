using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using MessageBox = System.Windows.Forms.MessageBox;
namespace ProfileManager
{
    public class InstallPackageWorker
    {
        protected List<TaskModel> ProfileTaskList { get; } = [];
        protected Dictionary<string, TaskModel> ProfileTasks { get; } = [];
        protected bool ProfileTasksCompleted { get { return ProfileTaskList.All(t => t.State == TaskState.COMPLETED); } }
        protected DispatcherTimer SwapTimer { get; set; }
        protected ProfileController ProfileController { get; set; } = new();
        protected FuncStreamDeck StreamDeck { get; set; } = new();
        protected bool IsCanceled { get; set; } = false;
        public PackageFile PackageFile { get; protected set; } = new(null);

        public bool OptionRemoveOldProfiles { get; set; } = false;

        public bool IsValid { get { return IsChecked && IsLoaded && IsCompatible; } }
        public bool IsChecked { get; protected set; } = false;
        public bool IsLoaded { get; protected set; } = false;
        public bool IsCompatible { get { return PackageFile.IsCompatible; } }
        public bool FilesInstalled { get; protected set; } = false;
        public bool IsInstalled { get { return FilesInstalled && ProfileTasksCompleted; } }

        public int CountValidFiles { get { return PackageFile.CountValidTotal; } }
        public int CountTotalFiles { get { return PackageFile.CountValidTotal + PackageFile.FilesUnknown.Count; } }
        public int CountProfileUpdates { get { return PackageFile.PackagedProfiles.Where(p => p.HasOldProfile).Count(); } }

        public void SetFile(string filePath)
        {
            PackageFile = new(filePath);
        }

        public bool LoadPackage()
        {
            try
            {
                IsChecked = PackageFile.CheckFile();
                if (IsChecked)
                    IsLoaded = PackageFile.LoadPackageInfo();

                if (IsValid && PackageFile.CountProfiles > 0)
                    CheckExistingProfiles();
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }

            return IsValid;
        }

        public void CheckExistingProfiles()
        {
            var checkTask = TaskStore.Add($"Check existing Profiles", "");
            checkTask.DisplayCompleted = false;
            ProfileController.Load();
            foreach (var profile in PackageFile.PackagedProfiles)
            {
                if (!string.IsNullOrEmpty(profile.ProfileName) && ProfileController.ManifestNameExits(profile.ProfileName))
                {
                    checkTask.Message = $"Found match for '{profile.ProfileName}' (File: {profile.FileName})";
                    profile.HasOldProfile = true;
                }
            }
            OptionRemoveOldProfiles = CountProfileUpdates > 0;
            checkTask.SetState($"Found {CountProfileUpdates} existing Profiles", TaskState.COMPLETED);
        }

        public async Task InstallPackageAsync()
        {
            try
            {
                FilesInstalled = PackageFile.InstallPackage();
                PackageFile.Dispose();
                if (!FilesInstalled)
                    return;

                if (PackageFile.CountProfiles > 0)
                {
                    if (Parameters.IsStreamDockMode)
                    {
                        string _temp = PackageFile.PackagedProfiles[0].InstallPath.Trim('"');
                        string argument = $"/select,\"{_temp}\"";
                        Process.Start("explorer.exe", argument);
                        MainWindow.SetForeground();
                        await SwapProfilesAsync();
                        string message = "You can use the Deck Scene Conversion plugin in SPACE to perform the conversion.\n\n" +
                   "Would you like to open the plugin page to learn more?";
                        string caption = "Plugin Suggestion";

                        DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo("https://space.key123.vip/product?id=20251206002211")
                                {
                                    UseShellExecute = true
                                });
                            }
                            catch (System.ComponentModel.Win32Exception)
                            {
                                MessageBox.Show("Could not open the browser. Please visit: https://space.key123.vip/product?id=20251206002211");
                            }
                        }
                        MainWindow.SetForeground();
                        return;
                    }
                    if (await AddStreamDeckProfilesAsync() && await CheckProfilesInstalled() && OptionRemoveOldProfiles)
                    {
                        MainWindow.SetForeground();
                        await SwapProfilesAsync();
                    }
                    else
                        MainWindow.SetForeground();
                }
                else
                    Logger.Debug($"Skipping Add & Swap for Package with no Profiles!");
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
            }
        }

        protected async Task<bool> AddStreamDeckProfilesAsync()
        {
            Logger.Debug($"Installing Profiles in {Parameters.PlatformSoftwareName} ...");
            bool result = false;
            try
            {
                if (!FuncStreamDeck.IsDeckAndPluginRunning())
                {
                    Logger.Information($"Starting {Parameters.PlatformSoftwareName} for profile installation...");
                    var worker = new WorkerStreamDeckStartStop<Config>(Config.Instance, DeckProcessOperation.START) { RefocusWindow = true, RefocusWindowTitle = MainWindow.AppTitle };
                    await worker.Run(System.Threading.CancellationToken.None);
                }

                string sdBinary = StreamDeck.BinaryPath;
                string file;
                bool disableLinks = false;
                foreach (var profile in PackageFile.PackagedProfiles)
                {
                    file = Path.GetFileNameWithoutExtension(profile.FileName);
                    Logger.Debug($"Adding Task for '{profile.FileName}'");
                    var task = TaskStore.Add($"Add Profile '{profile.ProfileName}' to {Parameters.PlatformName}");
                    task.AddMessage($"Click the Link and select the {Parameters.PlatformName} in the UI (or click Ignore):", true, false, false, FontWeights.DemiBold);
                    task.State = TaskState.WAITING;
                    task.DisplayCompleted = true;
                    var link = task.AddLink(profile.FileName, sdBinary, profile.InstallPath);
                    task.DisableAllLinksOnClick = disableLinks;
                    link.DisableLinkOnClick = disableLinks;
                    link.LinkStyleBold = true;
                    link.LinkFontSize = 12;
                    link.StateOnLinkClicked = TaskState.ACTIVE;
                    link.ClickedCallback = () => { SetStreamDeckWindowForeground(); profile.ClickResponse = PackageClickResponse.Clicked; Logger.Debug($"Clicked Install for on '{profile.FileName}'"); };
                    link = task.AddLink("Ignore", null);
                    link.DisableLinkOnClick = disableLinks;
                    link.LinkStyleBold = true;
                    link.LinkFontSize = 12;
                    link.StateOnLinkClicked = TaskState.COMPLETED;
                    link.ClickedCallback = () => { profile.ClickResponse = PackageClickResponse.Ignored; Logger.Debug($"Clicked Ignore for on '{profile.FileName}'"); };
                    ProfileTaskList.Add(task);
                    ProfileTasks.Add(profile.FileName, task);
                }
                return true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                result = false;
            }

            return result;
        }

        private static void MessageWaitForProfilesInstalled(IEnumerable<PackageFile.PackagedProfile> query, TaskModel task)
        {
            string msg = $"Wait for Profiles to be added to {Parameters.PlatformName}:";
            foreach (var profile in query)
                msg += $"\r\n{profile.FileName}";
            task.ReplaceLastMessage(msg);
        }

        protected void CompleteTasks(IEnumerable<PackageFile.PackagedProfile> query)
        {
            foreach (var profile in query)
            {
                if (!ProfileTasks.TryGetValue(profile.FileName, out var task))
                    continue;

                foreach (var link in task.Links)
                    link.WasNavigated = true;
                profile.IsLinkDisabled = true;
                task.State = TaskState.COMPLETED;
                task.IsCompleted = true;
            }
        }

        protected async Task<bool> CheckProfilesInstalled()
        {
            var task = TaskStore.Add($"Install {PackageFile.PackagedProfiles.Count} Profiles to {Parameters.PlatformName}", "Wait for all Profiles to be clicked ...");
            task.DisplayCompleted = false;
            task.State = TaskState.WAITING;

            try
            {
                int waitDelay = 1000;
                while (PackageFile.PackagedProfiles.Where(p => p.ClickResponse == PackageClickResponse.NotClicked).Any() && !IsCanceled)
                {
                    await Task.Delay(waitDelay);
                    CompleteTasks(PackageFile.PackagedProfiles.Where(p => p.ClickResponse == PackageClickResponse.Ignored && !p.IsLinkDisabled));

                    if (!QueryProfilesInstalled(task, PackageFile.PackagedProfiles.Where(p => p.ClickResponse == PackageClickResponse.Clicked && !p.IsInstalled)))
                        return false;

                    CompleteTasks(PackageFile.PackagedProfiles.Where(p => p.IsInstalled && !p.IsLinkDisabled));
                }
                task.ReplaceLastMessage("All profiles clicked!");

                var query = PackageFile.PackagedProfiles.Where(p => !p.IsInstalled && p.ClickResponse != PackageClickResponse.Ignored);
                waitDelay = 1000;
                while (query.Any() && !IsCanceled)
                {
                    MessageWaitForProfilesInstalled(query, task);
                    await Task.Delay(waitDelay);

                    if (!QueryProfilesInstalled(task, query))
                        return false;

                    CompleteTasks(PackageFile.PackagedProfiles.Where(p => p.IsInstalled && !p.IsLinkDisabled));
                    CompleteTasks(PackageFile.PackagedProfiles.Where(p => p.ClickResponse == PackageClickResponse.Ignored && !p.IsLinkDisabled));
                    query = PackageFile.PackagedProfiles.Where(p => !p.IsInstalled && p.ClickResponse != PackageClickResponse.Ignored);
                }

                CompleteTasks(PackageFile.PackagedProfiles.Where(p => !p.IsLinkDisabled));
                task.SetSuccess("All Profiles installed (or ignored)!");
                task.IsCompleted = true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }

            return true;
        }

        protected bool QueryProfilesInstalled(TaskModel task, IEnumerable<PackageFile.PackagedProfile> query)
        {
            ProfileController.LoadWithoutMapping();
            if (ProfileController.IsLoaded && !ProfileController.HasError)
            {
                foreach (var profile in query)
                {
                    if ((profile.HasOldProfile && ProfileController.ManifestHasCopy(profile.ProfileName))
                        || (!profile.HasOldProfile && ProfileController.ManifestNameExits(profile.ProfileName)))
                    {
                        profile.IsInstalled = true;
                        Logger.Debug($"Match found for '{profile.ProfileName}'");
                    }
                }
            }
            else
            {
                task.SetError($"Unable to read Profile Data from {Parameters.PlatformSoftwareName}!");
                return false;
            }

            return true;
        }

        protected async Task SwapProfilesAsync()
        {
            var query = PackageFile.PackagedProfiles.Where(p => p.IsInstalled && p.HasOldProfile);
            if (!query.Any())
            {
                Logger.Debug($"No Profiles to swap!");
                return;
            }

            await ProfileController.SwapUpdateManifest([.. query.Select(p => p.ProfileName)]);
        }

        public static bool SetStreamDeckWindowForeground()
        {
            Sys.SetForegroundWindow(Parameters.SD_WINDOW_NAME);
            return true;
        }

        public void Dispose()
        {
            Logger.Debug("Dispose");
            ProfileTaskList.Clear();
            IsCanceled = true;
            SwapTimer?.Stop();
            PackageFile?.Dispose();
            PackageFile = null;
        }
    }
}
