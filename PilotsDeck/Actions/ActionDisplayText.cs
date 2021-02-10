using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.display")]
    public class ActionDisplayText : ActionBase<ModelDisplayText>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            base.OnWillAppear(args, new HandlerDisplayText(args.context, SettingsModel));
        }
    }
}
