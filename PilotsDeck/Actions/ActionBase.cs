using Serilog;
using System.Threading.Tasks;
using StreamDeckLib;
using StreamDeckLib.Messages;

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

            Log.Logger.Debug($"ActionBase:OnTitleParametersDidChange {args.context} | {Plugin.ActionController[args.context]?.ActionID} | {args.payload.titleParameters.fontStyle}");

            return Task.CompletedTask;
        }

        //public override Task OnKeyDown(StreamDeckEventPayload args)
        //{
        //    Log.Logger.Debug($"ActionBase:OnKeyDown {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

        //    if (Plugin.ActionController[args.context] is IHandlerSwitch)
        //        ticksDown = Plugin.ActionController.Ticks;


        //    return Task.CompletedTask;
        //}

        //public override Task OnKeyUp(StreamDeckEventPayload args)
        //{
        //    Log.Logger.Debug($"ActionBase:OnKeyUp {args.context} | {Plugin.ActionController[args.context]?.ActionID} | Ticks: {Plugin.ActionController.Ticks - ticksDown}");

        //    if (Plugin.ActionController[args.context] is IHandlerSwitch)
        //    {
        //        if (!Plugin.ActionController.RunAction(args.context, Plugin.ActionController.Ticks - ticksDown >= AppSettings.longPressTicks))
        //        {
        //            Log.Logger.Error($"ActionBase: RunAction NOT successful (Long: {Plugin.ActionController.Ticks - ticksDown >= AppSettings.longPressTicks}) for Action {Plugin.ActionController[args.context]?.ActionID}");
        //            _ = Manager.ShowAlertAsync(args.context);
        //        }
        //        else
        //        {
        //            Log.Logger.Information($"ActionBase: RunAction successful (Long: {Plugin.ActionController.Ticks - ticksDown >= AppSettings.longPressTicks}) for Action {Plugin.ActionController[args.context]?.ActionID}");
        //        }
        //        ticksDown = 0;
        //    }

        //    return Task.CompletedTask;
        //}

        public override Task OnKeyDown(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionBase:OnKeyDown {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            if (Plugin.ActionController[args.context] is IHandlerSwitch)
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

            if (Plugin.ActionController[args.context] is IHandlerSwitch)
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

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            if (Plugin.ActionController[args.context].UseFont)
                _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspectorFonts());
            else
                _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspector(Plugin.ActionController[args.context] is HandlerSwitchKorry));

            Log.Logger.Debug($"ActionBase:OnPropertyInspectorDidAppear {args.context} | {Plugin.ActionController[args.context]?.ActionID}");

            return Task.CompletedTask;
        }

    }
}
