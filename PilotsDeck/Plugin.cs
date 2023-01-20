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
                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                        .WriteTo.File("log/PilotsDeck.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3, outputTemplate: "{Timestamp:yy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}")
                        .MinimumLevel.Debug();
                Log.Logger = loggerConfiguration.CreateLogger();

                Log.Logger.Information("PLUGIN STARTED");
                Log.Logger.Information("---------------------------------------------------------------------------");
                

                ActionController.Init();
                await ConnectionManager.Initialize(args, ActionController)
                                                            .RegisterAllActions(typeof(Plugin).Assembly)
                                                            .StartAsync();

                Log.Logger.Information("---------------------------------------------------------------------------");
                Log.Logger.Information("PLUGIN CLOSED");
            }
            catch (Exception e)
            {
                Log.Logger.Fatal("---------------------------------------------------------------------------");
                Log.Logger.Fatal(e.Source);
                Log.Logger.Fatal(e.Message);
                Log.Logger.Fatal(e.StackTrace);
                Log.Logger.Fatal("---------------------------------------------------------------------------");
                Log.Logger.Fatal("PLUGIN CRASHED");
                throw;
            }
        }

    }
}