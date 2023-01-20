using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.switch")]
    public class ActionSwitch : ActionBase<ModelSwitch>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Plugin.ActionController.RegisterAction(args.context, new HandlerSwitch(args.context, SettingsModel, Plugin.ActionController.GetDeckTypeById(args.device, args.payload.controller)));

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionSwitch:OnWillAppear", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
        }
    }
}
