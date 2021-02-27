﻿using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using StreamDeckLib;
using StreamDeckLib.Messages;
using Serilog;
using System.Diagnostics;

namespace PilotsDeck
{
       
    public class ActionController : IActionController
    {
        private Dictionary<string, IHandler> currentActions = null;
        private IPCManager ipcManager = null;
        private ImageManager imgManager = null;

        public ConnectionManager DeckManager { get; set; }
        public int Timing { get { return AppSettings.waitTicks; } }
        public bool IsApplicationOpen { get; set; }
        public string Application { get; } = AppSettings.applicationName;

        public long Ticks { get { return tickCounter; } }
        private long tickCounter = 0;
        private bool lastAppState = false;
        private bool appIsStarting = false;
        private bool appAlreadyRunning = false;
        private bool lastConnectState = false;
        private bool lastProcessState = false;
        private bool redrawRequested = false;
        private readonly int waitTicks = AppSettings.waitTicks;
        private readonly int firstTick = (int)(AppSettings.waitTicks / 7.5);
        private Stopwatch watchRefresh = new Stopwatch();
        private Stopwatch watchLoading = new Stopwatch();
        private double averageTime = 0;
        private bool redrawAlways = AppSettings.redrawAlways;

        public ModelProfileSwitcher GlobalProfileSettings { get; protected set; } = new ModelProfileSwitcher();
        private List<string> manifestProfiles;
        private IPCValueOffset loadedFSProfile;
        private string lastFSProfile = null;
        private IPCValueOffset loadedAircraft;
        private string lastAircraft = "none";

        public ActionController()
        {
            currentActions = new Dictionary<string, IHandler>();
            ipcManager = new IPCManager(AppSettings.groupStringRead);
            imgManager = new ImageManager();
            manifestProfiles = new List<string>();
        }

        public void Init()
        {
            if (currentActions != null && ipcManager != null && imgManager != null && manifestProfiles != null)
            {
                loadedFSProfile = ipcManager.RegisterAddress("9540:64:s", AppSettings.groupStringRead, true) as IPCValueOffset;
                loadedAircraft = ipcManager.RegisterAddress("3500:24:s", AppSettings.groupStringRead, true) as IPCValueOffset;
                Log.Logger.Information($"ActionController successfully initialized. Poll-Time {AppSettings.pollInterval}ms / Wait-Ticks {waitTicks} / Redraw Always {redrawAlways}");
            }

            dynamic manifest = JObject.Parse(File.ReadAllText(@"manifest.json"));
            if (manifest?.Profiles != null)
            {
                foreach (var profile in manifest.Profiles)
                {
                    string name = profile?.Name;
                    if (!string.IsNullOrEmpty(name))
                        manifestProfiles.Add(name);
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
            GlobalProfileSettings.UpdateSettings(manifestProfiles);
            DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);

            //For Test/Debugging - clear GlobalSettings
            //GlobalProfileSettings = new ModelProfileSwitcher();
            //GlobalProfileSettings.ExportToJson();
            //DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);

            Log.Logger.Information($"ActionController:OnDidReceiveGlobalSettings - Switching => {GlobalProfileSettings.EnableSwitching} | Default: {GlobalProfileSettings.UseDefault} | Profiles => {GlobalProfileSettings?.ProfileMappings?.Count}");
        }

        protected void OnApplicationDidLaunch(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionController:OnApplicationDidLaunchAsync {args.payload.application}");

            if (args.payload.application == Application)
            {
                IsApplicationOpen = true;
                appAlreadyRunning = tickCounter < firstTick;
                appIsStarting = true;
            }
        }

        protected void OnApplicationDidTerminate(StreamDeckEventPayload args)
        {
            Log.Logger.Debug($"ActionController:OnApplicationDidTerminateAsync {args.payload.application}");

            if (args.payload.application == Application)
                IsApplicationOpen = false;
        }

        public void Run(CancellationToken token)
        {
            watchRefresh.Restart();
            tickCounter++;

            if (tickCounter == 1)
                _ = DeckManager.GetGlobalSettingsAsync(DeckManager.PluginUUID);

            if (tickCounter < firstTick) //wait till streamdeck<>plugin init is done ( <150> / 7.5 = 20 Ticks => 20 * <200> = 4s )
                return;

            if (!IsApplicationOpen)     //P3D closed          ????or the first tick || tickCounter == firstTick
            {
                if (lastAppState)       //P3D changed to closed
                {
                    lastAppState = false;
                    appAlreadyRunning = false;
                    appIsStarting = false;
                    lastAircraft = "none";
                    lastFSProfile = null;
                    ipcManager.Close();
                    CallOnAll(handler => handler.SetDefault());
                    redrawRequested = true;
                    if (GlobalProfileSettings.EnableSwitching && GlobalProfileSettings.ProfilesInstalled)
                        _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, DeckManager.FirstDeviceID, null);
                }
            }
            else                        //P3D open
            {
                if (!lastAppState)      //P3D changed to opened
                {
                    lastAppState = true;
                    lastConnectState = false;
                    lastProcessState = false;
                    CallOnAll(handler => handler.SetWait());
                    redrawRequested = true;
                    ipcManager.Connect();
                }
                
                if (!ipcManager.IsConnected && (tickCounter % waitTicks == 0 || tickCounter == firstTick)) //still open not connected, check retry connection every 30s when not connected (every <150> Ticks * <200>ms)
                {
                    ipcManager.Connect();
                }
                else if (ipcManager.IsConnected)            //open and connected
                {
                    if (!lastConnectState)                  //connection changed to opened
                    {
                        lastConnectState = true;
                        CallOnAll(handler => handler.SetWait());
                    }

                    if (!lastProcessState && tickCounter % (waitTicks / 3) != 0 && tickCounter != firstTick && !appAlreadyRunning)  //throttle process calls to every 10s (150/3 * 200ms) if last was unsuccessful (but not on first)
                    {
                        lastProcessState = false;
                        //Log.Logger.Verbose("PROC - Throttle (not proc)");
                    }
                    else if (!appAlreadyRunning && appIsStarting && tickCounter % (waitTicks / 3) != 0) //throttle calls while still loading (60s timer still running) to every 10s
                    {
                        //Log.Logger.Verbose($"PROC - Throttle (starting) - Delay Elapsed: {watchLoading.Elapsed.Seconds} | Total: {watchLoading.Elapsed.TotalSeconds} | in ms {watchLoading.ElapsedMilliseconds}");
                    }
                    else if (ipcManager.Process(AppSettings.groupStringRead))
                    {
                        if (!appAlreadyRunning && appIsStarting)
                        {
                            if (!watchLoading.IsRunning)
                            {
                                watchLoading.Restart();
                                //Log.Logger.Verbose("PROC - Processed OK - Start App Delay");
                            }
                            else
                                Log.Logger.Debug($"ActionController: Throttled Processing, awaiting appStartDelay - elapsed: {watchLoading.Elapsed.TotalSeconds:n0}");

                            if (watchLoading.Elapsed.TotalSeconds > AppSettings.appStartDelay)
                            {
                                watchLoading.Stop();
                                watchLoading.Reset();
                                appIsStarting = false;
                                Log.Logger.Debug($"ActionController: appStartDelay expired, Processing normally.");
                                lastProcessState = false;
                                //Log.Logger.Verbose("PROC - Processed OK - Stop App Delay");
                            }
                        }
                        else if (appAlreadyRunning && appIsStarting)
                            appIsStarting = false;
                        //else
                        //    Log.Logger.Verbose("PROC - Processed OK");

                        RefreshActions(token, !lastProcessState);   //toggles process change
                        lastProcessState = true;
                        if (GlobalProfileSettings.EnableSwitching && !string.IsNullOrEmpty(loadedAircraft.Value) && lastAircraft != loadedAircraft.Value)
                            SwitchProfiles();
                    }
                    else
                    {
                        //Log.Logger.Verbose("PROC - Processed ERR");
                        lastProcessState = false;
                    }

                    redrawRequested = true;
                }
                else if (!ipcManager.IsConnected)       //open and disconnected
                {
                    if (lastConnectState)               //changed to disconnected
                    {
                        lastConnectState = false;
                        lastProcessState = false;
                        ipcManager.Close();
                        CallOnAll(handler => handler.SetError());
                        redrawRequested = true;
                    }
                }
            }

            if (redrawRequested)
                RedrawAll(token);

            if (redrawAlways)
            {
                RefreshActions(token, true);
                RedrawAll(token);
            }

            watchRefresh.Stop();
            if (IsApplicationOpen)
            {
                averageTime += watchRefresh.Elapsed.TotalMilliseconds;
                if (tickCounter % (waitTicks / 2) == 0) //every <150> / 2 = 75 Ticks => 75 * <200> = 15s
                {
                    Log.Logger.Debug($"ActionController: Refresh Tick #{tickCounter}: average Refresh-Time over the last {waitTicks / 2} Ticks: {averageTime / (waitTicks / 2):F3}ms. Registered Values: {ipcManager.Length}. Registered Actions: {currentActions.Count}");
                    averageTime = 0;
                }

                if (appAlreadyRunning && tickCounter > waitTicks)
                    appAlreadyRunning = false;
            }
        }

        protected void SwitchProfiles()
        {
            string switchTo = "";

            if (string.IsNullOrEmpty(loadedFSProfile.Value) && GlobalProfileSettings.UseDefault && !string.IsNullOrEmpty(GlobalProfileSettings.DefaultProfile))
                switchTo = GlobalProfileSettings.DefaultProfile;
            else if (GlobalProfileSettings?.ProfileMappings != null)
            {
                foreach (var profile in GlobalProfileSettings.ProfileMappings)
                {
                    if (ModelProfileSwitcher.IsInProfile(profile.Mappings, loadedFSProfile.Value))
                    {
                        switchTo = profile.Name;
                        break;
                    }    
                }

                if (switchTo == "" && GlobalProfileSettings.UseDefault && !string.IsNullOrEmpty(GlobalProfileSettings.DefaultProfile))
                    switchTo = GlobalProfileSettings.DefaultProfile;
            }

            if (switchTo != "")
            {
                _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, DeckManager.FirstDeviceID, switchTo);
                Log.Logger.Information($"ActionController: FSUIPC Profile [{loadedFSProfile.Value}] active for Aircraft [{loadedAircraft.Value}]-> switching to [{switchTo}]");
            }
            
            lastFSProfile = loadedFSProfile.Value;
            lastAircraft = loadedAircraft.Value;
        }

        public void LoadProfiles()
        {
            if (!GlobalProfileSettings.ProfilesInstalled)
            {
                _ = DeckManager.SwitchToProfileAsync(DeckManager.PluginUUID, DeckManager.FirstDeviceID, AppSettings.deckDefaultProfile);
            }
            GlobalProfileSettings.ProfilesInstalled = true;
            DeckManager.SetGlobalSettingsAsync(DeckManager.PluginUUID, GlobalProfileSettings);
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
                    action.Refresh(imgManager, ipcManager);
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

                    if (action.Value.NeedRedraw || action.Value.ForceUpdate)
                    {
                        //Log.Logger.Verbose($"RedrawAll: Needs Redraw [{action.Value.ActionID}] [{action.Key}] ({action.Value.NeedRedraw}, {action.Value.ForceUpdate}): {(!action.Value.IsRawImage ? action.Value.DrawImage : "raw")}");
                        if (action.Value.IsRawImage)
                            _ = DeckManager.SetImageRawAsync(action.Key, action.Value.DrawImage);
                        else 
                            _ = DeckManager.SetImageRawAsync(action.Key, imgManager.GetImageBase64(action.Value.DrawImage));
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
                if (!ipcManager.IsConnected)
                {
                    Log.Logger.Error($"RunAction: IPC not connected {context}");
                    return false;
                }

                if (currentActions.ContainsKey(context))
                {
                    return (currentActions[context] as IHandlerSwitch).OnButtonDown(ipcManager, tickCounter);
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
                if (!ipcManager.IsConnected)
                {
                    Log.Logger.Error($"RunAction: IPC not connected {context}");
                    return false;
                }

                if (currentActions.ContainsKey(context))
                {
                    return (currentActions[context] as IHandlerSwitch).OnButtonUp(ipcManager, tickCounter);
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

        //public bool RunAction(string context, bool longPress)
        //{
        //    try
        //    {
        //        if (!ipcManager.IsConnected)
        //        {
        //            Log.Logger.Error($"RunAction: IPC not connected {context}");
        //            return false;
        //        }

        //        if (currentActions.ContainsKey(context))
        //        {
        //            return (currentActions[context] as IHandlerSwitch).Action(ipcManager, longPress);
        //        }
        //        else
        //        {
        //            Log.Logger.Error($"RunAction: Could not find Context {context}");
        //            return false;
        //        }
        //    }
        //    catch
        //    {
        //        Log.Logger.Error($"RunAction: Exception while running {context} | {currentActions[context]?.ActionID}");
        //        return false;
        //    }
        //}

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
            if (!IsApplicationOpen || !handler.IsInitialized)
                handler.SetDefault();
            else if (!ipcManager.IsConnected)
                handler.SetError();
            else if (!lastProcessState)
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
                    currentActions[context].Update(imgManager, ipcManager);
                    SetActionState(currentActions[context]);                        

                    if (!currentActions[context].IsRawImage)
                        imgManager.UpdateImage(currentActions[context].DrawImage);

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
                    currentActions[context].Deregister(imgManager, ipcManager);

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
