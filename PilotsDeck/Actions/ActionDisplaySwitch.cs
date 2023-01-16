using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display.switch")]
    public class ActionDisplaySwitch : ActionBase<ModelDisplaySwitch>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerDisplaySwitch(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device, args.payload.controller)));

            Log.Logger.Debug($"ActionDisplaySwitch:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
