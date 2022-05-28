using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.switch.korry")]
    public class ActionSwitchKorry : ActionBase<ModelSwitchKorry>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerSwitchKorry(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device)));

            Log.Logger.Debug($"ActionSwitch:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
