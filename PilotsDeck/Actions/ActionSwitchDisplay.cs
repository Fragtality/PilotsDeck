using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.switch.display")]
    public class ActionSwitchDisplay : ActionBase<ModelSwitchDisplay>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerSwitchDisplay(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device)));

            Log.Logger.Debug($"ActionSwitchDisplay:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }
    }
}
