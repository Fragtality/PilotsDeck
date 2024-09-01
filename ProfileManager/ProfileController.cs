using Microsoft.VisualBasic.FileIO;
using ProfileManager.json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ProfileManager
{
    public class ProfileController
    {
        public static readonly JsonSerializerOptions WriteIndented = new() { WriteIndented = true };

        public List<ProfileMapping> ProfileMappings { get; protected set; } = [];
        public List<DeviceInfo> DeviceInfos { get; protected set; } = [];
        public List<ProfileManifest> ProfileManifests { get; protected set; } = [];
        public bool IsLoaded { get; protected set; } = false;
        public bool HasError { get; protected set; } = false;
        public bool HasChanges { get { return ProfileMappings.Any(m => m.IsChanged || m.DeleteFlag) || ProfileManifests.Any(m => m.IsChanged || m.DeleteFlag); } }

        public int CountMappingsUnmatched { get; protected set; } = 0;
        public int CountMappingsChanged { get { return ProfileMappings.Where(m => m.IsChanged).Count(); } }
        public int CountManifestsChanged { get { return ProfileManifests.Where(m => m.IsChanged).Count(); } }
        public int CountMappingsRemoved { get { return ProfileMappings.Where(m => m.DeleteFlag).Count() - CountMappingsUnmatched; } }
        public int CountManifestsRemoved { get { return ProfileManifests.Where(m => m.DeleteFlag).Count(); } }

        public static bool AppsRunning { get { return Tools.GetProcessRunning(Parameters.SD_BINARY_NAME) || Tools.GetProcessRunning("PilotsDeck"); } }

        public void Load()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "Controller is loading Files ...");

                IsLoaded = false;
                HasError = false;
                CountMappingsUnmatched = 0;
                LoadProfileManifests();
                LoadDeviceInfo();
                LoadProfileMappings();
                if (!HasError)
                    MapAndCheckData();
                IsLoaded = !HasError;
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageBox.Show($"Exception '{ex.GetType()}' while loading Profiles!\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogException(ex);
            }
        }

        public void LoadWithoutMapping()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "Controller is loading Manifests ...");

                IsLoaded = false;
                HasError = false;
                CountMappingsUnmatched = 0;
                LoadProfileManifests(false);
                LoadDeviceInfo(false);
                LoadProfileMappings(false);
                IsLoaded = !HasError;
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageBox.Show($"Exception '{ex.GetType()}' while loading Profiles!\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogException(ex);
            }
        }

        protected void LoadProfileManifests(bool log = true)
        {
            ProfileManifests.Clear();

            var profilesDir = new DirectoryInfo(Parameters.SD_PROFILE_PATH);
            string filename;
            foreach (var directory in profilesDir.GetDirectories())
            {
                filename = $@"{directory.FullName}\{Parameters.SD_PROFILE_MANIFEST}";
                if (File.Exists(filename) && (new FileInfo(filename)).Length > 0)
                    ProfileManifests.Add(ProfileManifest.LoadManifest(filename, directory.Name, this));
                else
                    Logger.Log(LogLevel.Warning, $"Profile Manifest File '{directory.Name}\\{Parameters.SD_PROFILE_MANIFEST}' does not exist or is empty!");
            }
            if (log)
                Logger.Log(LogLevel.Information, $"ProfileManifests loaded (Count {ProfileManifests.Count})");
        }

        protected void LoadDeviceInfo(bool log = true)
        {
            DeviceInfos.Clear();

            string path = $@"{Parameters.PLUGIN_PROFILE_PATH}\{Parameters.PLUGIN_MAPPING_DEVICEINFO}";
            if (File.Exists(path) && (new FileInfo(path)).Length > 0)
            {
                DeviceInfos = JsonSerializer.Deserialize<List<DeviceInfo>>(File.ReadAllText(path)); ;
                if (log)
                    Logger.Log(LogLevel.Information, $"DeviceInfos loaded (Count {DeviceInfos.Count})");
            }
            else
            {
                MessageBox.Show($"The File '{Parameters.PLUGIN_PROFILE_FOLDER}\\{Parameters.PLUGIN_MAPPING_DEVICEINFO}' does not exist or is empty!\r\nStart/Stop the StreamDeck Software and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Log(LogLevel.Error, $"The File '{Parameters.PLUGIN_MAPPING_DEVICEINFO}' does not exist or is empty! ({path})");
                HasError = true;
            }
        }

        protected void LoadProfileMappings(bool log = true)
        {
            ProfileMappings.Clear();

            string path = $@"{Parameters.PLUGIN_PROFILE_PATH}\{Parameters.PLUGIN_MAPPING_FILE}";
            if (!File.Exists(path))
            {
                Logger.Log(LogLevel.Warning, $"Profile Mapping File '{Parameters.PLUGIN_PROFILE_FOLDER}\\{Parameters.PLUGIN_MAPPING_FILE}' does not exist! ({path})");
                return;
            }
            if ((new FileInfo(path)).Length <= 0)
            {
                Logger.Log(LogLevel.Warning, $"Profile Mapping File '{Parameters.PLUGIN_PROFILE_FOLDER}\\{Parameters.PLUGIN_MAPPING_FILE}' is empty - delete");
                File.Delete(path);
                return;
            }

            ProfileMappings = JsonSerializer.Deserialize<List<ProfileMapping>>(File.ReadAllText(path));
            if (log)
                Logger.Log(LogLevel.Information, $"ProfileMappings loaded (Count {ProfileMappings.Count})");
        }

        protected void MapAndCheckData()
        {
            foreach (var deviceInfo in DeviceInfos)
                foreach (var manifest in ProfileManifests.Where(m => m.Device.Hash == deviceInfo.ID))
                    manifest.SetDeviceInfo(deviceInfo);

            foreach (var mapping in ProfileMappings)
            {
                var query = ProfileManifests.Where(m => m.ProfileDirectoryCleaned == mapping.ProfileUUID);
                int count = query.Count();

                if (count == 1)
                    mapping.SetCheckManifest(query.FirstOrDefault());
                else
                    Logger.Log(LogLevel.Error, $"{count} Manifest returned for Mapping @ {mapping}");
            }

            var emptyManifests = ProfileMappings.Where(m => !m.HasManifest).ToList();
            CountMappingsUnmatched = emptyManifests.Count;
            if (CountMappingsUnmatched > 0)
            {
                emptyManifests.ForEach(m => m.DeleteFlag = true);
                Logger.Log(LogLevel.Warning, $"Found {CountMappingsUnmatched} ProfileMappings with empty Manifest - flagged for Deletion");
            }

            Logger.Log(LogLevel.Information, $"Profile Data mapped");
        }

        public void SaveChanges()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "Controller is saving Files ...");

                if (ProfileMappings.Any(m => m.IsChanged || m.DeleteFlag))
                    SaveProfileMappings();

                if (ProfileManifests.Any(m => m.IsChanged || m.DeleteFlag))
                    SaveProfileManifests();
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageBox.Show($"Exception '{ex.GetType()}' while saving Profiles!\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogException(ex);
            }
        }

        protected void SaveProfileMappings()
        {
            Logger.Log(LogLevel.Information, $"Deleting {ProfileMappings.Where(m => m.DeleteFlag).Count()} Mappings");
            ProfileMappings.RemoveAll(m => m.DeleteFlag);

            string path = $@"{Parameters.PLUGIN_PROFILE_PATH}\{Parameters.PLUGIN_MAPPING_FILE}";
            File.WriteAllText(path, JsonSerializer.Serialize(ProfileMappings, WriteIndented));

            ProfileMappings.ForEach(m => m.IsChanged = false);
            CountMappingsUnmatched = 0;
            Logger.Log(LogLevel.Information, $"ProfileMappings saved (Total {ProfileMappings.Count})");
        }

        protected void SaveProfileManifests()
        {
            string directory;
            var deletedManifests = ProfileManifests.Where(m => m.DeleteFlag);
            int countDeleted = deletedManifests.Count();
            foreach (var manifest in deletedManifests)
            {
                directory = $@"{Parameters.SD_PROFILE_PATH}\{manifest.ProfileDirectory}";
                Logger.Log(LogLevel.Information, $"Deleting Manifest from Filesystem: {directory}");
                FileSystem.DeleteDirectory(directory, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
            }
            ProfileManifests.RemoveAll(m => m.DeleteFlag);

            string filename;
            var changedManifests = ProfileManifests.Where(m => m.IsChanged);
            int counterChanged = changedManifests.Count();
            foreach (var manifest in changedManifests)
            {
                filename = $@"{Parameters.SD_PROFILE_PATH}\{manifest.ProfileDirectory}\{Parameters.SD_PROFILE_MANIFEST}";
                Logger.Log(LogLevel.Information, $"Saving Manifest for {manifest.ProfileDirectory}");
                ProfileManifest.WriteManifest(filename, manifest);
            }

            ProfileManifests.ForEach(m => m.IsChanged = false);
            Logger.Log(LogLevel.Information, $"ProfileManifests saved (Changed {counterChanged} | Deleted {countDeleted})");
        }

        public void CleanProfileManifestFlag()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "Cleaning Plugin Flags from Profiles ...");

                IsLoaded = false;
                HasError = false;
                CountMappingsUnmatched = 0;
                LoadProfileManifests();
                LoadProfileMappings();
                IsLoaded = !HasError;

                if (!IsLoaded)
                    return;

                foreach (var manifest in ProfileManifests)
                {
                    if (manifest.InstalledByPluginUUID == Parameters.PLUGIN_UUID)
                    {
                        Logger.Log(LogLevel.Debug, $"Clear Flags from Profile '{manifest.ProfileName}' ({manifest.ProfileDirectory})");

                        manifest.InstalledByPluginUUID = null;
                        manifest.PreconfiguredName = null;
                        manifest.IsChanged = true;

                        var query = ProfileMappings.Where(m => m.ProfileUUID == manifest.ProfileDirectoryCleaned);
                        Logger.Log(LogLevel.Debug, $"Removing {query.Count()} Mappings matching the ProfileUUID of '{manifest.ProfileName}'");
                        query.ToList().ForEach(m => m.DeleteFlag = true);
                    }
                }

                SaveProfileManifests();
                SaveProfileMappings();
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageBox.Show($"Exception '{ex.GetType()}' while cleaning Plugin Flags from Profiles!\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogException(ex);
            }
        }

        public bool ManifestNameExits(string name)
        {
            return ProfileManifests.Where(m => m.ProfileName == name).Any();
        }

        public bool ManifestHasCopy(string name)
        {
            return ProfileManifests.Where(m => MatchManifestCopy(m.ProfileName, name)).Any();
        }

        private static bool MatchManifestCopy(string value, string name, bool includeCopyNumber = true)
        {
            if (includeCopyNumber)
                return Regex.IsMatch(value, @"^" + Regex.Escape(name) + @"\s[\w]+$") ||
                       Regex.IsMatch(value, @"^" + Regex.Escape(name) + @"\s[\w]+ [\d]$");
            else
                return Regex.IsMatch(value, @"^" + Regex.Escape(name) + @"\s[\w]+$");
        }

        public async Task SwapUpdateManifest(List<string> updatedNames, InstallerTask task)
        {
            Logger.Log(LogLevel.Debug, "Swapping updated Manifests ...");
            try
            {
                if (Tools.IsStreamDeckRunning())
                {
                    await Tools.StopStreamDeckSoftware();
                    await Tools.WaitOnTask(task, "Stop StreamDeck Software and wait {0}s", 3);
                }

                LoadWithoutMapping();

                if (!IsLoaded)
                {
                    task.SetError("Could not load Profile and Mapping Data");
                    return;
                }

                task.ReplaceLastMessage("Searching for matching Names on old and new Profiles:");
                int countChangedManifest = 0;
                int countChangedMapping = 0;
                int countErrors = 0;
                foreach (var name in updatedNames)
                {
                    Logger.Log(LogLevel.Debug, $"Checking Name '{name}'");
                    var queryExistingWithName = ProfileManifests.Where(m => m.ProfileName == name);
                    int queryExistingWithNameCount = queryExistingWithName.Count();
                    var queryCopyWithName = ProfileManifests.Where(m => MatchManifestCopy(m.ProfileName, name));
                    int queryCopyWithNameCount = queryCopyWithName.Count();
                    var oldManifest = queryExistingWithName.FirstOrDefault();
                    var newManifest = queryCopyWithName.FirstOrDefault();

                    Logger.Log(LogLevel.Debug, $"Counts - OLD {queryExistingWithNameCount} => NEW {queryCopyWithNameCount}");
                    if (queryExistingWithNameCount == 1 && queryCopyWithNameCount == 1)
                    {

                        Logger.Log(LogLevel.Debug, $"Match found - OLD {oldManifest.ProfileName} => NEW {newManifest.ProfileName}");
                        Logger.Log(LogLevel.Debug, $"Match found - OLD {oldManifest.ProfileDirectoryCleaned} => NEW {newManifest.ProfileDirectoryCleaned}");
                        task.Message = $"MATCH: Remove old Profile <{oldManifest.ProfileName}> and replace by new Profile <{newManifest.ProfileName}>";

                        newManifest.ProfileName = oldManifest.ProfileName;
                        oldManifest.DeleteFlag = true;
                        newManifest.IsChanged = true;
                        countChangedManifest++;

                        var queryExistingMapping = ProfileMappings.Where(m => m.ProfileUUID == oldManifest.ProfileDirectoryCleaned);
                        if (queryExistingMapping.Any())
                        {
                            var mapping = queryExistingMapping.First();
                            task.Message = $"Update existing Mapping for <{mapping.ProfileName}>";
                            mapping.SetCheckManifest(newManifest);
                            mapping.IsChanged = true;
                            countChangedMapping++;
                        }
                    }
                    else if (queryCopyWithNameCount > 1)
                    {
                        task.MessageLog($"ERROR: Found multiple Copies ({queryCopyWithNameCount}) for Profile <{oldManifest.ProfileName}>!");
                        countErrors++;
                    }
                    else
                    {
                        task.MessageLog($"NO MATCH: Nothing to do for <{oldManifest.ProfileName}>");
                    }
                }

                if (countChangedManifest > 0)
                    SaveProfileManifests();
                if (countChangedMapping > 0)
                    SaveProfileMappings();

                task.MessageLog("Start StreamDeck Software");
                Tools.StartStreamDeckSoftware();

                if (countErrors > 0)
                    task.SetError($"\r\n=> Completed with {countErrors} Errors!");
                else
                    task.SetState($"\r\n=> Completed! ({countChangedManifest} replaced)", TaskState.COMPLETED);
            }
            catch (Exception ex)
            {
                InstallerTask.CurrentTask.SetError(ex);
                HasError = true;
                MessageBox.Show($"Exception '{ex.GetType()}' while cleaning Plugin Flags from Profiles!\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogException(ex);
            }
        }
    }
}
