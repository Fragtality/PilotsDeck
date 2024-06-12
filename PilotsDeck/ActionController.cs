using Newtonsoft.Json;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PilotsDeck
{
    public enum ControllerState
    {
        Idle,
        Ready,
        Wait,
        Error
    }

    public class ActionController : IActionController
    {
        private Dictionary<string, IHandler> currentActions = null;
        public IPCManager ipcManager { get; protected set; } = null;
        public ImageManager imgManager { get; protected set; } = null;

        public ConnectionManager DeckManager { get; set; }
        public int Timing { get { return AppSettings.pollInterval; } }
        public SimulatorConnector SimConnector { get; set; }

        public long Ticks { get { return tickCounter; } }
        private long tickCounter = 0;
        private int waitCounter = 0;

        private ControllerState currentState = ControllerState.Idle;
        private ControllerState lastState = ControllerState.Idle;
        private bool wasPaused = false;
        private bool isClosing = false;
        private readonly int waitTicks = AppSettings.waitTicks;
        private readonly int firstTick = (int)(AppSettings.waitTicks / 7.5);
        private Stopwatch watchRefresh = new();
        private double averageTime = 0;
        private int imageUpdates = 0;
        private bool redrawAlways = AppSettings.redrawAlways;
        private bool devicesExported = false;

        public HandlerProfileSwitcher ProfileSwitcher { get; protected set; }
        private string lastAircraft = "";


        public ActionController()
        {
            currentActions = [];
            ipcManager = new();
            SimConnector = SimulatorConnector.CreateConnector("", tickCounter, ipcManager);
            imgManager = new();
            ProfileSwitcher = new();
        }

        public void Init()
        {
            if (currentActions != null && ipcManager != null && imgManager != null)
            {
                SimConnector = new ConnectorDummy();
                Logger.Log(LogLevel.Information, "ActionController:Init", "Configuration Parameters:");
                foreach (string key in ConfigurationManager.AppSettings.AllKeys)
                    Logger.Log(LogLevel.Information, "ActionController:Init", $"{key} = {ConfigurationManager.AppSettings[key]}");
            }

            ProfileSwitcher.LoadProfileMappings();
            Logger.Log(LogLevel.Information, "ActionController:Init", $"Loaded {ProfileSwitcher.Count} Profile Mappings from '{ProfileMapping.PLUGIN_MAPPING_FILE}'.");

            AppSettings.SetLocale();
            Logger.Log(LogLevel.Information, "ActionController:Init", $"Locale is set to '{AppSettings.locale}\'. (Default: {AppSettings.fontDefault}) (Bold: {AppSettings.fontBold}) (Italic: {AppSettings.fontItalic})");
            Logger.Log(LogLevel.Information, "ActionController:Init", $"ActionController successfully initialized!");
        }

        public void Dispose()
        {
            ipcManager.Dispose();
            imgManager.Dispose();
            GC.SuppressFinalize(this);
            Logger.Log(LogLevel.Information, "ActionController:Dispose", "ActionController and IPCManager Disposed");
        }

        public int Length => currentActions.Count;

        public IHandler this[string context]
        {
            get
            {
                if (currentActions.TryGetValue(context, out IHandler value))
                    return value;
                else
                    return null;
            }
        }

        public StreamDeckType GetDeckTypeById(string device, string controller)
        {
            StreamDeckType deckObj = new();
            
            var deckInfo = DeckManager.Info.devices.Where(d => d.id == device);
            if (deckInfo.Any())
            {
                deckObj.Type = (StreamDeckTypeEnum)deckInfo.First().type;
            }

            deckObj.IsEncoder = controller == "Encoder";

            return deckObj;
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
                case "didReceiveDeepLink":
                    OnDidReceiveDeepLink(args);
                    break;
                default:
                    break;
            }
        }

        protected void OnDidReceiveDeepLink(StreamDeckEventPayload args)
        {
            Logger.Log(LogLevel.Debug, "ActionController:OnDidReceiveDeepLink", $"url: {args?.payload?.url}");
            if (!string.IsNullOrWhiteSpace(args?.payload?.url))
            {
                string url = args?.payload?.url;
                url = url.Replace("/", "");
                string[] parts = url.Split('=');
                if (parts.Length == 2)
                {
                    parts[0] = $"X:{parts[0]}";
                    if (IPCTools.rxInternal.IsMatch(parts[0]) && ipcManager.Contains(parts[0]))
                    {
                        Logger.Log(LogLevel.Debug, "ActionController:OnDidReceiveDeepLink", $"Setting internal Variable '{parts[0]}' with Value '{parts[1]}'");
                        ipcManager[parts[0]].SetValue(parts[1]);
                    }
                }
            }
        }

        protected void OnDidReceiveGlobalSettings(StreamDeckEventPayload args)
        {
            ProfileSwitcher.OnDidReceiveGlobalSettings(args);
        }

        protected void OnApplicationDidLaunch(StreamDeckEventPayload args)
        {
            Logger.Log(LogLevel.Information, "ActionController:OnApplicationDidLaunch", $"Exectuable '{args.payload.application}' launched.");

            SimConnector.Dispose();
            SimConnector = SimulatorConnector.CreateConnector(args.payload.application, tickCounter, ipcManager);
        }

        protected void OnApplicationDidTerminate(StreamDeckEventPayload args)
        {
            Logger.Log(LogLevel.Information, "ActionController:OnApplicationDidTerminate", $"Exectuable '{args.payload.application}' terminated.");

            SimConnector.Close();
        }

        public void Run(CancellationToken token)
        {
            watchRefresh.Restart();
            tickCounter++;
            SimConnector.TickCounter = tickCounter;

            if (tickCounter == 15)
            {
                Logger.Log(LogLevel.Debug, "ActionController:Run", $"Requesting Global Settings");
                _ = DeckManager.GetGlobalSettingsAsync(DeckManager.PluginUUID);
            }

            if (tickCounter < firstTick) //wait till streamdeck<>plugin init is done ( <150> / 7.5 = 20 Ticks => 20 * <200> = 4s )
                return;

            if (waitCounter > 0)
            {
                waitCounter--;
                if (waitCounter == 0)
                {
                    currentState = ControllerState.Ready;
                    Logger.Log(LogLevel.Information, "ActionController:Run", $"Wait ended (Delay Ended).");
                }
                else if (waitCounter % 25 == 0)
                {
                    ipcManager.Process();
                    if (!SimConnector.IsReady)
                        Logger.Log(LogLevel.Debug, "ActionController:Run", $"Waiting ...");
                    else
                    {
                        waitCounter = 0;
                        currentState = ControllerState.Ready;
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Wait ended (Sim Ready).");
                    }
                }
            }          

            if (!SimConnector.IsRunning) //SIM not running
            {
                if (SimConnector.LastStateApp()) //SIM changed to not running
                {
                    ipcManager.UnloadScripts();
                    SimConnector.Close();
                    currentState = ControllerState.Error;
                    isClosing = true;

                    waitCounter = (5000 / AppSettings.pollInterval);
                    Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim changed to NOT running, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                }
                else if (isClosing && waitCounter == 0)
                {
                    currentState = ControllerState.Idle;
                    isClosing = false;

                    lastAircraft = "";
                    wasPaused = false;
                    if (ProfileSwitcher.GlobalProfileSettings.EnableSwitching)
                        ProfileSwitcher.SwitchToDefaultProfile();
                }
                else if (!isClosing && waitCounter == 0)
                    currentState = ControllerState.Idle;
            }
            else //SIM running
            {
                if (!SimConnector.LastStateApp()) //SIM changed to running
                {
                    currentState = ControllerState.Wait;
                    isClosing = false;

                    if (tickCounter > firstTick)
                    {
                        waitCounter = (30000 / AppSettings.pollInterval);
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim changed to running, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim changed to running, directly connect.");
                        SimConnector.Connect();
                    }
                }

                if (!SimConnector.IsConnected && waitCounter == 0) //NOT connected
                {
                    if (SimConnector.LastStateConnect())
                    {
                        currentState = ControllerState.Error;
                        waitCounter = waitTicks / 2;
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim changed to disconnected, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                    }

                    if (!SimConnector.Connect())
                    {
                        waitCounter = waitTicks;
                        currentState = ControllerState.Wait;
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim Connection failed, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim is connected.");
                    }
                }
                else if (SimConnector.IsConnected) //CONNECTED
                {
                    if (!SimConnector.LastStateConnect()) //changed to connected
                    {
                        currentState = ControllerState.Wait;
                        ipcManager.Process();
                        if (tickCounter > firstTick && !SimConnector.IsReady)
                        {
                            waitCounter = waitTicks / 2;
                            Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim changed to connected, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                        }
                    }

                    if (waitCounter == 0 && SimConnector.IsReady) //process possible
                    {
                        //if (tickCounter % 5 == 0)
                        //    Log.Logger.Debug("Process()");
                        if (ipcManager.Process())
                        {
                            if (!SimConnector.LastStateProcess())
                            {
                                currentState = ControllerState.Ready;
                                CallOnAll(handler => handler.NeedRefresh = true);
                                Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim Process successful again.");
                            }

                            if (wasPaused && !SimConnector.IsPaused)
                            {
                                currentState = ControllerState.Ready;
                                CallOnAll(handler => handler.NeedRefresh = true);
                                wasPaused = false;
                                Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim is unpaused.");
                            }
                            else if (!wasPaused && SimConnector.IsPaused)
                            {
                                currentState = ControllerState.Wait;
                                wasPaused = true;
                                Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim is paused.");
                            }
                        }
                        else
                        {
                            currentState = ControllerState.Error;
                            waitCounter = waitTicks / 2;
                            Logger.Log(LogLevel.Warning, "ActionController:Run", $"Sim Process failed, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                        }
                    }
                    else if (!SimConnector.IsReady && waitCounter == 0)
                    {
                        waitCounter = waitTicks / 6;
                        SimConnector.Process();
                        currentState = ControllerState.Wait;
                        wasPaused = true;
                        Logger.Log(LogLevel.Information, "ActionController:Run", $"Sim not ready, waiting for {(waitCounter * AppSettings.pollInterval) / 1000}s.");
                    }
                }
            }


            if (ProfileSwitcher.GlobalProfileSettings.EnableSwitching && ProfileSwitcher.CanSwitch)
            {
                Logger.Log(LogLevel.Information, "ActionController:Run", $"AircraftString changed to '{SimConnector.AicraftPathString}', searching for matching Profiles ...");
                ProfileSwitcher.SwitchProfiles();
                lastAircraft = SimConnector.AicraftPathString;
            }
            else if (SimConnector.IsRunning)
            {
                RefreshActions(token);

                if (lastAircraft != SimConnector.AicraftPathString)
                {
                    lastAircraft = SimConnector.AicraftPathString;
                    Logger.Log(LogLevel.Information, "ActionController:Run", $"AircraftString changed to '{SimConnector.AicraftPathString}'");
                }
            }
            
            UpdateImages(token);


            watchRefresh.Stop();
            averageTime += watchRefresh.Elapsed.TotalMilliseconds;
            int time = AppSettings.refreshInterval;
            if (tickCounter % (time / AppSettings.pollInterval) == 0)
            {
                int changedScripts = ipcManager.ScriptManager.CheckFiles();
                int removedAddresses = ipcManager.UnsubscribeUnusedAddresses();
                int removedImages = imgManager.RemoveUnused();
                if (SimConnector.IsRunning || imageUpdates > 0 || changedScripts > 0 || removedAddresses > 0 || removedImages > 0)
                    Logger.Log(LogLevel.Debug, "ActionController:Run", $"Refresh Tick #{tickCounter}: average Refresh-Time over the last {time/1000}s: {averageTime / (time / AppSettings.pollInterval):F3}ms. (Actions: {currentActions.Count}) (IPCValues: {ipcManager.Length}) (Scripts: {ipcManager.ScriptManager.Count} d / {ipcManager.ScriptManager.CountGlobal} g / {ipcManager.ScriptManager.CountImages} i) (Images: {imgManager.Length}) (Updates: {imageUpdates})");
                averageTime = 0;
                imageUpdates = 0;
            }

            if (!devicesExported)
            {
                ExportDeviceInfo();
                devicesExported = true;
            }
        }

        protected void ExportDeviceInfo()
        {
            try
            {
                string strJson = JsonConvert.SerializeObject(DeckManager.Info.devices, Formatting.Indented);
                File.WriteAllText("Profiles/DeviceInfo.json", strJson);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:ExportDeviceInfo", $"Exception '{ex.GetType}' while exporting Device Info: {ex.Message}");
            }
        }

        protected void RefreshActions(CancellationToken token)
        {
            foreach (var action in currentActions.Values)
            {
                if (token.IsCancellationRequested)
                    return;
                action.Refresh();
            }
        }

        protected void CallOnAll(Action<IHandler> method)
        {
            foreach (var action in currentActions.Values)
                method(action);
        }

        protected void UpdateImages(CancellationToken token)
        {
            try
            {
                if (lastState != currentState)
                    Logger.Log(LogLevel.Debug, "ActionController:UpdateImages", $"Sim State changed: {lastState} -> {currentState}");

                foreach (var action in currentActions)
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (action.Value.NeedRedraw || lastState != currentState || redrawAlways)
                    {
                        imageUpdates++;
                        //Logger.Log(LogLevel.Debug, "ActionController:UpdateImages", $"--REDRAW-- Action [{action.Value.ActionID}] | Needs Redraw, Refresh ({action.Value.NeedRedraw}, {action.Value.NeedRefresh})");
                        if (action.Value.DeckType.IsEncoder)
                        {
                            _ = DeckManager.SetFeedbackItemImageRawAsync(AppSettings.targetImage, action.Key, ActionSendImage(action.Value));
                            if (action.Value.FirstLoad)
                            {
                                _ = DeckManager.SetImageAsync(action.Key, "Images/category/Encoder.png");
                                action.Value.FirstLoad = false;
                            }
                        }
                        else
                            _ = DeckManager.SetImageRawAsync(action.Key, ActionSendImage(action.Value));

                        if (lastState != currentState && currentState == ControllerState.Ready)
                            CallOnAll(handler => handler.NeedRefresh = true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:UpdateImages", $"Exception while Redrawing Actions! (Exception: {ex.GetType()})");
            }

            CallOnAll(handler => handler.ResetDrawState());
            lastState = currentState;
        }

        public string ActionSendImage(IHandler action)
        {
            if (currentState == ControllerState.Idle)
                return action.DefaultImage64;
            if (currentState == ControllerState.Ready)
                return action.RenderImage64;
            if (currentState == ControllerState.Wait)
                return action.WaitImage64;

            return action.ErrorImage64;
        }

        public bool OnButtonDown(string context)
        {
            try
            {
                if (!SimConnector.IsConnected)
                {
                    Logger.Log(LogLevel.Warning, "ActionController:OnButtonDown", $"Sim not connected! (Context: {context}) (ActionID: {currentActions[context]?.ActionID})");
                    return false;
                }

                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    return value.OnButtonDown(tickCounter);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:OnButtonDown", $"Could not find Context '{context}'.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:OnButtonDown", $"Exception on ButtonDown! (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
                return false;
            }
        }

        public bool OnButtonUp(string context)
        {
            try
            {
                if (!SimConnector.IsConnected)
                {
                    Logger.Log(LogLevel.Warning, "ActionController:OnButtonUp", $"Sim not connected! (Context: {context}) (ActionID: {currentActions[context]?.ActionID})");
                    return false;
                }

                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    return value.OnButtonUp(tickCounter);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:OnButtonUp", $"Could not find Context '{context}'.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:OnButtonUp", $"Exception on ButtonUp! (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
                return false;
            }
        }

        public bool OnDialRotate(string context, int ticks)
        {
            try
            {
                if (!SimConnector.IsConnected)
                {
                    Logger.Log(LogLevel.Warning, "ActionController:OnDialRotate", $"Sim not connected! (Context: {context}) (ActionID: {currentActions[context]?.ActionID})");
                    return false;
                }

                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    return value.OnDialRotate(ticks);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:OnDialRotate", $"Could not find Context '{context}'.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:OnDialRotate", $"Exception on DialRotate! (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
                return false;
            }
        }

        public bool OnTouchTap(string context)
        {
            try
            {
                if (!SimConnector.IsConnected)
                {
                    Logger.Log(LogLevel.Warning, "ActionController:OnTouchTap", $"Sim not connected! (Context: {context}) (ActionID: {currentActions[context]?.ActionID})");
                    return false;
                }

                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    return value.OnTouchTap();
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:OnTouchTap", $"Could not find Context '{context}'.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:OnTouchTap", $"Exception on TouchTap! (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
                return false;
            }
        }

        public void SetTitleParameters(string context, string title, StreamDeckEventPayload.TitleParameters titleParameters)
        {
            try
            {
                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    value.SetTitleParameters(title, StreamDeckTools.ConvertTitleParameter(titleParameters));
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:SetTitleParameters", $"Could not find Context '{context}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:SetTitleParameters", $"Exception while settint Title Params! (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
            }
        }

        public void UpdateAction(string context)
        {
            try
            {
                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    value.Update();
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:UpdateAction", $"Could not find Context '{context}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:UpdateAction", $"Exception while updating Action! (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
            }
        }

        public void RegisterAction(string context, IHandler handler)
        {
            try
            {
                if (currentActions.TryAdd(context, handler))
                {
                    handler.Register(imgManager, ipcManager);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:RegisterAction", $"Context already registered! (Context: {context}) (ActionID: {currentActions[context].ActionID})");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:RegisterAction", $"Exception while registering Action! (Context: {context}) (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
            }
        }

        public void DeregisterAction(string context)
        {
            try
            { 
                if (currentActions.TryGetValue(context, out IHandler value))
                {
                    value.Deregister();

                    currentActions.Remove(context);
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ActionController:DeregisterAction", $"Could not find Context '{context}'.");
                }                   
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ActionController:DeregisterAction", $"Exception while deregistering Action! (Context: {context}) (ActionID: {currentActions[context]?.ActionID}) (Exception: {ex.GetType()})");
            }
        }

    }
}
