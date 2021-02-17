using StreamDeckLib;
using System.Threading.Tasks;
#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace PilotsDeck
{
    public class Plugin
    {
        public static ActionController ActionController { get; } = new ActionController();

        static async Task Main(string[] args)
        {
#if DEBUG
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(500);
                Debugger.Launch();
            }
#endif
            using (var config = StreamDeckLib.Config.ConfigurationBuilder.BuildDefaultConfiguration(args))
            {
                ActionController.Init();
                await ConnectionManager.Initialize(args, config.LoggerFactory, ActionController)
                                                         .RegisterAllActions(typeof(Plugin).Assembly)
                                                         .StartAsync();
            }

        }

    }
}