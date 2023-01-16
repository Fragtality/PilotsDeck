using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public class ActionBase<T> : BaseStreamDeckActionWithSettingsModel<T>
    {
        protected long ticksDown = 0;

        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            await base.OnWillDisappear(args);

            Log.Logger.Debug($"ActionBase:OnWillDisappear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            Plugin.ActionController.DeregisterAction(args.context);
        }

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            Log.Logger.Debug($"ActionBase:OnWillAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);

            Plugin.ActionController.UpdateAction(args.context);
            
            if (Plugin.ActionController[args.context].UpdateSettingsModel)
            {
                _ = Manager.SetSettingsAsync(args.context, SettingsModel);
                Plugin.ActionController[args.context].UpdateSettingsModel = false;
            }

            Log.Logger.Debug($"ActionBase:OnDidReceiveSettings {args.context} | {Plugin.ActionController[args.context]?.ActionID}");
        }

        public override Task OnTitleParametersDidChange(StreamDeckEventPayload args)
        {
            Plugin.ActionController.SetTitleParameters(args.context, args.payload.title, args.payload.titleParameters);

            Log.Logger.Debug($"ActionBase:OnTitleParametersDidChange {args.context} | {Plugin.ActionController[args.context]?.ActionID} | {args.payload.titleParameters.fontStyle.Replace("\n","").Replace("\r", "").Replace("\t", "").Replace("\0", "")}");

            if (Plugin.ActionController[args.context].DeckType.IsEncoder)
                Plugin.ActionController[args.context].Update();

            return Task.CompletedTask;
        }

        public override Task OnKeyDown(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionBase:OnKeyDown {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnButtonDown(args.context))
                {
                    Log.Logger.Error($"ActionBase: OnButtonDown NOT successful (Long: {Plugin.ActionController.Ticks - ticksDown >= AppSettings.longPressTicks}) for Action {Plugin.ActionController[args.context]?.ActionID}");
                    _ = Manager.ShowAlertAsync(args.context);
                }
            }

            return Task.CompletedTask;
        }

        public override Task OnKeyUp(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionBase:OnKeyUp {args.context} | {Plugin.ActionController[args.context]?.ActionID} | Ticks: {Plugin.ActionController.Ticks - ticksDown}");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnButtonUp(args.context))
                {
                    Log.Logger.Error($"ActionBase: OnButtonUp NOT successful (Long: {Plugin.ActionController.Ticks - ticksDown >= AppSettings.longPressTicks}) for Action {Plugin.ActionController[args.context]?.ActionID}");
                    _ = Manager.ShowAlertAsync(args.context);
                }
            }
            ticksDown = 0;

            return Task.CompletedTask;
        }

        public override Task OnDialRotate(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionBase:OnDialRotate Ticks: {args.payload.ticks} | {args.context} | {Plugin.ActionController[args.context]?.ActionID} | Ticks: {Plugin.ActionController.Ticks - ticksDown}");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnDialRotate(args.context, args.payload.ticks))
                {
                    Log.Logger.Error($"ActionBase: OnDialRotate NOT successful for Action {Plugin.ActionController[args.context]?.ActionID}");
                    _ = Manager.ShowAlertAsync(args.context);
                }
            }
            ticksDown = 0;

            return Task.CompletedTask;
        }

        public override Task OnTouchTap(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionBase:OnTouchTap {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnTouchTap(args.context))
                {
                    Log.Logger.Error($"ActionBase: OnTouchTap NOT successful for Action {Plugin.ActionController[args.context]?.ActionID}");
                    _ = Manager.ShowAlertAsync(args.context);
                }
            }
            ticksDown = 0;

            return Task.CompletedTask;
        }

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionBase:OnPropertyInspectorDidAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            return Task.CompletedTask;
        }

        public override Task OnSendToPlugin(StreamDeckEventPayload args)
        {
            var action = Plugin.ActionController[args.context];
            if (args?.payload?.settings == "propertyInspectorConnected" && action != null)
            {
                if (action.UseFont)
                    _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspectorFonts(action.DeckType.IsEncoder, action is HandlerSwitchKorry));
                else
                    _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspector(action.DeckType.IsEncoder, action is HandlerSwitchKorry));
            }

            Log.Logger.Debug($"ActionBase:OnSendToPlugin {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            return Task.CompletedTask;
        }
    }
}
