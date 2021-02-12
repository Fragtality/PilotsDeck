using Serilog;
using System.Threading.Tasks;
using StreamDeckLib;
using StreamDeckLib.Messages;

namespace PilotsDeck
{
    public class ActionBase<T> : BaseStreamDeckActionWithSettingsModel<T>
    {
        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            await base.OnWillDisappear(args);

            Log.Logger.Verbose($"ActionBase:OnWillDisappear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            Plugin.ActionController.DeregisterAction(args.context);
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);

            Plugin.ActionController.UpdateAction(args.context);

            Log.Logger.Verbose($"ActionBase:OnDidReceiveSettings {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }

        public override Task OnTitleParametersDidChange(StreamDeckEventPayload args)
        {
            Plugin.ActionController.SetTitleParameters(args.context, args.payload.title, args.payload.titleParameters);

            Log.Logger.Verbose($"ActionBase:OnTitleParametersDidChange {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            return Task.CompletedTask;
        }

        public override Task OnKeyUp(StreamDeckEventPayload args)
        {
            Log.Logger.Verbose($"ActionBase:OnKeyUp {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            if ((Plugin.ActionController[args.context] is IHandlerSwitch) &&  !Plugin.ActionController.RunAction(args.context))
                _ = Manager.ShowAlertAsync(args.context);

            return Task.CompletedTask;
        }

        public override async Task OnApplicationDidLaunchAsync(StreamDeckEventPayload args)
        {
            Log.Logger.Verbose($"ActionBase:OnApplicationDidLaunchAsync {args.payload.application}");
        }

        public override async Task OnApplicationDidTerminateAsync(StreamDeckEventPayload args)
        {
            Log.Logger.Verbose($"ActionBase:OnApplicationDidTerminateAsync {args.payload.application}");
        }

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            if (Plugin.ActionController[args.context].UseFont)
                _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspectorFonts());
            else
                _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspector());

            Log.Logger.Verbose($"ActionBase:OnPropertyInspectorDidAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            return Task.CompletedTask;
        }

    }
}
