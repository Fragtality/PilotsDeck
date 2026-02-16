using System.IO;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Product;
using CFIT.Installer.UI;
using Installer.Worker;

namespace Installer
{
    public class WorkerManager : WorkerManagerBase
    {
        public virtual Config Config { get { return BaseConfig as Config; } }

        public WorkerManager(ConfigBase config) : base(config)
        {
            FuncStreamDeck.PluginBinary = Config.PluginBinary;
        }

        protected void CreateInstallUpdateTasks(SetupMode key)
        {
            bool hotSpotStreamDockInstall = Config?.GetOption<bool>(Config.OptionHotSpotStreamDockInstall) == true;

            WorkerQueues[key].Enqueue(new WorkerDotNet<Config>(Config));

            // StreamDeck software check (only for non-HotSpot installations)
            if (!Config.IgnoreStreamDeck && !hotSpotStreamDockInstall)
                WorkerQueues[key].Enqueue(new WorkerStreamDeckSoftware(Config));

            WorkerQueues[key].Enqueue(new WorkerCheckSimulators(Config));
            WorkerQueues[key].Enqueue(new WorkerFsuipc7(Config, Simulator.MSFS2020));
            WorkerQueues[key].Enqueue(new WorkerFsuipc7(Config, Simulator.MSFS2024));
            WorkerQueues[key].Enqueue(new WorkerMobi(Config, Simulator.MSFS2020));
            WorkerQueues[key].Enqueue(new WorkerMobi(Config, Simulator.MSFS2024));
            WorkerQueues[key].Enqueue(new WorkerFsuipc6(Config, Simulator.P3DV4));
            WorkerQueues[key].Enqueue(new WorkerFsuipc6(Config, Simulator.P3DV5));
            WorkerQueues[key].Enqueue(new WorkerFsuipc6(Config, Simulator.P3DV6));

            // Stop plugin/software before installation
            if (!Config.IgnoreStreamDeck && !hotSpotStreamDockInstall)
                WorkerQueues[key].Enqueue(new WorkerStreamDeckStartStop<Config>(Config, DeckProcessOperation.STOP) { StartStopDelay = 1 });
            else if (hotSpotStreamDockInstall)
                WorkerQueues[key].Enqueue(new WorkerStreamDockStartStop(Config, DeckProcessOperation.STOP) { StartStopDelay = 1 });

            WorkerQueues[key].Enqueue(new WorkerInstallUpdate(Config));
            WorkerQueues[key].Enqueue(new WorkerLegacyProfiles(Config));

            if (Config?.GetOption<bool>(Config.OptionVjoyInstallUpdate) == true)
            {
                var worker = new WorkerVjoyInstall(Config);
                if (key == SetupMode.INSTALL)
                    worker.Model.DisplayInSummary = true;
                WorkerQueues[key].Enqueue(worker);
            }

            // Start plugin/software after installation
            if (!Config.IgnoreStreamDeck && !hotSpotStreamDockInstall)
                WorkerQueues[key].Enqueue(new WorkerStreamDeckStartStop<Config>(Config, DeckProcessOperation.START) { RefocusWindow = true, RefocusWindowTitle = InstallerWindow.WindowTitle, StartStopDelay = 1 });
            else if (hotSpotStreamDockInstall)
                WorkerQueues[key].Enqueue(new WorkerStreamDockStartStop(Config, DeckProcessOperation.START) { RefocusWindow = true, RefocusWindowTitle = InstallerWindow.WindowTitle, StartStopDelay = 1 });

            if (Config?.GetOption<bool>(ConfigBase.OptionDesktopLink) == true)
                WorkerQueues[key].Enqueue(new WorkerDesktopLink(Config, DesktopLinkOperation.CREATE));
        }

        protected override void CreateInstallTasks()
        {
            CreateInstallUpdateTasks(SetupMode.INSTALL);
        }

        protected override void CreateRemovalTasks()
        {
            // Determine which installation(s) exist
            bool hasHotSpotInstallation = Directory.Exists(Config.HotSpotPluginPath);
            bool hasStreamDeckInstallation = false;
            try
            {
                string streamDeckPath = $@"{FuncStreamDeck.DeckPluginPath}\com.extension.pilotsdeck.sdPlugin";
                hasStreamDeckInstallation = Directory.Exists(streamDeckPath);
            }
            catch { }

            // Stop the appropriate software
            if (!Config.IgnoreStreamDeck)
            {
                if (hasHotSpotInstallation && !hasStreamDeckInstallation)
                {
                    // Only HotSpot installation exists - stop StreamDock
                    WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerStreamDockStartStop(Config, DeckProcessOperation.STOP) { StartStopDelay = 1 });
                }
                else if (hasStreamDeckInstallation)
                {
                    // StreamDeck installation exists - stop StreamDeck
                    WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerStreamDeckStartStop<Config>(Config, DeckProcessOperation.STOP) { StartStopDelay = 1 });
                }
            }

            // Remove main installation
            WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerAppRemove<Config>(Config));

            // Also remove HotSpot installation if it exists
            if (hasHotSpotInstallation)
            {
                var workerHotSpotRemove = new WorkerRemoveHotSpot(Config);
                workerHotSpotRemove.Model.DisplayCompleted = true;
                workerHotSpotRemove.Model.DisplayInSummary = true;
                WorkerQueues[SetupMode.REMOVE].Enqueue(workerHotSpotRemove);
            }

            var workerDesktop = new WorkerDesktopLink(Config, DesktopLinkOperation.REMOVE);
            workerDesktop.Model.DisplayCompleted = true;
            workerDesktop.Model.DisplayInSummary = true;
            WorkerQueues[SetupMode.REMOVE].Enqueue(workerDesktop);

            // Restart the appropriate software
            if (!Config.IgnoreStreamDeck)
            {
                if (hasHotSpotInstallation && !hasStreamDeckInstallation)
                {
                    // Only HotSpot installation existed - restart StreamDock
                    WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerStreamDockStartStop(Config, DeckProcessOperation.START) { RefocusWindow = true, RefocusWindowTitle = InstallerWindow.WindowTitle, IgnorePluginRunning = true, StartStopDelay = 1 });
                }
                else if (hasStreamDeckInstallation)
                {
                    // StreamDeck installation existed - restart StreamDeck
                    WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerStreamDeckStartStop<Config>(Config, DeckProcessOperation.START) { RefocusWindow = true, RefocusWindowTitle = InstallerWindow.WindowTitle, IgnorePluginRunning = true, StartStopDelay = 1 });
                }
            }
        }

        protected override void CreateUpdateTasks()
        {
            CreateInstallUpdateTasks(SetupMode.UPDATE);
        }
    }
}
