using Newtonsoft.Json.Linq;
using Serilog;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PilotsDeck
{
    public class ActionController : IActionController
    {
        private Dictionary<string, IHandler> currentActions = null;
        private IPCManager ipcManager = null;
        private ImageManager imgManager = null;

        public ConnectionManager DeckManager { get; set; }
        public int Timing
        {
            get
            {
                return AppSettings.pollInterval;
            }
        }
        public SimulatorConnector SimConnector { get; set; }

        public long Ticks { get { return tickCounter; } }
        private long tickCounter = 0;
        private int waitCounter = 0;

        private bool wasPaused = false;
        private readonly int waitTicks = AppSettings.waitTicks;
        private readonly int firstTick = (int)(AppSettings.waitTicks / 7.5);
        private Stopwatch watchRefresh = new();
        private Stopwatch watchLoading = new();
        private double averageTime = 0;
        private bool redrawRequested = false;
        private bool redrawAlways = AppSettings.redrawAlways;

        public ModelProfileSwitcher GlobalProfileSettings { get; protected set; } = new ModelProfileSwitcher();
        private List<string> profileSwitcherActions = new();
        private List<string> switchedDecks = new();
        private List<StreamDeckProfile> manifestProfiles;
        private string lastAircraft = "";


        public ActionController()
        {
            currentActions = new Dictionary<string, IHandler>();
            ipcManager = new IPCManager();
            SimConnector = SimulatorConnector.CreateConnector("", tickCounter, ipcManager);
            imgManager = new ImageManager();
            manifestProfiles = new List<StreamDeckProfile>();
        }

        public void Init()
        {
            if (currentActions != null && ipcManager != null && imgManager != null && manifestProfiles != null)
            {
                SimConnector = new ConnectorDummy();
                Log.Logger.Information($"ActionController successfully initialized. Poll-Time {AppSettings.pollInterval}ms / Wait-Ticks {waitTicks} / Redraw Always {redrawAlways}");
            }

            dynamic manifest = JObject.Parse(File.ReadAllText(@"manifest.json"));
            if (manifest?.Profiles != null)
            {
                foreach (var profile in manifest.Profiles)
                {
                    string name = profile?.Name;
                    if (!string.IsNullOrEmpty(name) && profile?.DeviceType != null)
                    {
                        manifestProfiles.Add(new StreamDeckProfile(name, (int)profile.DeviceType, "") );
                    }
                }
            }
            Log.Logger.Information($"Loaded {manifestProfiles.Count} StreamDeck Profiles from Manifest.");

            AppSettings.SetLocale();
            Log.Logger.Information($"Locale is set to \"{AppSettings.locale}\" - FontStyles: {AppSettings.fontDefault} / {AppSettings.fontBold} / {AppSettings.fontItalic}");
        }

        public void Dispose()
        {
            ipcManager.Dispose();
            imgManager.Dispose();
            GC.SuppressFinalize(this);
            Log.Logger.Information("ActionController and IPCManager Disposed");
        }

        public int Length => currentActions.Count;

        public IHandler this[string context]
        {
            get
            {
                if (currentActions.ContainsKey(context))
                    return currentActions[context];
                else
                    return null;
            }
        }

        public StreamDeckType GetDeckTypeById(string device)
        {
            var deckInfo = DeckManager.Info.devices.Where(d => d.id == device);
            if (deckInfo.Any())
                return (StreamDeckType)deckInfo.First().type;
            else
                return StreamDeckType.StreamDeck;
        }

        public void OnGlobalEvent(StreamDeckEventPayload args)
        {
            switch (args.Event)
            {
                case "applicationDidLaunch":
                    OnApplicationDidLaunch(args);
                    break;
                case "applicationDidTerminate":
                    OnApplicationDidTerminate(args);
                    break;
                case "didReceiveGlobalSettings":
                    OnDidReceiveGlobalSettings(args);
                    break;
                default:
                    break;
            }
        }

        protected void OnDidReceiveGlobalSettings(StreamDeckEventPayload args)
        {
            StreamDeckEventPayload.SetModelProperties(args, GlobalProfileSettings);
            GlobalProfileSettings.UpdateSettings(manifestProfiles, DeckManager.Info.devices);
            DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);

            UpdateProfileSwitchers();

            //For Test/Debugging - clear GlobalSettings
            //GlobalProfileSettings = new ModelProfileSwitcher();
            //GlobalProfileSettings.UpdateSettings(manifestProfiles, DeckManager.Info.devices);
            //DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);
            //DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, null);

            Log.Logger.Information($"ActionController:OnDidReceiveGlobalSettings - Switching => {GlobalProfileSettings.EnableSwitching} | DeviceMappings => {GlobalProfileSettings?.DeviceMappings?.Count} | Installed => {GlobalProfileSettings.ProfilesInstalled}");
        }

        protected void OnApplicationDidLaunch(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionController:OnApplicationDidLaunchAsync {args.payload.application}");

            SimConnector.Dispose();
            SimConnector = SimulatorConnector.CreateConnector(args.payload.application, tickCounter, ipcManager);
        }

        protected void OnApplicationDidTerminate(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionController:OnApplicationDidTerminateAsync {args.payload.application}");

            SimConnector.Close();
        }

        public void UpdateProfileSwitchers()
        {
            foreach (var switcher in profileSwitcherActions)
                ActionProfileSwitcher.SetActionImage(DeckManager, switcher, GlobalProfileSettings.EnableSwitching);
        }

        public void RegisterProfileSwitcher(string context)
        {
            if (!profileSwitcherActions.Contains(context))
            {
                profileSwitcherActions.Add(context);
                Log.Logger.Debug($"ActionController:RegisterProfileSwitcher ProfileSwitcher registered [{context}]");
            }
            else
                Log.Logger.Error($"ActionController:RegisterProfileSwitcher ProfileSwitcher already registered! [{context}]");
        }

        public void DeregisterProfileSwitcher(string context)
        {
            if (profileSwitcherActions.Contains(context))
            {
                profileSwitcherActions.Remove(context);
                Log.Logger.Debug($"ActionController:DeregisterProfileSwitcher ProfileSwitcher deregistered [{context}]");
            }
            else
                Log.Logger.Error($"ActionController:DeregisterProfileSwitcher ProfileSwitcher not registered! [{context}]");
        }

        protected void SwitchProfiles()
        {
            foreach (var deviceMapping in GlobalProfileSettings.DeviceMappings)
            {
                string switchTo = "";

                if (string.IsNullOrEmpty(SimConnector.AicraftString) && deviceMapping.UseDefault && !string.IsNullOrEmpty(deviceMapping.DefaultProfile))
                    switchTo = deviceMapping.DefaultProfile;
                else if (deviceMapping.Profiles != null && deviceMapping.Profiles.Count > 0)
                {
                    foreach (var profile in deviceMapping.Profiles)
                    {
                        if (ModelProfileSwitcher.IsInProfile(profile.Mappings, SimConnector.AicraftString))
                        {
                            switchTo = profile.Name;
                            break;
                        }
                    }

                    if (switchTo == "" && deviceMapping.UseDefault && !string.IsNullOrEmpty(deviceMapping.DefaultProfile))
                        switchTo = deviceMapping.DefaultProfile;
                }

                if (switchTo != "")
                {
                    Log.Logger.Information($"ActionController: Current Aircraft [{SimConnector.AicraftString}] matched -> switching to [{switchTo}] on StreamDeck [{deviceMapping.Name}]");
                    _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, deviceMapping.ID, switchTo);
                    if (!switchedDecks.Contains(deviceMapping.ID))
                        switchedDecks.Add(deviceMapping.ID);
                }
            }

            lastAircraft = SimConnector.AicraftString;
        }

        protected void SwitchToDefaultProfile()
        {
            if (GlobalProfileSettings.EnableSwitching && GlobalProfileSettings.ProfilesInstalled)
            {
                foreach (var deck in switchedDecks)
                {
                    Log.Logger.Information($"ActionController: Switching back profile on Deck {deck}");
                    _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, deck, null);
                }
            }
        }

        public void LoadProfiles()
        {
            if (!GlobalProfileSettings.ProfilesInstalled)
            {
                var deckTypes = new int[] { (int)StreamDeckType.StreamDeck, (int)StreamDeckType.StreamDeckXL, (int)StreamDeckType.StreamDeckMini, (int)StreamDeckType.StreamDeckMobile };
                List<string> decksToInstall = new();
                foreach (int deckType in deckTypes)
                {
                    if (manifestProfiles.Where(p => p.Type == deckType).Any())
                    {
                        var decks = DeckManager.Info.devices.Where(d => d.type == deckType);
                        foreach (var deck in decks)
                        {
                            decksToInstall.Add(deck.id);
                        }
                    }

                }

                foreach (var deck in decksToInstall)
                {
                    Log.Logger.Information($"ActionController: Profile install on deck {deck}");
                    DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, deck, "Profiles/install");
                }

                decksToInstall.Clear();

                GlobalProfileSettings.ProfilesInstalled = true;
                DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);
            }
        }

        public void UpdateGlobalSettings(ModelProfileSwitcher settings)
        {
            GlobalProfileSettings.CopySettings(settings);
            DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);
        }

        public void Run(CancellationToken token)
        {
            watchRefresh.Restart();
            bool resultProcess = false;
            bool forceRefresh = false;
            tickCounter++;
            SimConnector.TickCounter = tickCounter;

            if (tickCounter == 1)
                _ = DeckManager.GetGlobalSettingsAsync(DeckManager.PluginUUID);

            if (tickCounter < firstTick) //wait till streamdeck<>plugin init is done ( <150> / 7.5 = 20 Ticks => 20 * <200> = 4s )
                return;

            if (waitCounter > 0)
            {
                if (SimConnector.IsRunning)
                {
                    waitCounter--;
                    if (waitCounter == 0)
                        Log.Logger.Information($"ActionController: Wait ended");
                    else if (waitCounter % 25 == 0)
                        Log.Logger.Information($"ActionController: Waiting ...");
                }
                else
                    waitCounter = 0;
            }          

            if (!SimConnector.IsRunning) //SIM not running
            {
                if (SimConnector.LastStateApp()) //SIM changed to not running
                {
                    SimConnector.Close();
                    lastAircraft = "";
                    wasPaused = false;
                    CallOnAll(handler => handler.SetDefault());
                    redrawRequested = true;
                    if (GlobalProfileSettings.EnableSwitching)
                        SwitchToDefaultProfile();
                }
                else if (lastAircraft != "")
                {
                    lastAircraft = "";
                    wasPaused = false;
                    CallOnAll(handler => handler.SetDefault());
                    redrawRequested = true;
                }
            }
            else //SIM running
            {
                if (!SimConnector.LastStateApp()) //SIM changed to running
                {
                    CallOnAll(handler => handler.SetWait());
                    redrawRequested = true;
                    if (tickCounter > firstTick)
                    {
                        waitCounter = waitTicks * 2;
                        Log.Logger.Information($"ActionController: Sim changed to running, waiting for {(waitCounter * 200) / 1000}s");
                    }
                    else
                    {
                        Log.Logger.Information($"ActionController: Sim changed to running, directly connect");
                        SimConnector.Connect();
                    }
                }

                if (!SimConnector.IsConnected && waitCounter == 0) //NOT connected
                {
                    if (SimConnector.LastStateConnect())
                    {
                        CallOnAll(handler => handler.SetError());
                        redrawRequested = true;
                        waitCounter = waitTicks / 2;
                        Log.Logger.Information($"ActionController: Sim changed to disconnected, waiting for {(waitCounter * 200) / 1000}s");
                    }

                    if (!SimConnector.Connect())
                    {
                        waitCounter = waitTicks;
                        Log.Logger.Information($"ActionController: Sim Connection failed, waiting for {(waitCounter * 200) / 1000}s");
                    }
                    else
                        Log.Logger.Information($"ActionController: Sim is connected");
                }
                else if (SimConnector.IsConnected) //CONNECTED
                {
                    if (!SimConnector.LastStateConnect()) //changed to connected
                    {
                        CallOnAll(handler => handler.SetWait());
                        redrawRequested = true;
                        if (tickCounter > firstTick)
                        {
                            waitCounter = waitTicks / 2;
                            Log.Logger.Information($"ActionController: Sim changed to connected, waiting for {(waitCounter * 200) / 1000}s");
                        }
                    }

                    if (waitCounter == 0 && SimConnector.IsReady && !SimConnector.IsPaused) //process possible
                    {
                        //if (tickCounter % 5 == 0)
                        //    Log.Logger.Debug("Process()");
                        resultProcess = ipcManager.Process();
                        if (resultProcess)
                        {
                            if (!SimConnector.LastStateProcess() || wasPaused)
                            {
                                redrawRequested = true;
                                forceRefresh = true;
                                if (wasPaused)
                                {
                                    wasPaused = false;
                                    Log.Logger.Information($"ActionController: Sim is unpaused");
                                }
                                else
                                    Log.Logger.Information($"ActionController: Process OK, force Redraw");
                            }
                        }
                        else
                        {
                            CallOnAll(handler => handler.SetError());
                            redrawRequested = true;
                            waitCounter = waitTicks / 2;                            
                            Log.Logger.Warning($"ActionController: Process failed, waiting for {(waitCounter * 200) / 1000}s");
                        }
                    }
                    else if (SimConnector.IsPaused)
                    {
                        if (!wasPaused)
                        {
                            wasPaused = true;
                            CallOnAll(handler => handler.SetWait());
                            redrawRequested = true;
                            Log.Logger.Information($"ActionController: Sim is paused");
                        }
                    }
                    else if (!SimConnector.IsReady && waitCounter == 0)
                    {
                        waitCounter = waitTicks / 6;
                        SimConnector.Process();
                        CallOnAll(handler => handler.SetWait());
                        redrawRequested = true;
                        wasPaused = true;
                        Log.Logger.Information($"ActionController: Sim not ready, waiting for {(waitCounter * 200) / 1000}s");
                    }
                }
            }
            if (tickCounter % (waitTicks / 2) == 0 && SimConnector.IsReady && !SimConnector.IsPaused)
            {
                Log.Logger.Information($"ActionController: Forcing Redraw");
                redrawRequested = true;
                forceRefresh = true;
            }
            RefreshActions(token, forceRefresh); //every 15s force update
            RedrawAll(token);

            watchRefresh.Stop();
            averageTime += watchRefresh.Elapsed.TotalMilliseconds;
            if (tickCounter % (waitTicks / 2) == 0) //every <150> / 2 = 75 Ticks => 75 * <200> = 15s
            {
                Log.Logger.Debug($"ActionController: Refresh Tick #{tickCounter}: average Refresh-Time over the last {waitTicks / 2} Ticks: {averageTime / (waitTicks / 2):F3}ms. Registered Values: {ipcManager.Length}. Registered Actions: {currentActions.Count}.");
                averageTime = 0;
            }

            if (GlobalProfileSettings.EnableSwitching && !string.IsNullOrEmpty(SimConnector.AicraftString) && lastAircraft != SimConnector.AicraftString && SimConnector.IsReady && waitCounter == 0)
            {
                Log.Logger.Information($"ActionController: AircraftString changed to '{SimConnector.AicraftString}' -> Switch Profiles");
                SwitchProfiles();
            }
        }

        protected void RefreshActions(CancellationToken token, bool forceUpdate = false)
        {
            foreach (var action in currentActions.Values)
            {
                if (token.IsCancellationRequested)
                    return;
                
                if (forceUpdate)
                    action.ForceUpdate = forceUpdate;
                
                if (action.IsInitialized || redrawAlways)
                    action.Refresh(imgManager);
            }
        }

        protected void CallOnAll(Action<IHandler> method)
        {
            foreach (var action in currentActions.Values)
                method(action);
        }

        protected void RedrawAll(CancellationToken token)
        {
            try
            {
                foreach (var action in currentActions)
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (action.Value.NeedRedraw || action.Value.ForceUpdate || redrawAlways || redrawRequested)
                    {
                        //Log.Logger.Verbose($"RedrawAll: Needs Redraw [{action.Value.ActionID}] [{action.Key}] ({action.Value.NeedRedraw}, {action.Value.ForceUpdate}): {(!action.Value.IsRawImage ? action.Value.DrawImage : "raw")}");
                        if (action.Value.IsRawImage)
                            _ = DeckManager.SetImageRawAsync(action.Key, action.Value.DrawImage);
                        else 
                            _ = DeckManager.SetImageRawAsync(action.Key, imgManager.GetImageBase64(action.Value.DrawImage, action.Value.DeckType));
                    }

                    action.Value.ResetDrawState();
                }

                redrawRequested = false;
            }
            catch (Exception ex)
            {
                Log.Logger.Debug($"RedrawAll: Exception {ex.Message}");
            }
        }

        public bool OnButtonDown(string context)
        {
            try
            {
                if (!SimConnector.IsConnected)
                {
                    Log.Logger.Error($"RunAction: IPC not connected {context}");
                    return false;
                }

                if (currentActions.ContainsKey(context))
                {
                    return currentActions[context].OnButtonDown(tickCounter);
                }
                else
                {
                    Log.Logger.Error($"RunAction: Could not find Context {context}");
                    return false;
                }
            }
            catch
            {
                Log.Logger.Error($"RunAction: Exception while running {context} | {currentActions[context]?.ActionID}");
                return false;
            }
        }

        public bool OnButtonUp(string context)
        {
            try
            {
                if (!SimConnector.IsConnected)
                {
                    Log.Logger.Error($"RunAction: IPC not connected {context}");
                    return false;
                }

                if (currentActions.ContainsKey(context))
                {
                    return currentActions[context].OnButtonUp(ipcManager, tickCounter);
                }
                else
                {
                    Log.Logger.Error($"RunAction: Could not find Context {context}");
                    return false;
                }
            }
            catch
            {
                Log.Logger.Error($"RunAction: Exception while running {context} | {currentActions[context]?.ActionID}");
                return false;
            }
        }

        public void SetTitleParameters(string context, string title, StreamDeckEventPayload.TitleParameters titleParameters)
        {
            try
            {
                if (currentActions.ContainsKey(context))
                {
                    currentActions[context].SetTitleParameters(title, StreamDeckTools.ConvertTitleParameter(titleParameters));
                }
                else
                {
                    Log.Logger.Error($"SetTitleParameters: Could not find Context {context}");
                }
            }
            catch
            {
                Log.Logger.Error($"SetTitleParameters: Exception while updating {context} | {currentActions[context]?.ActionID}");
            }
        }

        protected void SetActionState(IHandler handler)
        {
            if (!SimConnector.IsRunning || !handler.IsInitialized)
                handler.SetDefault();
            else if (SimConnector.IsRunning && !SimConnector.IsConnected)
                handler.SetError();
            else if (SimConnector.IsRunning && (!SimConnector.IsReady || SimConnector.IsPaused))
                handler.SetWait();
            else
            {
                handler.ForceUpdate = true;
            }

            handler.NeedRedraw = true;
        }

        public void UpdateAction(string context)
        {
            try
            {
                if (currentActions.ContainsKey(context))
                {
                    currentActions[context].Update(imgManager);
                    SetActionState(currentActions[context]);                        

                    if (!currentActions[context].IsRawImage)
                        imgManager.UpdateImage(currentActions[context].DrawImage, currentActions[context].DeckType);

                    redrawRequested = true;
                }
                else
                {
                    Log.Logger.Error($"UpdateAction: Could not find Context {context}");
                }
            }
            catch
            {
                Log.Logger.Error($"UpdateAction: Exception while updating {context} | {currentActions[context]?.ActionID}");
            }
        }

        public void RegisterAction(string context, IHandler handler)
        {
            try
            {
                if (!currentActions.ContainsKey(context))
                {
                    currentActions.Add(context, handler);
                    handler.Register(imgManager, ipcManager);
                    SetActionState(handler);

                    redrawRequested = true;
                }
                else
                {
                    Log.Logger.Error($"RegisterAction: Context already registered! {context} | {currentActions[context].ActionID}");
                }
            }
            catch
            {
                Log.Logger.Error($"RegisterAction: Exception while registering {context} | {handler?.ActionID}");
            }
        }

        public void DeregisterAction(string context)
        {
            try
            { 
                if (currentActions.ContainsKey(context))
                {
                    currentActions[context].Deregister(imgManager);

                    currentActions.Remove(context);
                }
                else
                {
                    Log.Logger.Error($"DeregisterAction: Could not find Context {context}");
                }                   
            }
            catch
            {
                Log.Logger.Error($"DeregisterAction: Exception while deregistering {context}");
            }
        }

    }
}
