using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using Serilog;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.switch")]
    public class ActionSwitch : ActionBase<ModelSwitch>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerSwitch(args.context, SettingsModel));

            Log.Logger.Debug($"ActionSwitch:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
