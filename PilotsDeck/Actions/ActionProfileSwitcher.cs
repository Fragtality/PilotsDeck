using StreamDeckLib;
using StreamDeckLib.Messages;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;

namespace PilotsDeck
{
    [ActionUuid(Uuid = "com.extension.pilotsdeck.action.profile.switcher")]
    public class ActionProfileSwitcher : BaseStreamDeckActionWithSettingsModel<ModelProfileSwitcher>
    {
        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            SetActionImage(args.context, SettingsModel.EnableSwitching);
            SettingsModel.ProfileMappings = new List<ModelProfileSwitcher.Profile>();
            SettingsModel.MappingsJson = "";
            await Manager.SetSettingsAsync(args.context, SettingsModel);

            Log.Logger.Verbose($"ActionProfileSwitcher:OnWillAppear {args.context}");
        }

        private void SetActionImage(string context, bool switchState)
        {
            if (switchState)
                Manager.SetImageAsync(context, @"Images\category\StreamDeck_BtnOn.png");
            else
                Manager.SetImageAsync(context, @"Images\category\StreamDeck_BtnOff.png");
        }

        public override Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            base.OnDidReceiveSettings(args);

            SetActionImage(args.context, SettingsModel.EnableSwitching);

            Manager.SetGlobalSettingsAsync(Manager.PluginUUID, SettingsModel);
            Manager.GetGlobalSettingsAsync(Manager.PluginUUID);

            Log.Logger.Verbose($"ActionProfileSwitcher:OnDidReceiveSettings {args.context}");
            return Task.CompletedTask;
        }

        public override Task OnPropertyInspectorDidAppear(StreamDeckEventPayload args)
        {
            base.OnPropertyInspectorDidAppear(args);

            SetActionImage(args.context, Plugin.ActionController.GlobalProfileSettings.EnableSwitching);
            Plugin.ActionController.LoadProfiles();

            Log.Logger.Verbose($"ActionProfileSwitcher:OnPropertyInspectorDidAppear {args.context}");
            Plugin.ActionController.GlobalProfileSettings.ExportToJson();
            Manager.SetSettingsAsync(args.context, Plugin.ActionController.GlobalProfileSettings);

            return Task.CompletedTask;
        }
    }
}
