using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display.gauge.dual")]
    public class ActionDisplayGaugeDual : ActionBase<ModelDisplayGaugeDual>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerDisplayGaugeDual(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device, args.payload.controller)));

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionDisplayGaugeDual:OnWillAppear", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
        }
    }
}
