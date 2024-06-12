using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.profile.switcher")]
    public class ActionProfileSwitcher : BaseStreamDeckActionWithSettingsModel<ModelProfileSwitcher>
    {
        public static HandlerProfileSwitcher ProfileSwitcher { get { return Plugin.ActionController.ProfileSwitcher; } }

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);
            
            ProfileSwitcher.RegisterProfileSwitcher(args.context);
            
            SettingsModel.CopySettings(ProfileSwitcher.GlobalProfileSettings);
            await Manager.SetSettingsAsync(args.context, SettingsModel);

            SetActionImage(Manager, args.context, SettingsModel.EnableSwitching);

            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionProfileSwitcher:OnWillAppear", $"(Context: {args.context})");
        }

        public override Task OnWillDisappear(StreamDeckEventPayload args)
        {
            base.OnWillDisappear(args);

            ProfileSwitcher.DeregisterProfileSwitcher(args.context);

            PilotsDeck.Logger.Log(LogLevel.Verbose, "ActionProfileSwitcher:OnWillDisappear", $"(Context: {args.context})");
            return Task.CompletedTask;
        }

        public static void SetActionImage(ConnectionManager manager, string context, bool switchState)
        {
            if (switchState)
                manager.SetImageAsync(context, @"Images\category\StreamDeck_BtnOn.png");
            else
                manager.SetImageAsync(context, @"Images\category\StreamDeck_BtnOff.png");
        }

        public override Task OnKeyUp(StreamDeckEventPayload args)
        {
            base.OnKeyUp(args);

            SettingsModel.CopySettings(ProfileSwitcher.GlobalProfileSettings);
            SettingsModel.EnableSwitching = !SettingsModel.EnableSwitching;
            if (ProfileSwitcher.UpdateGlobalSettings(SettingsModel))
                Manager.SendToPropertyInspectorAsync(args.context, ProfileSwitcher.GlobalProfileSettings);

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionProfileSwitcher:OnKeyUp", $"(Context: {args.context})");
            return Task.CompletedTask;
        }

        public override Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            base.OnDidReceiveSettings(args);

            SetActionImage(Manager, args.context, SettingsModel.EnableSwitching);

            SettingsModel.CopySettings(ProfileSwitcher.GlobalProfileSettings);
            SettingsModel.EnableSwitching = !SettingsModel.EnableSwitching;
            if (ProfileSwitcher.UpdateGlobalSettings(SettingsModel))
                Manager.SendToPropertyInspectorAsync(args.context, ProfileSwitcher.GlobalProfileSettings);

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionProfileSwitcher:OnDidReceiveSettings", $"(Context: {args.context})");
            return Task.CompletedTask;
        }

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            base.OnPropertyInspectorDidAppear(args);

            SettingsModel.CopySettings(ProfileSwitcher.GlobalProfileSettings);
            Manager.SendToPropertyInspectorAsync(args.context, SettingsModel);

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionProfileSwitcher:OnPropertyInspectorDidAppear", $"(Context: {args.context})");
            return Task.CompletedTask;
        }

        public override Task OnSendToPlugin(StreamDeckEventPayload args)
        {
            base.OnSendToPlugin(args);

            SettingsModel.CopySettings(ProfileSwitcher.GlobalProfileSettings);
            if (ProfileSwitcher.UpdateGlobalSettings(SettingsModel))
                Manager.SendToPropertyInspectorAsync(args.context, ProfileSwitcher.GlobalProfileSettings);

            PilotsDeck.Logger.Log(LogLevel.Debug, "ActionProfileSwitcher:OnSendToPlugin", $"(Context: {args.context})");
            return Task.CompletedTask;
        }
    }
}
