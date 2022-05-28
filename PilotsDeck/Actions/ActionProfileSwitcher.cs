using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.profile.switcher")]
    public class ActionProfileSwitcher : BaseStreamDeckActionWithSettingsModel<ModelProfileSwitcher>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);
            
            Plugin.ActionController.RegisterProfileSwitcher(args.context);
            
            SettingsModel.CopySettings(Plugin.ActionController.GlobalProfileSettings);
            await Manager.SetSettingsAsync(args.context, SettingsModel);

            SetActionImage(Manager, args.context, SettingsModel.EnableSwitching);

            Log.Logger.Debug($"ActionProfileSwitcher:OnWillAppear {args.context}");
        }

        public override Task OnWillDisappear(StreamDeckEventPayload args)
        {
            base.OnWillDisappear(args);

            Plugin.ActionController.DeregisterProfileSwitcher(args.context);

            Log.Logger.Debug($"ActionProfileSwitcher:OnWillDisappear {args.context}");
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

            SettingsModel.CopySettings(Plugin.ActionController.GlobalProfileSettings);
            SettingsModel.EnableSwitching = !SettingsModel.EnableSwitching;
            Plugin.ActionController.UpdateGlobalSettings(SettingsModel);
            Manager.SendToPropertyInspectorAsync(args.context, SettingsModel);

            Plugin.ActionController.UpdateProfileSwitchers();

            Log.Logger.Debug($"ActionProfileSwitcher:OnKeyUp {args.context}");
            return Task.CompletedTask;
        }

        public override Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            base.OnDidReceiveSettings(args);

            SetActionImage(Manager, args.context, SettingsModel.EnableSwitching);

            Plugin.ActionController.UpdateGlobalSettings(SettingsModel);
            //Plugin.ActionController.LoadProfiles();
            SettingsModel.CopySettings(Plugin.ActionController.GlobalProfileSettings);
            Manager.SendToPropertyInspectorAsync(args.context, SettingsModel);

            Plugin.ActionController.UpdateProfileSwitchers();

            Log.Logger.Debug($"ActionProfileSwitcher:OnDidReceiveSettings {args.context}");
            return Task.CompletedTask;
        }

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            base.OnPropertyInspectorDidAppear(args);

            SettingsModel.CopySettings(Plugin.ActionController.GlobalProfileSettings);
            Manager.SendToPropertyInspectorAsync(args.context, SettingsModel);

            Log.Logger.Debug($"ActionProfileSwitcher:OnPropertyInspectorDidAppear {args.context}");
            return Task.CompletedTask;
        }

        public override Task OnSendToPlugin(StreamDeckEventPayload args)
        {
            base.OnSendToPlugin(args);

            Plugin.ActionController.LoadProfiles();
            SettingsModel.CopySettings(Plugin.ActionController.GlobalProfileSettings);
            Manager.SendToPropertyInspectorAsync(args.context, SettingsModel);

            Log.Logger.Debug($"ActionProfileSwitcher:OnSendToPlugin {args.context}");
            return Task.CompletedTask;
        }
    }
}
