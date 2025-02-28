using CFIT.AppLogger;
using CFIT.Installer.Tasks;
using ProfileManager.json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ProfileManager
{
    public enum PackageClickResponse
    {
        NotClicked = 0,
        Clicked = 1,
        Ignored = 2,
    }

    public class PackageFile(string filePath)
    {
        public class PackagedProfile(string filename, string profilename)
        {
            public string FileName { get; set; } = filename;
            public string ProfileName { get; set; } = profilename;
            public bool HasOldProfile { get; set; } = false;
            public string InstallPath { get { return @$"{Parameters.PLUGIN_PROFILE_PATH}\{FileName}"; } }
            public bool IsInstalled { get; set; } = false;
            public PackageClickResponse ClickResponse { get; set; } = PackageClickResponse.NotClicked;
            public bool IsLinkDisabled { get; set; } = false;
        }

        public string FullPath { get; protected set; } = filePath;
        public string FileName { get { return Path.GetFileNameWithoutExtension(FullPath); } }
        public PackageManifest Manifest{ get; protected set; }
        public FileStream ArchiveStream { get; protected set; }
        public ZipArchive Archive { get; protected set; }
        public string Title { get { return string.IsNullOrWhiteSpace(Manifest?.Title) ? FileName : Manifest.Title; } }
        public string Notes { get { return Manifest?.Notes; } }
        public string Aircraft { get { return Manifest?.Aircraft; } }
        public string VersionPackage { get { return Manifest?.VersionPackage; } }
        public string Author { get { return Manifest?.Author; } }
        public string URL { get { return Manifest?.URL; } }
        public string VersionPlugin { get { return Manifest?.VersionPlugin; } }
        public bool KeepPackageContents { get; set; }
        public int CountProfiles { get { return PackagedProfiles.Count; } }
        public int CountImages { get { return FilesImages.Count; } }
        public int CountScripts { get { return FilesScripts.Count; } }
        public int CountExtras { get { return FilesExtras.Count; } }
        public int CountValidTotal { get { return PackagedProfiles.Count + FilesImages.Count + FilesScripts.Count; } }
        public bool IsCompatible { get; protected set; } = false;
        public bool IsDisposed { get; protected set; } = false;
        public string ProfileWorkPath { get { return KeepPackageContents ? @$"{Parameters.PLUGIN_PROFILE_PATH}\{Path.GetFileNameWithoutExtension(FileName)}" : Parameters.PROFILE_WORK_PATH; } }
        public List<PackagedProfile> PackagedProfiles { get; protected set; } = [];
        public List<string> FilesImages { get; protected set; } = [];
        public List<string> FilesScripts { get; protected set; } = [];
        public List<string> FilesExtras { get; protected set; } = [];
        public List<string> FilesUnknown { get; protected set; } = [];

        public bool CheckFile()
        {
            Logger.Debug($"Setting File to '{FullPath}'");
            var task = TaskStore.Add("Check File", $"Query basic File Information for '{FullPath}'");

            try
            {
                if (string.IsNullOrWhiteSpace(FullPath))
                {
                    task.SetError("Empty Path passed!");
                    return false;
                }

                string extension = Path.GetExtension(FullPath).ToLowerInvariant();
                if (extension != Parameters.PACKAGE_EXTENSION && extension != ".zip")
                {
                    task.SetError("The File has the wrong Extension!");
                    return false;
                }

                if (!File.Exists(FullPath))
                {
                    task.SetError("File does not exist!");
                    return false;
                }

                var fileInfo = new FileInfo(FullPath);
                if (fileInfo.Length == 0)
                {
                    task.SetError("File is empty!");
                    return false;
                }
                else
                    Logger.Debug($"Filesize is {fileInfo.Length}");

                task.Message = "Check File Access";
                using var stream = File.Open(FullPath, FileMode.Open);
                if (stream == null || !stream.CanRead)
                {
                    task.SetError("File can not be opened for Read!");
                    return false;
                }

                task.Message = "File checks done.";
                task.State = TaskState.COMPLETED;
                return true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }

        public bool LoadPackageInfo()
        {
            Logger.Debug($"Loading Package Info ...");
            var task = TaskStore.Add("Load Package Information", "Open ZIP Archive");

            try
            {
                Logger.Debug($"Opening Archive ...");
                ArchiveStream = new(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
                Archive = new(ArchiveStream, ZipArchiveMode.Read, true);
                if (Archive == null)
                {
                    task.SetError("Failed to open Stream (Archive was NULL)!");
                    return false;
                }

                if (Archive.Entries.Count == 0)
                {
                    task.SetError("Archive is Empty!");
                    return false;
                }


                task.Message = $"Get {Parameters.PACKAGE_JSON_FILE} from Archive";
                Logger.Debug(task.Message);
                var zipEntry = GetZipEntry(Parameters.PACKAGE_JSON_FILE);
                if (zipEntry == null)
                {
                    Dispose();
                    task.SetError($"The {Parameters.PACKAGE_JSON_FILE} File was not found!");
                    return false;
                }

                task.Message = $"Read {Parameters.PACKAGE_JSON_FILE} Content";
                Logger.Debug(task.Message);
                string packageStr = ReadStringFromZip(zipEntry);
                if (!string.IsNullOrWhiteSpace(packageStr))
                    Manifest = PackageManifest.LoadManifest(packageStr);
                else
                {
                    Dispose();
                    task.SetError($"The File {Parameters.PACKAGE_JSON_FILE} is Empty!");
                    return false;
                }

                task.Message = "Check Plugin Version";
                Logger.Debug(task.Message);
                if (!Tools.CheckVersion(Parameters.PLUGIN_VERSION, Tools.VersionCompare.GREATER_EQUAL, VersionPlugin, out bool compareable) || !compareable)
                {
                    Dispose();
                    if (compareable)
                        task.SetError($"Current Plugin Version '{Parameters.PLUGIN_VERSION}' is below the required Version '{VersionPlugin}' of the Package");
                    else
                        task.SetError($"The Plugin Version '{VersionPlugin}' in the Package could not be matched against the Plugin Version '{Parameters.PLUGIN_VERSION}'");
                    return false;
                }
                else
                    IsCompatible = true;

                task.Message = "Count Package Files";
                Logger.Debug(task.Message);
                foreach (ZipArchiveEntry entry in Archive.Entries)
                {
                    Logger.Debug(entry.FullName);

                    if (IsProfilePath(entry.FullName))
                    {
                        string filename = entry.FullName.Replace($"{Parameters.PLUGIN_PROFILE_FOLDER}/", "", StringComparison.InvariantCultureIgnoreCase);
                        string profilename = ReadProfileManifestFromZip(task, entry);
                        if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(profilename))
                            return false;
                        else
                            PackagedProfiles.Add(new(filename, profilename));
                    }
                    else if (IsImagePath(entry.FullName))
                        FilesImages.Add(entry.FullName.Replace($"{Parameters.PLUGIN_IMAGE_FOLDER}/", "", StringComparison.InvariantCultureIgnoreCase));
                    else if (IsScriptPath(entry.FullName))
                        FilesScripts.Add(entry.FullName.Replace($"{Parameters.PLUGIN_SCRIPTS_FOLDER}/", "", StringComparison.InvariantCultureIgnoreCase));
                    else if (entry.FullName.StartsWith($"{Parameters.PACKAGE_PATH_EXTRAS}/", StringComparison.InvariantCultureIgnoreCase) && !Path.EndsInDirectorySeparator(entry.FullName))
                        FilesExtras.Add(entry.FullName.Replace($"{Parameters.PACKAGE_PATH_EXTRAS}/", "", StringComparison.InvariantCultureIgnoreCase));
                    else if (entry.FullName != Parameters.PACKAGE_JSON_FILE && !Path.EndsInDirectorySeparator(entry.FullName))
                        FilesUnknown.Add(entry.FullName);
                }
                Logger.Debug($"-> {CountProfiles} Profiles | {CountImages} Images | {CountScripts} Scripts");

                if (CountValidTotal == 0)
                {
                    Dispose();
                    task.SetError("The Package does not contain any Files to install!");
                    return false;
                }


                if (CountProfiles == 0)
                    Logger.Debug("Image or Script only Package detected");

                task.Message = "Package loaded.";
                task.State = TaskState.COMPLETED;
                return true;
            }
            catch (Exception ex)
            {
                Dispose();
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }

        protected string ReadProfileManifestFromZip(TaskModel task, ZipArchiveEntry archiveEntry)
        {
            task.Message = $"Open {archiveEntry.FullName} Content";
            Logger.Debug(task.Message);
            string profilename = null;
            using var tempStream = archiveEntry.Open();
            if (tempStream == null)
            {
                Dispose();
                task.SetError("Failed to open Stream (tempStream was NULL)!");
                return profilename;
            }

            using var tempArchive = new ZipArchive(tempStream, ZipArchiveMode.Read, true);
            if (tempArchive == null)
            {
                Dispose();
                task.SetError("Failed to read Stream (tempArchive was NULL)!");
                return profilename;
            }
            if (tempArchive.Entries.Count == 0)
            {
                Dispose();
                task.SetError("Archive is Empty!");
                return profilename;
            }

            task.Message = $"Get {Parameters.SD_PROFILE_MANIFEST} for Profile";
            ZipArchiveEntry manifestEntry = null;
            foreach (var entry in tempArchive.Entries)
            {
                if (entry.FullName.Contains($".sdProfile/{Parameters.SD_PROFILE_MANIFEST}", StringComparison.InvariantCultureIgnoreCase)
                    || entry.FullName.Contains($".sdProfile\\{Parameters.SD_PROFILE_MANIFEST}", StringComparison.InvariantCultureIgnoreCase))
                {
                    manifestEntry = entry;
                    break;
                }
            }
            if (manifestEntry == null)
            {
                Dispose();
                task.SetError($"Failed to locate {Parameters.SD_PROFILE_MANIFEST} for Profile!");
                return profilename;
            }

            string packageStr = ReadStringFromZip(manifestEntry);
            if (!string.IsNullOrWhiteSpace(packageStr))
                return ProfileManifest.LoadManifest(packageStr)?.ProfileName;
            else
            {
                Dispose();
                task.SetError($"The File {archiveEntry.FullName} is Empty!");
                return profilename;
            }
        }

        public bool InstallPackage()
        {
            try
            {
                Logger.Debug($"Install Package Files ...");

                var task = TaskStore.Add("Install Package Files", $"Extract Archive to Work-Directory: ({ProfileWorkPath})");              
                task.DisplayCompleted = false;

                if (Directory.Exists(ProfileWorkPath))
                {
                    Logger.Debug($"Clean up work Folder (pre)");
                    Directory.Delete(ProfileWorkPath, true);
                }
                
                Logger.Debug($"Extract Archive to work dir");
                Archive.ExtractToDirectory(ProfileWorkPath, true);

                if (CountProfiles > 0)
                    CopyValidFolderFiles(Parameters.PLUGIN_PROFILE_FOLDER, $"*{Parameters.SD_PROFILE_EXTENSION}", 0);
                if (CountImages > 0)
                    CopyValidFolderFiles(Parameters.PLUGIN_IMAGE_FOLDER, $"*{Parameters.PLUGIN_IMAGE_EXT}", 1);
                if (CountScripts > 0)
                    CopyValidFolderFiles(Parameters.PLUGIN_SCRIPTS_FOLDER, $"*{Parameters.PLUGIN_SCRIPTS_EXT}", 2);
                if (CountExtras > 0)
                    CopyExtraFiles();

                if (!KeepPackageContents && Directory.Exists(ProfileWorkPath))
                {
                    task.Message = "Cleanup Work-Directory";
                    Logger.Debug($"Clean up work Folder (post)");
                    Directory.Delete(ProfileWorkPath, true);
                }
                else
                    Logger.Debug($"Cleanup (post) skipped (keep {KeepPackageContents})");

                Dispose();
                task.SetSuccess("Package Files placed into Plugin-Directory.");
                task.IsCompleted = true;
                return true;
            }
            catch (Exception ex)
            {
                Dispose();
                TaskStore.CurrentTask.SetError(ex);
                return false;
            }
        }

        protected void CopyValidFolderFiles(string folder, string extension, int recurseDepth = 0)
        {
            TaskStore.CurrentTask.Message = $"Copying Files for Folder '{folder}'";


            EnumerationOptions enumOptions = recurseDepth > 0 ? new() { RecurseSubdirectories = true, MaxRecursionDepth = recurseDepth } : new();
            var enumDir = Directory.EnumerateFiles(@$"{ProfileWorkPath}\{folder}", $"*{extension}", enumOptions);
            string dest;
            foreach (var entry in enumDir)
            {
                dest = GetFileDestinationPath(entry);
                if (!Directory.Exists(Path.GetDirectoryName(dest)))
                {
                    Logger.Warning($"Destination Path for '{dest}' does not exist!");
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                }    
                File.Copy(entry, dest, true);
            }
        }

        protected void CopyExtraFiles()
        {
            string folder = Parameters.PACKAGE_PATH_EXTRAS;
            TaskStore.CurrentTask.Message = $"Copying Files for Folder '{folder}' to Desktop";


            EnumerationOptions enumOptions = new() { RecurseSubdirectories = true, MaxRecursionDepth = 5 };
            var enumDir = Directory.EnumerateFiles(@$"{ProfileWorkPath}\{folder}", $"*.*", enumOptions);
            string dest;
            foreach (var entry in enumDir)
            {
                dest = ChangeFileDestinationPath(entry, @$"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}\{FileName}");
                if (!Directory.Exists(Path.GetDirectoryName(dest)))
                {
                    Logger.Debug($"Destination Path for '{dest}' does not exist!");
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                }
                File.Copy(entry, dest, true);
            }
        }

        protected string GetFileDestinationPath(string sourcePath)
        {
            return sourcePath.Remove(0, ProfileWorkPath.Length).Insert(0, Parameters.PLUGIN_PATH);
        }

        protected string ChangeFileDestinationPath(string sourcePath, string destinationPath)
        {
            return sourcePath.Remove(0, ProfileWorkPath.Length + 1 + Parameters.PACKAGE_PATH_EXTRAS.Length).Insert(0, destinationPath);
        }

        protected static bool IsProfilePath(string filepath)
        {
            return CheckPathBeginEnd(filepath, $"{Parameters.PLUGIN_PROFILE_FOLDER}/", Parameters.SD_PROFILE_EXTENSION);
        }

        protected static bool IsImagePath(string filepath)
        {
            return CheckPathBeginEnd(filepath, $"{Parameters.PLUGIN_IMAGE_FOLDER}/", Parameters.PLUGIN_IMAGE_EXT);
        }

        protected static bool IsScriptPath(string filepath)
        {
            return CheckPathBeginEnd(filepath, $"{Parameters.PLUGIN_SCRIPTS_FOLDER}/", Parameters.PLUGIN_SCRIPTS_EXT);
        }

        protected static bool CheckPathBeginEnd(string filepath, string start, string end)
        {
            return filepath.StartsWith(start, StringComparison.InvariantCultureIgnoreCase) && filepath.EndsWith(end, StringComparison.InvariantCultureIgnoreCase);
        }

        protected ZipArchiveEntry GetZipEntry(string relPath)
        {
            return Archive.GetEntry(relPath);
        }

        protected static string ReadStringFromZip(ZipArchiveEntry zipEntry)
        {
            using StreamReader entryStream = new(zipEntry.Open(), Encoding.UTF8);
            return entryStream.ReadToEnd();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            Logger.Debug("Dispose");
            Archive?.Dispose();
            Archive = null;
            ArchiveStream?.Dispose();
            ArchiveStream = null;

            IsDisposed = true;
        }
    }
}
