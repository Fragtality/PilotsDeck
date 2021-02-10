using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using Serilog;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.switch.toggle")]
    public class ActionSwitchDisplay : ActionBase<ModelSwitchDisplay>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            base.OnWillAppear(args, new HandlerSwitchDisplay(args.context, SettingsModel));
        }

        public override async Task OnKeyUp(StreamDeckEventPayload args)
        {
            await base.OnKeyUp(args);

            Log.Logger.Verbose($"ActionSwitch:OnKeyUp {args.context} | {Manager.ActionController[args.context]?.ActionID}");

            if (!Manager.ActionController.RunAction(args.context))
            {
                await Manager.ShowAlertAsync(args.context);
                Log.Logger.Error($"Could not send command to Sim! Address: {args.context} | {Manager.ActionController[args.context]?.ActionID}");
            }
        }
    }
}
