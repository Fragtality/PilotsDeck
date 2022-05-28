using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display.radio")]
    public class ActionDisplayRadio : ActionBase<ModelDisplayRadio>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerDisplayRadio(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device)));

            Log.Logger.Debug($"ActionDisplayRadio:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
