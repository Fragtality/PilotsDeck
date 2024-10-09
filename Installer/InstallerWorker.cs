using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Installer
{
    public class InstallerWorker
    {
        public enum Simulator
        {
            FSX = 0,
            P3DV4 = 4,
            P3DV5 = 5,
            P3DV6 = 6,
            MSFS2020 = 2020,
            MSFS2024 = 2024,
            XP11 = 11,
            XP12 = 12,
        }

        public bool IsRunning { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public bool IsSuccess { get; set; } = false;
        public bool IsRemoved { get; set; } = false;
        public bool IsExistingInstallation { get; protected set; } = false;
        public bool ResetConfiguration { get; set; } = false;

        private Dictionary<Simulator, bool> SimulatorStates { get; set; } = new Dictionary<Simulator, bool>();
        private string MsfsPackagePath  = "";

        public InstallerWorker()
        {
            IsExistingInstallation = InstallerFunctions.CheckPluginInstalled();
        }

        public async Task Run()
        {
            IsRunning = true;

            try
            {
                await Task.Run(DoTasks);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                InstallerTask.CurrentTask.SetError(ex);
                IsSuccess = false;
            }

            IsRunning = false;
            IsCompleted = true;
        }

        private void DoTasks()
        {
            IsSuccess = DotNetFrameWork();
            if (!IsSuccess)
                return;

            IsSuccess = StreamDeckSW();
            if (!IsSuccess)
                return;

            if (CheckSimulators())
            {
                if (SimulatorStates.ContainsKey(Simulator.MSFS2020))
                    SimulatorStates[Simulator.MSFS2020] = CheckFSUIPC7() && CheckMobiFlight();

                if (SimulatorStates.ContainsKey(Simulator.P3DV4))
                    SimulatorStates[Simulator.P3DV4] = CheckFSUIPC6();
                else if (SimulatorStates.ContainsKey(Simulator.P3DV5))
                    SimulatorStates[Simulator.P3DV5] = CheckFSUIPC6();
                else if (SimulatorStates.ContainsKey(Simulator.P3DV6))
                    SimulatorStates[Simulator.P3DV6] = CheckFSUIPC6();

                IsSuccess = SimulatorStates.All(kv => kv.Value);
                if (!IsSuccess)
                    return;
            }

            IsSuccess = InstallPlugin();
            if (!IsSuccess)
                return;

            CheckProfiles();

            ShowVjoyInfo();

            if (IsSuccess)
            {
                if (!App.CmdLineStreamDeck)
                {
                    var task = InstallerTask.AddTask("Start StreamDeck", "Installation successful - starting StreamDeck Software again ...");
                    InstallerFunctions.StartStreamDeckSoftware();
                    task.State = TaskState.COMPLETED;
                }
                else
                {
                    int seconds = 10;
                    string msg = "Installation successful! The StreamDeck Software will be stopped in {0}s.\nPlease start it manually again!";
                    var task = InstallerTask.AddTask("Stop StreamDeck", "");
                    task.State = TaskState.WAITING;
                    for (int i = seconds; i >= 0; i--)
                    {
                        task.ReplaceLastMessage(string.Format(msg, i));
                        Thread.Sleep(1000);
                    }
                    InstallerFunctions.StopStreamDeckSoftware();
                    task.State = TaskState.COMPLETED;
                }
            }
        }

        private bool DotNetFrameWork()
        {
            //.NET Runtime
            var task = InstallerTask.AddTask(".NET Runtime", "Checking Runtime Version ...");
            
            if (InstallerFunctions.CheckDotNet())
            {
                task.SetSuccess($"The Runtime is at Version {Parameters.netVersion} or greater.");                
                return true;
            }
            else
            {
                task.SetState($"The Runtime is not installed or outdated!\r\nDownloading Runtime ...", TaskState.WAITING);
                if (!InstallerFunctions.DownloadNetRuntime(Parameters.netUrl, Parameters.netUrlFile, out string filepath))
                {
                    task.SetError("Could not download .NET Runtime!");
                    return false;
                }

                task.Message = $"Installing Runtime ...";
                Tools.RunCommand($"{filepath} /install /quiet /norestart");
                File.Delete(filepath);

                task.SetSuccess($"Runtime Version {Parameters.netVersion} was installed/updated successfully!\r\nPlease consider a Reboot.");                
                return true;
            }
        }

        private bool StreamDeckSW()
        {
            //StreamDeck Version
            var task = InstallerTask.AddTask("StreamDeck Software", "Checking Software Version ...");
            
            if (InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersion) && InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersionRecommended))
            {
                task.SetSuccess($"The installed Software is at Version {Parameters.sdVersionRecommended} or greater.");
                return true;
            }
            else if (InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersion) && !InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersionRecommended))
            {
                task.SetState($"The installed Software Version mets the Minimum Requirements but is outdated.\r\nPlease consider updating the StreamDeck Software to Version {Parameters.sdVersionRecommended} or greater.\r\n",
                     TaskState.WAITING);
                task.Hyperlink = "StreamDeck Software";
                task.HyperlinkURL = $"{Parameters.sdUrl}\r\n";

                return true;
            }
            else
            {
                task.SetState($"The StreamDeck Software is not installed or outdated!\r\nDownloading Installer ...", TaskState.WAITING);
                if (!InstallerFunctions.DownloadFile(Parameters.sdUrl, Parameters.sdUrlFile, out string filepath))
                {
                    task.SetError("Could not download StreamDeck Installer!");
                    return false;
                }

                task.Message = $"Starting interactive Installer ...";
                Tools.RunCommand(filepath);
                Thread.Sleep(1000);
                File.Delete(filepath);

                bool result = InstallerFunctions.CheckStreamDeckSW(Parameters.sdVersionRecommended);
                if (result)
                    task.SetSuccess($"StreamDeck Software {Parameters.sdVersionRecommended} installed!");
                else
                    task.SetError($"Setup failed.");

                return result;
            }
        }

        private static void AddMessageOrReplace(InstallerTask task, string msg, int counter)
        {
            if (counter == 0)
                task.ReplaceLastMessage(msg);
            else
                task.Message = msg;
        }

        private bool CheckSimulators()
        {
            var task = InstallerTask.AddTask("Installed Simulators", "Checking installed Simulators ...");
            task.DisplayOnlyLastCompleted = false;
            var successState = TaskState.COMPLETED;
            int countFound = 0;

            if (InstallerFunctions.CheckInstalledMSFS(out MsfsPackagePath))
            {
                string msg = "Found: FlightSimulator 2020";
                if (App.CmdLineIgnoreMSFS)
                {
                    msg += " (Requirement Check disabled by User!)";
                    successState = TaskState.ACTIVE;
                }
                else
                {
                    SimulatorStates.Add(Simulator.MSFS2020, true);
                }
                AddMessageOrReplace(task, msg, countFound);
                countFound++;
            }

            var xpVersions = CheckInstalledVersionsXp();
            if (SimulatorStates.ContainsKey(Simulator.XP11) || SimulatorStates.ContainsKey(Simulator.XP12))
            {
                AddMessageOrReplace(task, $"Found: X-Plane {string.Join(", ", xpVersions)}", countFound);
                countFound++;
            }

            var p3dVersions = CheckInstalledVersionsP3d();
            if (SimulatorStates.ContainsKey(Simulator.P3DV4) || SimulatorStates.ContainsKey(Simulator.P3DV5) || SimulatorStates.ContainsKey(Simulator.P3DV6))
            {
                AddMessageOrReplace(task, $"Found: Prepar3D {string.Join(", ", p3dVersions)}", countFound);
                countFound++;
            }


            if (countFound == 0)
            {
                AddMessageOrReplace(task, "No Simulators found - Can not check for Requirements!", countFound);
                successState = TaskState.ACTIVE;
            }

            task.State = successState;
            return SimulatorStates.Count != 0;
        }

        private static readonly Regex rxXpPrefFile = new Regex(@"^X-Plane (\d+) Preferences$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private List<string> CheckInstalledVersionsXp()
        {
            var versions = new List<string>();

            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] files = Directory.EnumerateFiles(path, "*.prf").ToArray();

                foreach (var file in files)
                {
                    var match = rxXpPrefFile.Match(Path.GetFileNameWithoutExtension(file));
                    if (match.Success && match.Groups.Count == 2 && int.TryParse(match.Groups[1].Value, out int version))
                    {
                        SimulatorStates.Add((Simulator)version, true);
                        versions.Add(match.Groups[1].Value);
                    }
                }
            }
            catch { }

            return versions;
        }

        private List<string> CheckInstalledVersionsP3d()
        {
            var versions = new List<string>();
            foreach (int version in Enum.GetValues(typeof(Simulator)))
            {
                try
                {
                    if ((int)Registry.GetValue($@"{Parameters.prepRegPath}\{Parameters.prepRegFolderPrefix}{version}", Parameters.prepRegValueInstalled, null) == 1)
                    {
                        if (!SimulatorStates.ContainsKey((Simulator)version))
                            SimulatorStates.Add((Simulator)version, true);
                        versions.Add($"v{version}");
                    }
                }
                catch { }
            }

            return versions;
        }

        private bool CheckFSUIPC7()
        {
            var task = InstallerTask.AddTask("FSUIPC7 Installation", "Check State and Version of FSUIPC7 ...");

            if (InstallerFunctions.CheckFSUIPC7(out bool isInstalled))
            {
                if (!InstallerFunctions.CheckPackageVersion(MsfsPackagePath, Parameters.wasmIpcName, Parameters.wasmIpcVersion))
                {
                    task.SetState($"FSUIPC7 is installed, but its installed WASM Module does not match the Minimum Version {Parameters.wasmIpcVersion}!\r\nIt is not required for the Plugin itself, but could lead to Problems with Profiles/Integrations which use Lua-Scripts and L-Vars.\r\nConsider Reinstalling FSUIPC!",
                            TaskState.WAITING);
                    task.Hyperlink = "\r\nFSUIPC";
                    task.HyperlinkURL = "http://fsuipc.com/\r\n";
                }
                else if (!InstallerFunctions.CheckFSUIPC7Pumps())
                {
                    task.SetState("FSUIPC7 is installed, but the FSUIPC7.ini is missing the NumberOfPumps=0 Entry in the [General] Section (which helps to avoid Stutters)!", TaskState.WAITING);
                }
                else
                {
                    task.SetSuccess($"FSUIPC7 at or above minimum Version {Parameters.ipcVersion}!");
                }

                return true;
            }
            else if (!isInstalled)
            {
                task.ReplaceLastMessage($"FSUIPC7 not installed!\r\n\r\nIt is not mandatory to have FSUIPC installed anymore, but still recommended - some Profiles might use Variables/Commands from that Connector.\r\nIf you do not plan to install it, please change the Parameter 'UseFsuipcForMSFS' to false in your 'PluginConfig.json' File!");
                task.State = TaskState.WAITING;
                bool canRunSetup = false;

                string reason = "";
                if (Tools.GetProcessRunning("FlightSimulator") || Tools.GetProcessRunning("FSUIPC7"))
                    reason = "FlightSimulator is running.";
                else
                    canRunSetup = true;

                if (!canRunSetup)
                {
                    task.Message = $"\r\nCan not run Setup: {reason}\r\nPlease install the latest Version manually.";
                    task.Hyperlink = "\r\nFSUIPC";
                    task.HyperlinkURL = "http://fsuipc.com/\r\n";
                    task.SetUrlBold = true;
                    Logger.Log(LogLevel.Error, task.Message);
                }
                else
                {
                    task.Hyperlink = "\r\nInstall FSUIPC7";
                    task.SetUrlBold= true;
                    task.HyperlinkURL = InstallerTask.callbackUrl;
                    task.HyperlinkOnClick = () =>
                    {
                        InstallFSUIPC7(task);
                    };
                    task.SetCompletedOnUrl = true;
                }

                return true;
            }
            else
            {
                string msg = $"FSUIPC7 below minimum Version {Parameters.ipcVersion}!";
                task.SetState(msg, TaskState.ERROR);
                bool canRunSetup = false;

                string reason = "";
                if (Tools.GetProcessRunning("FlightSimulator") || Tools.GetProcessRunning("FSUIPC7"))
                    reason = "FlightSimulator/FSUIPC7 is running.";
                else if (MessageBox.Show($"{msg}\r\nInstall Update now?", "Update FSUIPC", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    reason = "Installation declined by User.";
                else
                    canRunSetup = true;

                if (!canRunSetup)
                {
                    task.ReplaceLastMessage($"{msg}\r\nCan not run Setup: {reason}\r\nPlease install the latest Version manually.");
                    task.Hyperlink = "\r\nFSUIPC";
                    task.HyperlinkURL = "http://fsuipc.com/\r\n";
                    task.SetUrlBold = true;
                    task.State = TaskState.ERROR;
                    Logger.Log(LogLevel.Error, msg);
                    return false;
                }
                
                return InstallFSUIPC7(task);
            }
        }

        public bool InstallFSUIPC7(InstallerTask task)
        {
            task.SetState($"Downloading FSUIPC Installer ...", TaskState.WAITING);
            if (!InstallerFunctions.DownloadFile(Parameters.ipcUrl, Parameters.ipcUrlFile, out string archivePath))
            {
                task.SetError("Could not download FSUIPC Installer!");
                return false;
            }
            string workDir = Path.GetDirectoryName(archivePath);

            task.Message = "Extracting Installer Archive ...";
            try
            {
                Directory.Delete($@"{workDir}\{Parameters.ipcSetup}", true);
            }
            catch { }
            if (!InstallerFunctions.ExtractZipFile(workDir, archivePath))
            {
                task.SetError("Error while extracting FSUIPC Installer!");
                return false;
            }

            task.Message = $"Running FSUIPC Installer - manual Interaction required ...";
            string binPath = $@"{workDir}\{Parameters.ipcSetup}\{Parameters.ipcSetupFile}";
            if (!File.Exists(binPath))
            {
                task.SetError("Could not locate the Installer Binary!");
                return false;
            }

            Tools.RunCommand(binPath);
            Thread.Sleep(1000);

            bool ioSuccessfull = true;
            try
            {
                File.Delete(archivePath);
                Directory.Delete($@"{workDir}\{Parameters.ipcSetup}", true);
            }
            catch
            {
                ioSuccessfull = false;
            }

            if (ioSuccessfull)
                task.SetSuccess($"FSUIPC Version {Parameters.ipcVersion} was installed/updated successfully!");
            else
                task.SetState($"FSUIPC Version {Parameters.ipcVersion} was installed/updated, but the temporary Files could not be removed!", TaskState.WAITING);


            return true;
        }

        private bool CheckFSUIPC6()
        {
            var task = InstallerTask.AddTask("FSUIPC6 Installation", "Check State and Version of FSUIPC6 ...");

            if (InstallerFunctions.CheckFSUIPC6())
            {
                task.SetSuccess($"FSUIPC6 at or above minimum Version {Parameters.ipc6Version}!");

                return true;
            }
            else
            {
                string msg = $"FSUIPC6 below minimum Version {Parameters.ipc6Version}!";
                task.SetState(msg, TaskState.ERROR);
                bool canRunSetup = false;

                string reason = "";
                if (Tools.GetProcessRunning("Prepar3D"))
                    reason = "Prepar3D is running.";
                else if (MessageBox.Show($"{msg}\r\nInstall FSUIPC6 now?", "Install FSUIPC", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    reason = "Installation declined by User.";
                else
                    canRunSetup = true;

                if (!canRunSetup)
                {
                    task.ReplaceLastMessage($"{msg}\r\nCan not run Setup: {reason}\r\nPlease install the latest Version manually.");
                    task.Hyperlink = "\r\nFSUIPC";
                    task.HyperlinkURL = "http://fsuipc.com/\r\n";
                    task.SetUrlBold = true;
                    task.State = TaskState.ERROR;
                    Logger.Log(LogLevel.Error, msg);
                    return false;
                }

                task.SetState($"Downloading FSUIPC Installer ...", TaskState.WAITING);
                if (!InstallerFunctions.DownloadFile(Parameters.ipc6Url, Parameters.ipc6UrlFile, out string archivePath))
                {
                    task.SetError("Could not download FSUIPC Installer!");
                    return false;
                }
                string workDir = Path.GetDirectoryName(archivePath);
                

                task.Message = "Extracting Installer Archive ...";
                try
                {
                    Directory.Delete($@"{workDir}\{Parameters.ipc6Setup}", true);
                }
                catch { }
                if (!InstallerFunctions.ExtractZipFile(workDir, archivePath))
                {
                    task.SetError("Error while extracting FSUIPC Installer!");
                    return false;
                }

                task.Message = $"Running FSUIPC Installer - manual Interaction required ...";
                string binPath = $@"{workDir}\{Parameters.ipc6Setup}\{Parameters.ipc6SetupFile}";
                if (!File.Exists(binPath))
                {
                    task.SetError("Could not locate the Installer Binary!");
                    return false;
                }

                Tools.RunCommand(binPath);
                Thread.Sleep(1000);

                bool ioSuccessfull = true;
                try
                {
                    File.Delete(archivePath);
                    Directory.Delete($@"{workDir}\{Parameters.ipc6Setup}", true);
                }
                catch
                {
                    ioSuccessfull = false;
                }

                if (ioSuccessfull)
                    task.SetSuccess($"FSUIPC Version {Parameters.ipc6Version} was installed/updated successfully!");
                else
                    task.SetState($"FSUIPC Version {Parameters.ipc6Version} was installed/updated, but the temporary Files could not be removed!", TaskState.WAITING);


                return true;
            }
        }

        protected bool CheckMobiFlight()
        {
            var task = InstallerTask.AddTask("MobiFlight Module", "Check State and Version of MobiFlight Event Module ...");
            bool result = false;

            if (InstallerFunctions.CheckPackageVersion(MsfsPackagePath, Parameters.wasmMobiName, Parameters.wasmMobiVersion))
            {
                result = true;
                task.SetSuccess($"Module at or above minimum Version {Parameters.wasmMobiVersion}!");
                return result;
            }
            else
                task.SetState($"Module below minimum Version {Parameters.wasmMobiVersion}!", TaskState.WAITING);

            if (!Tools.GetProcessRunning("FlightSimulator"))
            {
                if (Directory.Exists(MsfsPackagePath + @"\" + Parameters.wasmMobiName))
                {
                    task.Message = "Deleting old Version ...";
                    Directory.Delete(MsfsPackagePath + @"\" + Parameters.wasmMobiName, true);
                }
                task.Message = "Downloading MobiFlight Module ...";
                if (!InstallerFunctions.DownloadFile(Parameters.wasmUrl, Parameters.wasmUrlFile, out string filepath))
                {
                    task.SetError("Could not download MobiFlight Module!");
                    return result;
                }
                task.Message = "Extracting new Version ...";
                if (!InstallerFunctions.ExtractZipFile(MsfsPackagePath, filepath))
                {
                    task.SetError("Error while extracting MobiFlight Module!");
                    return result;
                }
                File.Delete(filepath);

                result = true;
                task.SetSuccess($"MobiFlight Module Version {Parameters.wasmMobiVersion} installed/updated successfully!");
            }
            else
            {
                task.SetError("Can not install/update MobiFlight WASM Module while MSFS is running!");
            }

            return result;
        }

        private bool InstallPlugin()
        {
            InstallerTask task;
            //Stop Deck SW
            if (!App.CmdLineStreamDeck)
            {
                task = InstallerTask.AddTask("PilotsDeck Plugin", "Stopping StreamDeck Software ...");

                if (InstallerFunctions.IsStreamDeckRunning() && !InstallerFunctions.WaitOnStreamDeckClose(10))
                {
                    task.SetError($"The StreamDeck Software could not be stopped!\r\nPlease stop it manually and try again.");

                    return false;
                }

                //Delete Old Binaries
                if (!ResetConfiguration)
                    task.Message = "StreamDeck Software stopped. Deleting old Plugin ...";
                else
                    task.Message = "StreamDeck Software stopped. Deleting old Plugin (and Configuration) ...";
            }
            else
            {
                task = InstallerTask.AddTask("PilotsDeck Plugin", "Deleting old Plugin ...");
                if (ResetConfiguration)
                    task.Message = "Deleting old Plugin (and Configuration) ...";
            }

            

            if (!InstallerFunctions.DeleteOldFiles(ResetConfiguration))
            {
                task.SetError($"The old Binaries could not be removed!\r\nPlease remove them manually and try again.");

                return false;
            }

            //Extract Plugin
            task.Message = "Old Plugin deleted. Extracting new Version ...";
            if (InstallerFunctions.ExtractZip() && InstallerFunctions.CreatePluginFolders())
            {
                task.SetSuccess($"Plugin installed successfully!\r\nPath: %appdata%{Parameters.sdPluginDir}\\{Parameters.pluginName}");
            }
            else
            {
                task.SetError($"Plugin Installation failed!");

                return false;
            }

            return true;
        }

        public bool RemovePlugin()
        {
            IsRunning = true;
            var task = InstallerTask.AddTask("Remove Plugin", "Stopping StreamDeck Software ...");
            try
            {
                if (InstallerFunctions.IsStreamDeckRunning() && !InstallerFunctions.WaitOnStreamDeckClose(10))
                {
                    task.SetError($"The StreamDeck Software could not be stopped!\r\nPlease stop it manually and try again.");

                    return false;
                }

                task.Message = "StreamDeck Software stopped. Deleting Plugin Folder ...";
                Directory.Delete(Parameters.pluginDir, true);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                InstallerTask.CurrentTask.SetError(ex);
                IsRunning = false;
                IsCompleted = true;
                return false;
            }

            IsRunning = false;
            IsCompleted = true;
            IsRemoved = true;
            task.SetSuccess("Plugin removed from the System.");
            return true;
        }

        public static void PlaceDesktopLink()
        {
            InstallerFunctions.PlaceDesktopLink("Profile Manager", "Install Profile Packages and configure your Profiles for automatic Switching", $@"{Parameters.pluginDir}\ProfileManager.exe");
        }

        private bool CheckProfiles()
        {
            //Check Profiles
            var task = InstallerTask.AddTask("Profile Mappings", "Checking for imported Profiles ...");

            string legacyFile = $@"{Parameters.profileDir}\savedProfiles.txt";
            if (File.Exists(legacyFile))
            {
                task.State = TaskState.WAITING;
                task.ReplaceLastMessage("Detected imported Profiles for automatic Switching. You need to reconfigure all your Mappings!\r\nClick on the Link to switch to the ProfileManager Tool:");
                task.Hyperlink = "ProfileManager";
                task.SetUrlBold = true;
                task.HyperlinkURL = Parameters.pluginDir + "\\" + "ProfileManager.exe";
                task.HyperlinkOnClick = () =>
                {
                    PlaceDesktopLink();
                    File.Delete(legacyFile);
                };
                task.SetCompletedOnUrl = true;
            }
            else
            {
                task.State = TaskState.ACTIVE;
                task.ReplaceLastMessage("No legacy Profile Mappings found!\r\nOptional - click the Link to place a Desktop Link for the new Profile Manager Tool:");
                task.Hyperlink = "ProfileManager";
                task.SetCallbackUrl();
                task.HyperlinkOnClick = PlaceDesktopLink;
                task.SetCompletedOnUrl = true;
            }

            return true;
        }

        public void InstallVjoyDriver()
        {
            if (MessageBox.Show($"The vJoy Installer does not ask any further Questions and starts directly with uninstalling the old Version!\r\nContinue?", "Install vJoy Driver", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                vjoyTask.State = TaskState.ACTIVE;
                return;
            }

            vjoyTask.SetState($"Downloading Installer ...", TaskState.WAITING);
            if (!InstallerFunctions.DownloadFile(Parameters.vjoyUrl, Parameters.vjoyUrlFile, out string filepath))
            {
                vjoyTask.SetError("Could not download vJoy Installer!");
                return;
            }

            vjoyTask.Message = $"Installing vJoy Driver ...";
            Tools.RunCommand(filepath);
            File.Delete(filepath);

            vjoyTask.SetSuccess($"vJoy Driver was installed/updated successfully!\r\nPlease consider a Reboot.");
        }

        private static InstallerTask vjoyTask = new InstallerTask("Dummy", "");

        private void ShowVjoyInfo()
        {
            vjoyTask = InstallerTask.AddTask("vJoy Driver", "Checking installed Version ...");
            if (InstallerFunctions.CheckVjoy())
            {
                vjoyTask.DisplayOnlyLastCompleted = true;
                vjoyTask.SetState($"vJoy Driver Version {Parameters.vjoyDisplayVersion} installed.", TaskState.COMPLETED);
            }
            else
            {
                vjoyTask.Message = $"Optional: The vJoy Driver was not detected or does not match the recommended Version ({Parameters.vjoyDisplayVersion}).\r\n" +
                                   "The vJoy Driver enables the Plugin to trigger Joystick Events (recognizable by the Simulator).\r\n" +
                                   "Click the Link to run the Installer:";
                vjoyTask.SetCallbackUrl();
                vjoyTask.DisableLinkAfterClick = false;
                vjoyTask.Hyperlink = Path.GetFileNameWithoutExtension(Parameters.vjoyUrlFile);
                vjoyTask.HyperlinkOnClick = InstallVjoyDriver;
                vjoyTask.SetCompletedOnUrl = true;
            }
        }
    }
}
