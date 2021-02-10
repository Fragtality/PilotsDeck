using StreamDeckLib;
using System.Threading.Tasks;
#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace PilotsDeck
{
    public class PilotsDeckPlugin
    {

        static async Task Main(string[] args)
        {
//#if DEBUG
//            while (!Debugger.IsAttached)
//            {
//                Thread.Sleep(500);
//                Debugger.Launch();
//            }
//#endif
            using (var config = StreamDeckLib.Config.ConfigurationBuilder.BuildDefaultConfiguration(args))
            {
                await ConnectionManager.Initialize(args, config.LoggerFactory)
                                                         .RegisterAllActions(typeof(PilotsDeckPlugin).Assembly)
                                                         .StartAsync();
            }

        }

    }
}