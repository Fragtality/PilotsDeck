using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Installer
{
    public class InstallerWorker
    {
        public bool Running { get; set; } = false;
        public bool Completed { get; set; } = false;
        public bool Success { get; set; } = false;
        private Queue<InstallerTask> taskQueue;

        public InstallerWorker(Queue<InstallerTask> queue)
        {
            taskQueue = queue;
        }
        public void Run()
        {
            Running = true;

            try
            {
                DoTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception '{ex.GetType()}' in InstallerWorker", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Success = false;
            }

            Running = false;
            Completed = true;
        }

        private void DoTasks()
        {
            Success = DotNetFrameWork();
            if (!Success)
                return;

            Success = StreamDeckSW();
            if (!Success)
                return;

            Success = CheckMSFS();
            if (!Success)
                return;

            Success = InstallPlugin();
            if (!Success)
                return;

            Success = CheckProfiles();
            if (!Success)
                return;
        }

        private bool DotNetFrameWork()
        {
            //.NET Runtime
            var task = new InstallerTask(".NET Runtime", "Checking Runtime Version ...");
            taskQueue.Enqueue(task);
            if (InstallerFunctions.CheckDotNet())
            {
                task.ResultIcon = ActionIcon.OK;
                task.Message = $"The Runtime is at Version {Parameters.netVersion} or greater.";
                
                return true;
            }
            else
            {
                task.ResultIcon = ActionIcon.Warn;
                task.Message = $"The Runtime is not installed or outdated!\r\nDownloading Runtime ...";
                if (!InstallerFunctions.DownloadFile(Parameters.netUrl, Parameters.netUrlFile))
                {
                    task.ResultIcon = ActionIcon.Error;
                    task.Message = "Could not download .NET 7 Runtime!";
                    
                    return false;
                }
                task.Message = $"Installing Runtime ...";
                InstallerFunctions.RunCommand($"{Parameters.netUrlFile} /install /quiet /norestart");
                File.Delete(Parameters.netUrlFile);

                task.ResultIcon = ActionIcon.OK;
                task.Message = $"Runtime Version {Parameters.netVersion} was installed/updated successfully!\r\nPlease consider a Reboot.";
                
                return true;
            }
        }

        private bool StreamDeckSW()
        {
            //StreamDeck Version
            var task = new InstallerTask("StreamDeck Software", "Checking Software Version ...");
            taskQueue.Enqueue(task);
            if (InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersion) && InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersionRecommended))
            {
                task.ResultIcon = ActionIcon.OK;
                task.Message = $"The installed Software is at Version {Parameters.sdVersionRecommended} or greater.";
                
                return true;
            }
            else if (InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersion) && !InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersionRecommended))
            {
                task.ResultIcon = ActionIcon.Notice;
                task.Message = $"The installed Software Version mets the Minimum Requirements but is outdated.\r\nPlease consider updating the StreamDeck Software to Version {Parameters.sdVersionRecommended} or greater.\r\n";
                task.Hyperlink = "StreamDeck Software";
                task.HyperlinkURL = "https://www.elgato.com/downloads\r\n";

                return true;
            }
            else
            {
                task.ResultIcon = ActionIcon.Error;
                task.Message = $"The installed Software does not match the Minimum Version {Parameters.sdVersion}.\r\nPlease update the StreamDeck Software!\r\n";
                task.Hyperlink = "StreamDeck Software";
                task.HyperlinkURL = "https://www.elgato.com/downloads\r\n";

                return false;
            }
        }

        private bool CheckMSFS()
        {
            //Check MSFS Requirements
            var task = new InstallerTask("MSFS Requirements", "Checking FSUIPC & MobiFlight ...");
            taskQueue.Enqueue(task);
            if (!App.argIgnoreMSFS && InstallerFunctions.CheckInstalledMSFS(out string packagePath) && !string.IsNullOrWhiteSpace(packagePath))
            {                
                if (InstallerFunctions.CheckFSUIPC())
                {
                    if (InstallerFunctions.CheckPackageVersion(packagePath, Parameters.wasmMobiName, Parameters.wasmMobiVersion))
                    {
                        if (!InstallerFunctions.CheckPackageVersion(packagePath, Parameters.wasmIpcName, Parameters.wasmIpcVersion))
                        {
                            task.ResultIcon = ActionIcon.Warn;
                            task.Message = $"All MSFS Requirements met!\r\nBut the installed WASM Module from FSUIPC does not match the Minimum Version {Parameters.wasmIpcVersion}! It is not required for the Plugin itself, but could lead to Problems with Profiles/Integrations which use Lua-Scripts and L-Vars.\r\nConsider Reinstalling FSUIPC!";
                            task.Hyperlink = "\r\nFSUIPC";
                            task.HyperlinkURL = "http://fsuipc.com/\r\n";

                        }
                        else if (!InstallerFunctions.CheckFSUIPC7Pumps())
                        {
                            task.ResultIcon = ActionIcon.Notice;
                            task.Message = "All MSFS Requirements met!\r\nBut the FSUIPC7.ini is missing the NumberOfPumps=0 Entry in the [General] Section (which helps to avoid Stutters)!";

                        }
                        else
                        {
                            task.ResultIcon = ActionIcon.OK;
                            task.Message = "All MSFS Requirements met!";
                        }

                        return true;
                    }
                    else
                    {
                        task.ResultIcon = ActionIcon.Warn;
                        task.Message = $"The MobiFlight WASM Module is not installed or outdated! Downloading Module ...";

                        return InstallWasm(task, packagePath);
                    }
                }
                else
                {
                    task.ResultIcon = ActionIcon.Error;
                    task.Message = $"The installed FSUIPC Version does not match the Minimum Version {Parameters.ipcVersion}! Please install the latest Version.";
                    task.Hyperlink = "\r\nFSUIPC";
                    task.HyperlinkURL = "http://fsuipc.com/\r\n";

                    return false;
                }
            }
            else if (App.argIgnoreMSFS && InstallerFunctions.CheckInstalledMSFS(out _))
            {
                task.ResultIcon = ActionIcon.Notice;
                task.Message = "MSFS Validation was skipped as per User Request. Don't be suprised when the Plugin does not work for MSFS ;)";

                return true;
            }
            else
            {
                task.ResultIcon = ActionIcon.Error;
                task.Message = "MSFS not found.";

                return false;
            }
        }

        protected bool InstallWasm(InstallerTask task, string packagePath)
        {
            bool result = false;

            if (!InstallerFunctions.GetProcessRunning("FlightSimulator"))
            {
                if (Directory.Exists(packagePath + @"\" + Parameters.wasmMobiName))
                {
                    task.Message = "Deleting old Version ...";
                    Directory.Delete(packagePath + @"\" + Parameters.wasmMobiName, true);
                }
                task.Message = "Downloading MobiFlight Module ...";
                if (!InstallerFunctions.DownloadFile(Parameters.wasmUrl, Parameters.wasmUrlFile))
                {
                    task.ResultIcon = ActionIcon.Error;
                    task.Message = "Could not download MobiFlight Module!";
                    return result;
                }
                task.Message = "Extracting new Version ...";
                if (!InstallerFunctions.ExtractZipFile(packagePath, Parameters.wasmUrlFile))
                {
                    task.ResultIcon = ActionIcon.Error;
                    task.Message = "Error while extracting MobiFlight Module!";
                    return result;
                }
                File.Delete(Parameters.wasmUrlFile);

                result = true;
                task.ResultIcon = ActionIcon.OK;
                task.Message = $"All MSFS Requirements met!\r\nMobiFlight Module Version {Parameters.wasmMobiVersion} installed/updated successfully!";
            }
            else
            {
                task.ResultIcon = ActionIcon.Error;
                task.Message = "Can not install/update MobiFlight WASM Module while MSFS is running!";
            }

            return result;
        }

        private bool InstallPlugin()
        {
            //Stop Deck SW
            var task = new InstallerTask("StreamDeck Plugin", "Stopping StreamDeck Software ...");
            taskQueue.Enqueue(task);
            if (!InstallerFunctions.StopStreamDeck())
            {
                task.ResultIcon = ActionIcon.Error;
                task.Message = $"The StreamDeck Software could not be stopped!\r\nPlease stop it manually and try again.";

                return false;
            }

            //Delete Old Binaries
            task.Message = "StreamDeck Software stopped. Deleting old Plugin ...";
            if (!InstallerFunctions.DeleteOldFiles())
            {
                task.ResultIcon = ActionIcon.Error;
                task.Message = $"The old Binaries could not be removed!\r\nPlease remove them manually and try again.";

                return false;
            }

            //Extract Plugin
            task.Message = "Old Plugin deleted. Extracting new Version ...";
            if (InstallerFunctions.ExtractZip())
            {
                task.ResultIcon = ActionIcon.OK;
                task.Message = $"Plugin installed successfully!\r\nPath: %appdata%{Parameters.sdPluginDir}\\{Parameters.pluginName}";
            }
            else
            {
                task.ResultIcon = ActionIcon.Error;
                task.Message = $"Plugin Installation failed!";

                return false;
            }

            return true;
        }

        private bool CheckProfiles()
        {
            //Check Profiles
            var task = new InstallerTask("PilotsDeck Profiles", "Checking installed Profiles ...");
            taskQueue.Enqueue(task);
            if (InstallerFunctions.HasCustomProfiles(out bool oldDefault))
            {
                task.ResultIcon = ActionIcon.Warn;
                task.Message = $"Custom Profiles where detected - Run the Importer before you start the StreamDeck Software again!\r\n(It will only briefly pop-up if there are no new Profiles to import.)";
                if (oldDefault)
                    task.Message += "The old default Profiles 'Whiskey', 'X-Ray', 'Yankee' or 'Zulu' seem to be installed. You can remove them, if you never used them.\r\n";

                task.Hyperlink = "Import Profiles";
                task.HyperlinkURL = Parameters.pluginDir + "\\" + "ImportProfiles.exe";

            }
            else if (oldDefault)
            {
                task.ResultIcon = ActionIcon.Notice;
                task.Message = $"The old default Profiles 'Whiskey', 'X-Ray', 'Yankee' or 'Zulu' seem to be installed. You can remove them, if you never used them.\r\nIf you used them you need to import them:\r\n";
                task.Hyperlink = "Import Profiles";
                task.HyperlinkURL = Parameters.pluginDir + "\\" + "ImportProfiles.exe";
            }
            else
            {
                task.ResultIcon = ActionIcon.OK;
                task.Message = $"No Custom Profiles installed - nothing todo.";
            }

            return true;
        }
    }
}
