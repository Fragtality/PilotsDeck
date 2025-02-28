using CFIT.Installer.Tasks;
using System.IO;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public class WorkerLegacyProfiles : TaskWorker<Config>
    {
        public WorkerLegacyProfiles(Config config) : base(config, "Profile Mappings", "Checking for imported Profiles ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayCompleted = false;
        }

        protected override async Task<bool> DoRun()
        {
            string legacyFile = Path.Combine(Config.ProductPathProfiles, "savedProfiles.txt");
            if (File.Exists(legacyFile))
            {
                Model.SetSuccess($"Detected imported Profiles for automatic Switching. You need to reconfigure all your Mappings in the {Config.ProfileManagerName}!\r\nClick on the Link to open the Tool:");
                Model.State = TaskState.WAITING;

                Model.AddLink(Config.ProfileManagerName, Config.ProfileManagerExePath);
                Model.DisplayInSummary = true;
            }
            else
            {
                Model.SetSuccess("No legacy Profile Mappings found!");
            }

            await Task.Delay(0);
            return true;
        }
    }
}
