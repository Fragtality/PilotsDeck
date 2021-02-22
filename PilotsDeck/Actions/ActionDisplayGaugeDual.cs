using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using Serilog;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display.gauge.dual")]
    public class ActionDisplayGaugeDual : ActionBase<ModelDisplayGaugeDual>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerDisplayGaugeDual(args.context, SettingsModel));

            Log.Logger.Debug($"ActionDisplayGauge:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
