using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public class WorkerRemoveHotSpot : TaskWorker<Config>
    {
        public WorkerRemoveHotSpot(Config config) : base(config, "HotSpot Plugin", "Removing HotSpot Plugin ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
        }

        protected override async Task<bool> DoRun()
        {
            await Task.Delay(500);

            try
            {
                string hotSpotPath = Config.HotSpotPluginPath;

                if (!Directory.Exists(hotSpotPath))
                {
                    Model.SetSuccess("HotSpot Plugin not found, nothing to remove.");
                    return true;
                }

                Logger.Debug($"Removing HotSpot Plugin from '{hotSpotPath}'");
                Model.Message = $"Removing HotSpot Plugin from '{hotSpotPath}' ...";

                // Stop the plugin if it's running
                if (Sys.GetProcessRunning(Config.PluginBinary))
                {
                    Logger.Debug($"Stopping {Config.PluginBinary} process");
                    Sys.KillProcess(Config.PluginBinary);
                    await Task.Delay(750);
                }

                // Delete the HotSpot plugin directory
                FuncIO.DeleteDirectory(hotSpotPath, true, true);
                await Task.Delay(500);

                if (Directory.Exists(hotSpotPath))
                {
                    Model.SetError($"Failed to remove HotSpot Plugin directory.");
                    return false;
                }

                Model.SetSuccess($"HotSpot Plugin successfully removed from '{hotSpotPath}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Model.SetError($"Error removing HotSpot Plugin: {ex.Message}");
                return false;
            }
        }
    }
}