using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Product;
namespace Installer
{
    public class Definition : ProductDefinition
    {
        public Config Config { get { return BaseConfig as Config; } }
        public WorkerManager WorkerManager { get { return BaseWorker as WorkerManager; } }

        public Definition(string[] args) : base(args)
        {

        }

        protected override void CreateConfig()
        {
            BaseConfig = new Config();
        }

        protected override void CreateWorker()
        {
            BaseWorker = new WorkerManager(Config);
        }

        protected override void ParseArguments(string[] args)
        {
            base.ParseArguments(args);
            if (Sys.HasArgument(args, "--ignoremsfs20"))
            {
                Config.IgnoreMsfs2020 = true;
                Logger.Information("Installer was started with IgnoreMSFS (2020)");
            }
            if (Sys.HasArgument(args, "--ignoremsfs24"))
            {
                Config.IgnoreMsfs2024 = true;
                Logger.Information("Installer was started with IgnoreMSFS (2024)");
            }
        }

        protected override void CreateWindowBehavior()
        {
            base.CreateWindowBehavior();
            BaseBehavior.MaxTasksShown = 6;
            BaseBehavior.CheckRunning = false;
            BaseBehavior.ShowInstallationWarnings = false;
            BaseBehavior.WelcomeLogoWidth = 192;
            BaseBehavior.WelcomeLogoResource = "Payload/icon";
        }

        protected override void CreatePageWelcome()
        {
            PageBehaviors.Add(InstallerPages.WELCOME, new PageWelcomePilotsDeck());
        }

        protected override void CreatePageConfig()
        {
            PageBehaviors.Add(InstallerPages.CONFIG, new ConfigPage());
        }
    }
}