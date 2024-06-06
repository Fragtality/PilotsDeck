using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public class ActionBase<T> : BaseStreamDeckActionWithSettingsModel<T>
    {
        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            await base.OnWillDisappear(args);
            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionBase:OnWillDisappear", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

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

            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionBase:OnDidReceiveSettings", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
        }

        public override Task OnTitleParametersDidChange(StreamDeckEventPayload args)
        {
            Plugin.ActionController.SetTitleParameters(args.context, args.payload.title, args.payload.titleParameters);

            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionBase:OnTitleParametersDidChange", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID}) (FontStyle: {args.payload.titleParameters.fontStyle.Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("\0", "")})");

            Plugin.ActionController[args.context].Update(true);

            return Task.CompletedTask;
        }

        public override Task OnKeyDown(StreamDeckEventPayload args)
        {
            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionBase:OnKeyDown", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnButtonDown(args.context))
                {
                    PilotsDeck.Logger.Log(LogLevel.Error, "ActionBase:OnKeyDown", $"ButtonDown NOT successful! (Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
                    _ = Manager.ShowAlertAsync(args.context);
                }
            }

            return Task.CompletedTask;
        }

        public override Task OnKeyUp(StreamDeckEventPayload args)
        {
            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionBase:OnKeyUp", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnButtonUp(args.context))
                {
                    PilotsDeck.Logger.Log(LogLevel.Error, "ActionBase:OnKeyUp", $"ButtonUp NOT successful! (Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
                    _ = Manager.ShowAlertAsync(args.context);
                }
                else
                    Plugin.ActionController[args.context].NeedRefresh = true;
            }

            return Task.CompletedTask;
        }

        public override Task OnDialRotate(StreamDeckEventPayload args)
        {
            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionBase:OnDialRotate", $"(Ticks: {args.payload.ticks}) (Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnDialRotate(args.context, args.payload.ticks))
                {
                    PilotsDeck.Logger.Log(LogLevel.Error, "ActionBase:OnDialRotate", $"DialRotate NOT successful! (Ticks: {args.payload.ticks}) (Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
                    _ = Manager.ShowAlertAsync(args.context);
                }
                else
                    Plugin.ActionController[args.context].NeedRefresh = true;
            }

            return Task.CompletedTask;
        }

        public override Task OnTouchTap(StreamDeckEventPayload args)
        {
            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionBase:OnTouchTap", $"(Hold: {args.payload.hold}) (Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

            if (Plugin.ActionController[args.context].HasAction)
            {
                if (!Plugin.ActionController.OnTouchTap(args.context))
                {
                    PilotsDeck.Logger.Log(LogLevel.Error, "ActionBase:OnTouchTap", $"TouchTap NOT successful! (Hold: {args.payload.hold}) (Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");
                    _ = Manager.ShowAlertAsync(args.context);
                }
                else
                    Plugin.ActionController[args.context].NeedRefresh = true;
            }

            return Task.CompletedTask;
        }

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionBase:OnPropertyInspectorDidAppear", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

            return Task.CompletedTask;
        }

        public override Task OnSendToPlugin(StreamDeckEventPayload args)
        {
            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionBase:OnSendToPlugin", $"(Context: {args.context}) (ActionID: {Plugin.ActionController[args.context]?.ActionID})");

            var action = Plugin.ActionController[args.context];
            if (args?.payload?.settings == "propertyInspectorConnected" && action != null)
            {
                if (action.UseFont)
                    _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspectorFonts(action.DeckType.IsEncoder, action is HandlerSwitchKorry));
                else
                    _ = Manager.SendToPropertyInspectorAsync(args.context, new StreamDeckTools.ModelPropertyInspector(action.DeckType.IsEncoder, action is HandlerSwitchKorry));
            }

            return Task.CompletedTask;
        }
    }
}
