using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display.gauge")]
    public class ActionDisplayGauge : ActionBase<ModelDisplayGauge>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerDisplayGauge(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device, args.payload.controller)));

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionDisplayGauge:OnWillAppear", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
        }
    }
}
