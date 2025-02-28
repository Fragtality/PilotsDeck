using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public enum DesktopLinkOperation
    {
        CREATE = 1,
        REMOVE = 2,
    }

    public class WorkerDesktopLink : TaskWorker<Config>
    {
        public DesktopLinkOperation Operation { get; set; }

        public WorkerDesktopLink(Config config, DesktopLinkOperation operation) : base(config, "Desktop Link", "Creating Link ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
            Operation = operation;
        }

        protected virtual bool CreateLink()
        {
            return Sys.CreateLink(Config.ProfileManagerName, Config.ProfileManagerExePath, $"Start {Config.ProfileManagerName}");
        }

        protected virtual bool RemoveLink()
        {
            string link = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{Config.ProfileManagerName}.lnk");

            if (File.Exists(link))
                File.Delete(link);
            else
            {
                Model.DisplayCompleted = false;
                Model.DisplayInSummary = false;
            }

            return !File.Exists(link);
        }

        protected override async Task<bool> DoRun()
        {
            await Task.Delay(0);
            bool result = false;
            if (Operation == DesktopLinkOperation.CREATE)
            {
                result = CreateLink();
                if (result)
                    Model.SetSuccess($"Link for {Config.ProfileManagerName} placed on Desktop!");
            }
            else if (Operation == DesktopLinkOperation.REMOVE)
            {
                result = RemoveLink();
                if (result)
                    Model.SetSuccess($"Link for {Config.ProfileManagerName} removed from Desktop!");
            }

            return result;
        }
    }
}
