using Serilog;
using System.Threading.Tasks;
using StreamDeckLib;
using StreamDeckLib.Messages;

namespace PilotsDeck
{
    public class ActionBase<T> : BaseStreamDeckActionWithSettingsModel<T>
    {
        public virtual void OnWillAppear(StreamDeckEventPayload args, IHandler actionHandler)
        {
            Manager.ActionController.RegisterAction(args.context, actionHandler);

            Log.Logger.Verbose($"ActionBase:OnWillAppear {args.context} | {actionHandler.ActionID}");
        }

        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            await base.OnWillDisappear(args);

            Log.Logger.Verbose($"ActionBase:OnWillDisappear {args.context} | {Manager.ActionController[args.context]?.ActionID}");

            Manager.ActionController.DeregisterAction(args.context);
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);

            Manager.ActionController.UpdateAction(args.context);

            Log.Logger.Verbose($"ActionBase:OnDidReceiveSettings {args.context} | {Manager.ActionController[args.context]?.ActionID}");
        }

        public override async Task OnTitleParametersDidChange(StreamDeckEventPayload args)
        {
            await base.OnTitleParametersDidChange(args);
            
            Manager.ActionController.SetTitleParameters(args.context, args.payload.title, args.payload.titleParameters);

            await Manager.SetSettingsAsync(args.context, SettingsModel);

            Log.Logger.Verbose($"ActionBase:OnTitleParametersDidChange {args.context} | {Manager.ActionController[args.context]?.ActionID}");
        }

        public override async Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            await base.OnPropertyInspectorDidAppear(args);

            if (Manager.ActionController[args.context] is IHandlerDisplay)
                await Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspectorFonts());
            else
                await Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspector());

            Log.Logger.Verbose($"ActionBase:OnPropertyInspectorDidAppear {args.context} | {Manager.ActionController[args.context]?.ActionID}");
        }

    }
}
