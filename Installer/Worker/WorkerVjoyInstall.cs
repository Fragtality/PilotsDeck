using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Tasks;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Installer.Worker
{
    public class WorkerVjoyInstall : TaskWorker<Config>
    {
        public virtual string VjoyUrl { get; set; } = "https://github.com/BrunnerInnovation/vJoy/releases/download/v2.2.2.0/vJoySetup_v2.2.2.0_Win10_Win11.exe";
        public virtual string VjoyUrlFile { get; set; } = "vJoySetup_v2.2.2.0_Win10_Win11.exe";
        public static string VjoyRegPath { get; set; } = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{8E31F76F-74C3-47F1-9550-E041EEDC5FBB}_is1";
        public static string VjoyRegValue { get; set; } = "DisplayVersion";
        public virtual string VjoyVersion { get; set; } = "2.2.2.0";

        public WorkerVjoyInstall(Config config) : base(config, "vJoy Driver", "Checking installed Version ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = false;

            SetPropertyFromConfig<string>("VjoyUrl");
            SetPropertyFromConfig<string>("VjoyUrlFile");
            SetPropertyFromConfig<string>("VjoyVersion");
        }

        protected override async Task<bool> DoRun()
        {
            await Task.Delay(0);

            if (CheckVersion(VjoyVersion))
            {
                Model.SetSuccess($"vJoy Driver Version {VjoyVersion} installed.");
                return true;
            }
            else
            {
                Model.DisplayInSummary = true;
                Model.DisplayCompleted = true;
                bool newInstall = !IsInstalled();

                if (newInstall)
                    Model.AddMessage(new TaskMessage($"vJoy Driver is not installed!", false, FontWeights.DemiBold), true, false);
                else
                    Model.AddMessage(new TaskMessage($"The installed Version does not match the target Version {Config.VjoyVersion}!", false, FontWeights.DemiBold), true, false);

                Model.State = TaskState.WAITING;

                Model.Message = "Downloading vJoy Installer ...";
                string installer = await FuncIO.DownloadFile(Token, VjoyUrl, VjoyUrlFile);
                if (string.IsNullOrWhiteSpace(installer))
                {
                    Model.SetError("Could not download vJoy Installer!");
                    return false;
                }

                Model.Message = $"Installing vJoy Driver ...";
                Sys.RunCommand($"\"{installer}\"", out _);
                await Task.Delay(500);
                FuncIO.DeleteFile(installer);

                Model.SetSuccess($"vJoy Driver was installed/updated successfully!");
                if (newInstall)
                    Model.AddMessage(new TaskMessage("Please consider a Reboot!\nNOTE: You need to enable a virtual Joystick and set it to 128 Buttons with the 'vJoyConf' Tool!", true, FontWeights.DemiBold), false, false);
                else
                    Model.AddMessage(new TaskMessage("Please consider a Reboot!", true, FontWeights.DemiBold), false, false);
                return true;
            }
        }

        public static bool CheckVersion(string vjoyVersion)
        {
            try
            {
                string regVersion = Sys.GetRegistryValue<string>(VjoyRegPath, VjoyRegValue, null);
                if (!string.IsNullOrWhiteSpace(regVersion) && regVersion == vjoyVersion)
                    return true;
            }
            catch (Exception ex)
            {
                TaskStore.CurrentTask?.SetError(ex);
            }

            return false;
        }

        public static bool IsInstalled()
        {
            try
            {
                string regVersion = Sys.GetRegistryValue<string>(VjoyRegPath, VjoyRegValue, null);
                return !string.IsNullOrWhiteSpace(regVersion);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }
    }
}
