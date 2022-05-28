using Serilog;
using StreamDeckLib;
using System;
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
            try
            {
                using var config = StreamDeckLib.Config.ConfigurationBuilder.BuildDefaultConfiguration(args);
                ActionController.Init();
                await ConnectionManager.Initialize(args, config.LoggerFactory, ActionController)
                                                            .RegisterAllActions(typeof(Plugin).Assembly)
                                                            .StartAsync();
            }
            catch (Exception e)
            {
                Log.Logger.Fatal("---------------------------------------------------------------------------");
                Log.Logger.Fatal("PLUGIN CRASHED");
                Log.Logger.Fatal(e.Source);
                Log.Logger.Fatal(e.Message);
                Log.Logger.Fatal(e.StackTrace);
                throw;
            }
        }

    }
}