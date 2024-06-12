using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck
{
    public class HandlerProfileSwitcher
    {
        public List<ProfileMapping> ProfileMappings { get; protected set; } = [];
        public int Count { get { return ProfileMappings.Count; } }
        public ModelProfileSwitcher GlobalProfileSettings { get; protected set; } = new();
        public List<string> ProfileSwitcherActions { get; protected set; } = [];
        public Dictionary<string,string> ActiveDecks { get; protected set; } = [];
        protected static ConnectionManager DeckManager { get { return Plugin.ActionController.DeckManager; } }
        protected static SimulatorConnector SimConnector { get { return Plugin.ActionController.SimConnector; } }
        protected string LastKnownAircraft { get; set; } = "";

        public void LoadProfileMappings()
        {
            try
            {
                ProfileMappings = ProfileMapping.LoadProfileMappings();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "HandlerProfileSwitcher:LoadProfileMappings", $"Exception '{ex.GetType}' reading Mappings: {ex.Message}");
                ProfileMappings = [];
            }
        }

        public void OnDidReceiveGlobalSettings(StreamDeckEventPayload args)
        {
            StreamDeckEventPayload.SetModelProperties(args, GlobalProfileSettings);
            UpdateProfileSwitchers();

            Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:OnDidReceiveGlobalSettings", $"Received Configuration for Profile Switching. (Switching Enabled: {GlobalProfileSettings.EnableSwitching})");
        }

        public List<string> GetProfileListForPI()
        {
            return ProfileMappings.Select(m => m.ProfileName).ToList();
        }

        public bool UpdateGlobalSettings(ModelProfileSwitcher settings)
        {
            if (GlobalProfileSettings.EnableSwitching != settings.EnableSwitching)
            {
                GlobalProfileSettings.EnableSwitching = settings.EnableSwitching;
                UpdateProfileSwitchers();
                DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);
                
                return true;
            }
            else
                return false;
        }

        public void UpdateProfileSwitchers()
        {
            foreach (var switcher in ProfileSwitcherActions)
                ActionProfileSwitcher.SetActionImage(DeckManager, switcher, GlobalProfileSettings.EnableSwitching);
        }

        public void RegisterProfileSwitcher(string context)
        {
            if (!ProfileSwitcherActions.Contains(context))
            {
                ProfileSwitcherActions.Add(context);
                Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:RegisterProfileSwitcher", $"ProfileSwitcher registered. (Context: {context})");
            }
            else
                Logger.Log(LogLevel.Error, "HandlerProfileSwitcher:RegisterProfileSwitcher", $"ProfileSwitcher already registered! (Context: {context})");
        }

        public void DeregisterProfileSwitcher(string context)
        {
            if (ProfileSwitcherActions.Remove(context))
            {
                Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:DeregisterProfileSwitcher", $"ProfileSwitcher deregistered. (Context: {context})");
            }
            else
                Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:DeregisterProfileSwitcher", $"ProfileSwitcher not registered or failed! (Context: {context})");
        }

        public static bool MatchAircraftStrings(string currentAircraft, List<string> aircraftStrings)
        {
            return aircraftStrings.Any(m => currentAircraft.Contains(m));
        }

        public bool CanSwitch
        {
            get
            {
                return SimConnector.IsReady && !string.IsNullOrEmpty(SimConnector.AicraftPathString) && LastKnownAircraft != SimConnector.AicraftPathString;
            }
        }

        public void SwitchProfiles()
        {
            string currentAircraft = SimConnector.AicraftPathString;
            if (currentAircraft == LastKnownAircraft)
            {
                Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchProfiles", $"Skipping Switch Request for '{currentAircraft}' (already set as LastKnownAircraft)");
                return;
            }
            else
                LastKnownAircraft = currentAircraft;

            ActiveDecks = [];
            foreach (var device in DeckManager.Info.devices)
                ActiveDecks.Add(device.id, null);
            


            var queryAircraft = ProfileMappings.Where(m => m.AircraftProfile && m.AircraftStrings.Count > 0);
            Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchProfiles", $"Searching {queryAircraft.Count()} valid Profiles for Aircraft '{currentAircraft}' ...");
            foreach (var deviceMapping in queryAircraft)
            {
                if (!ActiveDecks.TryGetValue(deviceMapping.DeckId, out string varDeck))
                {
                    Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:SwitchProfiles", $"Skipping StreamDeck '{deviceMapping.DeckName}' - not connected / not in list!");
                    continue;
                }
                else if (varDeck != null)
                {
                    Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:SwitchProfiles", $"Skipping StreamDeck '{deviceMapping.DeckName}' - already mapped!");
                    continue;
                }

                if (MatchAircraftStrings(currentAircraft, deviceMapping.AircraftStrings))
                {
                    Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchProfiles", $"Found Match for '{deviceMapping.ProfileName}' against: {string.Join(" | ", deviceMapping.AircraftStrings)}");
                    ActiveDecks[deviceMapping.DeckId] = deviceMapping.ProfilePath;
                }
            }


            var queryDefault = ProfileMappings.Where(m => m.DefaultProfile && m.DefaultSimulator == SimConnector.SimType);
            Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchProfiles", $"Searching {queryDefault.Count()} valid Profiles for Simulator '{SimConnector.SimType}' ...");
            foreach (var deviceMapping in queryDefault)
            {
                if (!ActiveDecks.TryGetValue(deviceMapping.DeckId, out string varDeck))
                {
                    Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:SwitchProfiles", $"Skipping StreamDeck '{deviceMapping.DeckName}' - not connected / not in list!");
                    continue;
                }
                else if (varDeck != null)
                {
                    Logger.Log(LogLevel.Debug, "HandlerProfileSwitcher:SwitchProfiles", $"Skipping StreamDeck '{deviceMapping.DeckName}' - already mapped!");
                    continue;
                }

                Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchProfiles", $"Using first Match on '{deviceMapping.ProfileName}' for Default Simulator Profile");
                ActiveDecks[deviceMapping.DeckId] = deviceMapping.ProfilePath;
            }


            foreach (var deck in ActiveDecks)
            {
                if (deck.Value != null)
                {
                    Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchProfiles", $"Switching Deck '{deck.Key}' to Profile '{deck.Value}'");
                    _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, deck.Key, deck.Value);
                }
            }
        }

        public void SwitchToDefaultProfile()
        {
            if (GlobalProfileSettings.EnableSwitching)
            {
                foreach (var deck in ActiveDecks)
                {
                    if (deck.Value != null)
                    {
                        Logger.Log(LogLevel.Information, "HandlerProfileSwitcher:SwitchToDefaultProfile", $"Switching back Profile on Deck '{deck}'.");
                        _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, deck.Key, null);
                    }
                }

                LastKnownAircraft = "";
            }
        }
    }
}
