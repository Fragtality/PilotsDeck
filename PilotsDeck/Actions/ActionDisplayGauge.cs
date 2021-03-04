using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using Serilog;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display.gauge")]
    public class ActionDisplayGauge : ActionBase<ModelDisplayGauge>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerDisplayGauge(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device)));

            Log.Logger.Debug($"ActionDisplayGauge:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
