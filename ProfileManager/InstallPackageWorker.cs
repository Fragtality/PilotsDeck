using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProfileManager
{
    public class InstallPackageWorker
    {
        protected List<InstallerTask> ProfileTaskList { get; } = [];
        protected bool ProfileTasksCompleted { get { return ProfileTaskList.All(t => t.State == TaskState.COMPLETED); } }
        protected DispatcherTimer SwapTimer { get; set; }
        protected ProfileController ProfileController { get; set; } = new();
        protected bool IsCanceled { get; set; } = false;
        public PackageFile PackageFile { get; protected set; } = new(null);

        public bool OptionRemoveOldProfiles {  get; set; } = false;

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
                InstallerTask.CurrentTask.SetError(ex);
            }

            return IsValid;
        }

        public void CheckExistingProfiles()
        {
            var checkTask = InstallerTask.AddTask($"Check existing Profiles", "");
            checkTask.DisplayOnlyLastCompleted = false;
            ProfileController.Load();
            foreach (var profile in PackageFile.PackagedProfiles)
            {
                if (!string.IsNullOrEmpty(profile.ProfileName) && ProfileController.ManifestNameExits(profile.ProfileName))
                {
                    checkTask.MessageLog($"Found match for '{profile.ProfileName}' (File: {profile.FileName})");
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
                    if (await AddStreamDeckProfilesAsync() && OptionRemoveOldProfiles)
                        await SwapProfilesAsync();
                }
                else
                    Logger.Log(LogLevel.Debug, $"Skipping Add & Swap for Package with no Profiles!");
            }
            catch (Exception ex)
            {
                InstallerTask.CurrentTask.SetError(ex);
            }
        }

        protected async Task<bool> AddStreamDeckProfilesAsync()
        {
            Logger.Log(LogLevel.Debug, $"Installing Profiles in StreamDeck Software ...");
            bool result = false;
            try
            {
                if (!Tools.IsStreamDeckRunning())
                {
                    Logger.Log(LogLevel.Debug, $"Starting StreamDeck Software ...");
                    var streamDeckTask = InstallerTask.AddTask($"Start StreamDeck Software", $"");

                    Tools.StartStreamDeckSoftware();
                    await Tools.WaitOnTask(streamDeckTask, "Start StreamDeck Software and wait {0}s", 3);

                    Tools.SetForegroundWindow(MainWindow.AppTitle);
                    if (Tools.IsStreamDeckRunning())
                        streamDeckTask.SetState("StreamDeck Software running.", TaskState.COMPLETED);
                    else
                        streamDeckTask.SetState("StreamDeck Software NOT running.", TaskState.WAITING);
                }

                string sdBinary = Tools.GetStreamDeckBinaryPath(out _);
                string file;
                foreach (var profile in PackageFile.PackagedProfiles)
                {
                    file = Path.GetFileNameWithoutExtension(profile.FileName);
                    Logger.Log(LogLevel.Debug, $"Adding Task for '{profile.FileName}'");
                    var task = InstallerTask.AddTask($"Add Profile '{profile.ProfileName}' to StreamDeck", $"Click the Link and select the StreamDeck Type:");
                    task.Hyperlink = profile.FileName;
                    task.HyperlinkURL = sdBinary;
                    task.HyperLinkArg = profile.InstallPath;
                    task.State = TaskState.WAITING;
                    task.DisableLinkAfterClick = false;
                    task.SetCompletedOnUrl = true;
                    task.SetUrlBold = true;
                    task.SetUrlFontSize = 12;
                    task.HyperlinkOnClick = SetStreamDeckWindowForeground;
                    ProfileTaskList.Add(task);
                }
                return true;
            }
            catch (Exception ex)
            {
                InstallerTask.CurrentTask.SetError(ex);
                result = false;
            }

            return result;
        }

        protected async Task SwapProfilesAsync()
        {
            if (CountProfileUpdates == 0)
            {
                Logger.Log(LogLevel.Debug, $"No Profiles to swap!");
                return;
            }
            else
                Logger.Log(LogLevel.Debug, $"Swapping {CountProfileUpdates} Profiles");
            var task = InstallerTask.AddTask($"Replace {CountProfileUpdates} old Profiles with updated Copies", "Wait for Profiles to be clicked & installed ...");
            task.DisplayOnlyLastCompleted = false;
            task.DisplayOnlyLastError = false;
            task.State = TaskState.WAITING;

            bool switchedToCompleted = false;
            int waitDelay = 2000;
            int countSearches = 0;
            int searchMax = 7;
            while (PackageFile.PackagedProfiles.Where(p => !p.IsInstalled && p.HasOldProfile).Any() && !IsCanceled)
            {
                await Task.Delay(waitDelay);
                if (IsCanceled)
                    return;

                if (ProfileTasksCompleted && !switchedToCompleted)
                {
                    switchedToCompleted = true;
                    task.ReplaceLastMessage("All Profiles clicked! Checking Profile Store ...");
                    Logger.Log(LogLevel.Debug, "All Profiles completed => switchedToCompleted");
                }

                if (switchedToCompleted)
                {
                    ProfileController.LoadWithoutMapping();
                    if (ProfileController.IsLoaded && !ProfileController.HasError)
                    {
                        
                        if (countSearches >= searchMax)
                        {
                            task.Message = $"Profile Search was aborted after {(int)((countSearches * waitDelay)/1000)}s";
                            Logger.Log(LogLevel.Debug, $"Aborting Check after {countSearches} Iterations - not installed Profiles: {PackageFile.PackagedProfiles.Where(p => !p.IsInstalled && p.HasOldProfile).Count()}");
                            break;
                        }
                        else if (countSearches > 1)
                            task.ReplaceLastMessage($"All Profiles clicked! Checking Profile Store ({countSearches}/{searchMax}) ...");

                        foreach (var profile in PackageFile.PackagedProfiles.Where(p => !p.IsInstalled && p.HasOldProfile))
                        {
                            if (ProfileController.ManifestHasCopy(profile.ProfileName))
                            {
                                profile.IsInstalled = true;
                                Logger.Log(LogLevel.Debug, $"Match found for '{profile.ProfileName}'");
                            }
                        }
                        countSearches++;
                    }
                    else
                    {
                        task.SetError("Unable to read Profile Data from StreamDeck!");
                        break;
                    }
                }
            }
            if (countSearches > 1)
                await Task.Delay(1000);
            else
            {
                Logger.Log(LogLevel.Debug, "Match on first search, use higher Delay");
                await Tools.WaitOnTask(task, "All Profiles installed - waiting {0}s", 10);
            }

            if (IsCanceled)
                return;

            Tools.SetForegroundWindow(MainWindow.AppTitle);
            await ProfileController.SwapUpdateManifest(PackageFile.PackagedProfiles.Where(p => p.HasOldProfile && p.IsInstalled).Select(p => p.ProfileName).ToList(), task);
        }

        public static bool SetStreamDeckWindowForeground()
        {
            Tools.SetForegroundWindow(Parameters.SD_WINDOW_NAME);
            return true;
        }

        public void Dispose()
        {
            Logger.Log(LogLevel.Debug, "Dispose");
            ProfileTaskList.Clear();
            IsCanceled = true;
            SwapTimer?.Stop();
            PackageFile?.Dispose();
            PackageFile = null;
        }
    }
}
