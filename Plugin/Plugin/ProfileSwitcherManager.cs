using PilotsDeck.Resources.Images;
using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.Plugin
{
    public class ProfileSwitcherManager
    {
        public List<ProfileMapping> ProfileMappings { get; protected set; } = [];
        public int Count { get { return ProfileMappings.Count; } }
        public ModelGlobalSettings GlobalSettings { get; protected set; } = new();
        public List<string> ProfileSwitcherActions { get; protected set; } = [];
        public Dictionary<string,string> ActiveDecks { get; protected set; } = [];
        protected static DeckController DeckController { get { return App.DeckController; } }
        protected static SimController SimController { get { return App.SimController; } }
        protected string LastKnownAircraft { get; set; } = "";
        public string ActivePropertyInspector { get; set; } = "";
        protected List<string> PropertyInspectors { get; set; } = [];

        public void LoadProfileMappings()
        {
            try
            {
                ProfileMappings = ProfileMapping.LoadProfileMappings();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                ProfileMappings = [];
            }
        }

        public ModelProfileSwitcherInspector CreatePropertyInspectorModel()
        {
            var model = new ModelProfileSwitcherInspector(GlobalSettings)
            {
                LoadedMappings = ProfileMappings.Select(m => m.ProfileName).ToList(),
                AircraftPathString = SimController.AircraftString
            };

            return model;
        }

        public void OnDidReceiveGlobalSettings(StreamDeckEvent args)
        {
            GlobalSettings = ModelGlobalSettings.Create(args);
            _ = DeckController.SendSetGlobalSettings(GlobalSettings.Serialize());
            UpdateProfileSwitchers();
            Logger.Information($"Received Configuration for Profile Switching. (Switching Enabled: {GlobalSettings.EnableSwitching} | Switch Back Enabled: {GlobalSettings.SwitchBack})");
        }

        public static void SetActionImage(string context, bool switchState)
        {
            if (switchState)
                _ = DeckController.SendSetImageRaw(context, ToolsImage.ReadImageFile64(@"Plugin\Images\StreamDeck_BtnOn.png"));
            else
                _ = DeckController.SendSetImageRaw(context, ToolsImage.ReadImageFile64(@"Plugin\Images\StreamDeck_BtnOff.png"));
        }

        public void SetAddPropertyInspector(string context)
        {
            Logger.Debug($"Adding new ProfileSwitcher PI: {context}");
            ActivePropertyInspector = context;
            PropertyInspectors.Add(ActivePropertyInspector);
        }

        public void RemoveClearPropertyInspector(string context)
        {
            Logger.Debug($"Removing ProfileSwitcher PI: {context}");
            PropertyInspectors.Remove(context);
            if (ActivePropertyInspector == context)
                ActivePropertyInspector = "";
        }

        public void ToggleEnableSwitching()
        {
            GlobalSettings.EnableSwitching = !GlobalSettings.EnableSwitching;
            _ = DeckController.SendSetGlobalSettings(GlobalSettings.Serialize());
            UpdateProfileSwitchers();
        }

        public void ToggleSwitchBack()
        {
            GlobalSettings.SwitchBack = !GlobalSettings.SwitchBack;
            _ = DeckController.SendSetGlobalSettings(GlobalSettings.Serialize());
            UpdateProfileSwitchers();
        }

        public void OnSendToPlugin(StreamDeckEvent sdEvent)
        {
            if (sdEvent.payload?.settings?.GetValue<string>() == "toggleEnableSwitching")
                ToggleEnableSwitching();
            if (sdEvent.payload?.settings?.GetValue<string>() == "toggleSwitchBack")
                ToggleSwitchBack();
        }

        public void UpdateProfileSwitchers()
        {
            foreach (var switcher in ProfileSwitcherActions)
                SetActionImage(switcher, GlobalSettings.EnableSwitching);

            UpdateActivePropertyInspector();
        }

        public void UpdateActivePropertyInspector()
        {
            if (!string.IsNullOrWhiteSpace(ActivePropertyInspector))
                _ = DeckController.SendToPropertyInspector(ActivePropertyInspector, CreatePropertyInspectorModel());
        }

        public bool HasContext(string context)
        {
            return ProfileSwitcherActions.Contains(context);
        }

        public void RegisterProfileSwitcher(string context)
        {
            if (!ProfileSwitcherActions.Contains(context))
            {
                ProfileSwitcherActions.Add(context);
                SetActionImage(context, GlobalSettings.EnableSwitching);
                Logger.Debug($"ProfileSwitcher registered. (Context: {context})");
            }
            else
                Logger.Error($"ProfileSwitcher already registered! (Context: {context})");
        }

        public void DeregisterProfileSwitcher(string context)
        {
            if (ProfileSwitcherActions.Remove(context))
            {
                Logger.Debug($"ProfileSwitcher deregistered. (Context: {context})");
            }
            else
                Logger.Warning($"ProfileSwitcher not registered or failed! (Context: {context})");
        }

        public static bool MatchAircraftStrings(string currentAircraft, List<string> aircraftStrings)
        {
            return aircraftStrings.Any(m => currentAircraft.ToLowerInvariant().Contains(m.ToLowerInvariant()));
        }

        public bool CanSwitch
        {
            get
            {
                return SimController.IsReadySession && !string.IsNullOrEmpty(SimController.AircraftString) && LastKnownAircraft != SimController.AircraftString;
            }
        }

        public void ResetAircraft()
        {
            LastKnownAircraft = "";
        }

        public void SwitchProfiles()
        {
            if (!GlobalSettings.EnableSwitching)
                return;

            try
            {
                string currentAircraft = SimController.AircraftString;
                if (currentAircraft == LastKnownAircraft)
                {
                    Logger.Information($"Skipping Switch Request for '{currentAircraft}' (already set as LastKnownAircraft)");
                    return;
                }
                else
                    LastKnownAircraft = currentAircraft;

                ActiveDecks = [];
                foreach (var device in DeckController.DeckInfo.devices)
                    ActiveDecks.Add(device.id, null);



                var queryAircraft = ProfileMappings.Where(m => m.AircraftProfile && m.AircraftStrings.Count > 0);
                Logger.Information($"Searching {queryAircraft.Count()} valid Profiles for Aircraft '{currentAircraft}' ...");
                foreach (var deviceMapping in queryAircraft)
                {
                    if (!ActiveDecks.TryGetValue(deviceMapping.DeckId, out string varDeck))
                    {
                        Logger.Debug($"Skipping StreamDeck '{deviceMapping.DeckName}' - not connected / not in list!");
                        continue;
                    }
                    else if (varDeck != null)
                    {
                        Logger.Debug($"Skipping StreamDeck '{deviceMapping.DeckName}' - already mapped!");
                        continue;
                    }

                    if (MatchAircraftStrings(currentAircraft, deviceMapping.AircraftStrings))
                    {
                        Logger.Information($"Found Match for '{deviceMapping.ProfileName}' against: {string.Join(" | ", deviceMapping.AircraftStrings)}");
                        ActiveDecks[deviceMapping.DeckId] = deviceMapping.ProfilePath;
                    }
                }


                var queryDefault = ProfileMappings.Where(m => m.DefaultProfile && m.DefaultSimulator == SimController.SimMainType);
                Logger.Information($"Searching {queryDefault.Count()} valid Profiles for Simulator '{SimController.SimMainType}' ...");
                foreach (var deviceMapping in queryDefault)
                {
                    if (!ActiveDecks.TryGetValue(deviceMapping.DeckId, out string varDeck))
                    {
                        Logger.Debug($"Skipping StreamDeck '{deviceMapping.DeckName}' - not connected / not in list!");
                        continue;
                    }
                    else if (varDeck != null)
                    {
                        Logger.Debug($"Skipping StreamDeck '{deviceMapping.DeckName}' - already mapped!");
                        continue;
                    }

                    Logger.Information($"Using first Match on '{deviceMapping.ProfileName}' for Default Simulator Profile");
                    ActiveDecks[deviceMapping.DeckId] = deviceMapping.ProfilePath;
                }


                foreach (var deck in ActiveDecks)
                {
                    if (deck.Value != null)
                    {
                        Logger.Information($"Switching Deck '{deck.Key}' to Profile '{deck.Value}'");
                        _ = DeckController.SendSwitchToProfile(DeckController.PluginContext, deck.Key, deck.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void SwitchBack()
        {
            if (GlobalSettings.SwitchBack)
            {
                try
                {
                    foreach (var deck in DeckController.DeckInfo.devices)
                    {
                        if (deck != null)
                        {
                            var profile = ProfileMappings.Where(p => p.SwitchBackProfile && p.DeckId == deck.id).FirstOrDefault() ?? null;
                            if (profile != null)
                            {
                                Logger.Information($"Switching Deck '{deck.id}' to Profile '{profile.ProfileUUID}'");
                                _ = DeckController.SendSwitchToProfile(DeckController.PluginContext, deck.id, profile.ProfilePath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
            LastKnownAircraft = "";
        }
    }
}
