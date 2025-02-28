using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Tasks;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public class WorkerStreamDeckSoftware : TaskWorker<Config>
    {
        public virtual string DeckVersionMinimum { get; set; }
        public virtual string DeckVersionTarget { get; set; }
        public virtual string DeckUrl { get; set; }
        public virtual string DeckInstaller { get; set; }

        protected FuncStreamDeck StreamDeck { get; set; }

        public WorkerStreamDeckSoftware(Config config, string title = "StreamDeck Software", string message = "Checking Software Version ...") : base(config, title, message)
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = false;

            SetPropertyFromConfig<string>("DeckVersionMinimum");
            SetPropertyFromConfig<string>("DeckVersionTarget");
            SetPropertyFromConfig<string>("DeckUrl");
            SetPropertyFromConfig<string>("DeckInstaller");
        }

        protected override async Task<bool> DoRun()
        {
            StreamDeck = new FuncStreamDeck();
            if (!StreamDeck.IsValid)
            {
                Model.SetError("Could not get StreamDeck Version/Path!");
                return false;
            }

            if (StreamDeck.CompareVersion(Config.DeckVersionMinimum) && StreamDeck.CompareVersion(Config.DeckVersionTarget))
            {
                Model.SetSuccess($"The installed Software is at Version {Config.DeckVersionTarget} or greater.");
                return true;
            }

            if (StreamDeck.CompareVersion(Config.DeckVersionMinimum) && !StreamDeck.CompareVersion(Config.DeckVersionTarget))
            {
                Model.SetSuccess($"The installed Software Version meets the Minimum Requirements but is outdated.\r\nPlease consider updating the StreamDeck Software to Version {Config.DeckVersionTarget} or greater.");
                Model.State = TaskState.WAITING;
                Model.AddLink("StreamDeck Software", Config.DeckUrl);
                Model.DisplayInSummary = true;

                return true;
            }

            Model.DisplayInSummary = true;
            Model.DisplayCompleted = true;
            Model.SetState($"The StreamDeck Software is not installed or outdated!\r\nDownloading Installer ...", TaskState.WAITING);
            string installerPath = await FuncIO.DownloadFile(Token, Config.DeckUrl, Config.DeckInstaller);
            if (string.IsNullOrWhiteSpace(installerPath))
            {
                Model.SetError("Could not download StreamDeck Installer!");
                return false;
            }

            Model.Message = $"Starting interactive Installer ...";
            Sys.RunCommand(installerPath, out _);
            await Task.Delay(1000);
            FuncIO.DeleteFile(installerPath);

            StreamDeck = new FuncStreamDeck();
            bool result = StreamDeck.CompareVersion(Config.DeckVersionTarget);
            if (result)
                Model.SetSuccess($"StreamDeck Software {Config.DeckVersionTarget} installed!");
            else
                Model.SetError($"Setup failed.");

            return result;
        }
    }
}
