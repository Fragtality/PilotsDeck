using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using Serilog;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.switch.korry")]
    public class ActionSwitchKorry : ActionBase<ModelSwitchKorry>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerSwitchKorry(args.context, SettingsModel));

            Log.Logger.Debug($"ActionSwitch:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
