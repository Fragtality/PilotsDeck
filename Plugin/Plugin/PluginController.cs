using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Actions;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck;
using PilotsDeck.StreamDeck.Messages;
using PilotsDeck.Tools;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PilotsDeck.Plugin
{
    public enum PluginState
    {
        IDLE = 0,
        WAIT = 1,
        READY = 2
    }

    public class PluginController
    {
        protected DispatcherTimer TimerRefresh { get; } = new();
        protected static DeckController DeckController { get { return App.DeckController; } }
        protected bool DevicesExport { get; set; } = false;
        protected static SimController SimController { get { return App.SimController; } }
        public ActionManager ActionManager { get; } = new();
        public VariableManager VariableManager { get; } = new();
        public ScriptManager ScriptManager { get; } = new();
        public ImageManager ImageManager { get; } = new();
        protected bool LastSimReadyCmd { get; set; } = false;
        protected SimulatorState LastSimState { get; set; } = SimulatorState.UNKNOWN;
        protected string LastAircraftString { get; set; } = "";
        protected DateTime LastRemovedUnused { get; set; } = DateTime.Now;
        protected DateTime LastCheckedScripts { get; set; } = DateTime.Now;
        protected DateTime ResetImageTime {  get; set; } = DateTime.Now;
        public PluginState State { get; protected set; } = PluginState.IDLE;
        protected bool FirstRun { get; set; } = true;
        protected bool ForcedRefresh { get; set; } = false;

        public PluginController()
        {
            JsonOptions.JsonSerializerOptions.Converters.Add(new AutoNumberToStringConverter());
            JsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async void Run()
        {
            StatisticManager.AddTracker(StatisticID.PLUGIN_REFRESH);
            StatisticManager.AddTracker(StatisticID.PLUGIN_RECEIVE);

            Logger.Information($"Starting Refresh Timer");
            TimerRefresh.Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalDeckRefresh);
            TimerRefresh.Tick += RefreshTask;
            TimerRefresh.Start();

            await ReceiveEvents();
            TimerRefresh?.Stop();
            Logger.Information("PluginController ended");
        }

        private async Task ReceiveEvents()
        {
            Logger.Information("ReceiveEvent Task started");
            while (!App.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    var sdEvent = await DeckController.ReceiveChannel.ReadAsync(App.CancellationToken);
                    StatisticManager.StartTrack(StatisticID.PLUGIN_RECEIVE);
                    SimCommand[] commands = null;

                    switch (sdEvent.Event)
                    {
                        case "didReceiveSettings":
                            ActionManager.SetSettingModel(sdEvent);
                            break;
                        case "didReceiveGlobalSettings":
                            ActionManager.ProfileSwitcherManager.OnDidReceiveGlobalSettings(sdEvent);
                            break;
                        case "didReceiveDeepLink":
                            OnDidReceiveDeepLink(sdEvent);
                            break;
                        case "touchTap":
                            commands = ActionManager.OnTouchTap(sdEvent);
                            break;
                        case "dialDown":
                            commands = ActionManager.OnDialDown(sdEvent);
                            break;
                        case "dialUp":
                            commands = ActionManager.OnDialUp(sdEvent);
                            break;
                        case "dialRotate":
                            commands = ActionManager.OnDialRotate(sdEvent);
                            break;
                        case "keyDown":
                            commands = ActionManager.OnKeyDown(sdEvent);
                            break;
                        case "keyUp":
                            commands = ActionManager.OnKeyUp(sdEvent);
                            break;
                        case "willAppear":
                            ActionManager.RegisterAction(sdEvent);
                            break;
                        case "willDisappear":
                            ActionManager.DeregisterAction(sdEvent);
                            break;                        
                        case "titleParametersDidChange":
                            ActionManager.SetTitleParameters(sdEvent);
                            break;
                        case "applicationDidLaunch":
                        case "applicationDidTerminate":
                        case "systemDidWakeUp":
                            Logger.Information($"The Plugin does not support the '{sdEvent.Event}' Event");
                            break;
                        case "propertyInspectorDidAppear":
                            ActionManager.PropertyInspectorDidAppear(sdEvent);
                            break;
                        case "propertyInspectorDidDisappear":
                            ActionManager.PropertyInspectorDidDisappear(sdEvent);
                            break;
                        case "sendToPlugin":
                            string msg = sdEvent.payload?.settings?.GetValue<string>();
                            if (msg == "propertyInspectorConnected")
                                ActionManager.SendPropertyInspectorModel(sdEvent);
                            else if (msg == "SettingsModelCopy" || msg == "SettingsModelPaste")
                                ActionManager.TransferSettingModel(sdEvent.context, msg);
                            else if (msg == "OpenDesigner")
                                ActionManager.OpenActionDesigner(sdEvent.context);
                            else
                                ActionManager.ProfileSwitcherManager.OnSendToPlugin(sdEvent);
                            break;
                        default:
                            Logger.Error($"Unkown Event '{sdEvent.Event}'!");
                            break;
                    }

                    if (commands != null)
                        foreach (var cmd in commands)
                            if (cmd != null)
                                _ = SimController.CommandChannel.WriteAsync(cmd).AsTask();
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                        Logger.LogException(ex);
                }
                StatisticManager.EndTrack(StatisticID.PLUGIN_RECEIVE);
            }
            Logger.Information($"Cancellation received - ReceiveEvent Task stopped");
        }

        protected void OnDidReceiveDeepLink(StreamDeckEvent args)
        {
            Logger.Debug($"url: {args?.payload?.url}");
            if (!string.IsNullOrWhiteSpace(args?.payload?.url))
            {
                string url = args?.payload?.url;
                url = url.Replace("/", "");
                string[] parts = url.Split('=');
                if (parts.Length == 2)
                {
                    parts[0] = $"X:{parts[0]}";
                    var address = new ManagedAddress(parts[0]);
                    if (TypeMatching.rxInternal.IsMatch(parts[0]) && VariableManager.Contains(address))
                    {
                        Logger.Debug($"Setting internal Variable '{address}' with Value '{parts[1]}'");
                        VariableManager[address].SetValue(parts[1]);
                    }
                }
            }
        }

        protected void RemoveUnusedResources(bool force = false)
        {
            int count = 0;            
            count += ScriptManager.RemoveUnused();
            SimController.RemoveUnusedResources(force);
            count += ImageManager.RemoveUnused();
            count += VariableManager.RemoveUnused();

            if (count > 0 && (State == PluginState.WAIT || State == PluginState.IDLE))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            LastRemovedUnused = DateTime.Now;
            StatisticManager.PrintRessourceStatistics();
        }

        private void RefreshTask(object sender, EventArgs e)
        {
            StatisticManager.StartTrack(StatisticID.PLUGIN_REFRESH);
            try
            {   
                if (App.CancellationTokenSource.IsCancellationRequested)
                {
                    Logger.Information($"Cancellation received - stopping Refresh Timer");
                    TimerRefresh.Stop();
                    return;
                }

                if (FirstRun)
                {
                    _ = DeckController.SendGetGlobalSettings();
                    ActionManager.ProfileSwitcherManager.LoadProfileMappings();
                    ScriptManager.CheckFiles();
                    FirstRun = false;
                }

                if (LastAircraftString != SimController.AircraftString)
                {
                    Logger.Debug($"Aircraft String changed to: {SimController.AircraftString}");
                    ActionManager.ProfileSwitcherManager.UpdateActivePropertyInspector();
                    LastAircraftString = SimController.AircraftString;
                }


                // State Switching
                if (LastSimState != SimController.SimState)
                {
                    Logger.Debug($"SimState changed to {SimController.SimState}");

                    if (SimController.SimState == SimulatorState.STOPPED)
                    {
                        Logger.Information("--- Sim changed to STOPPED ---");
                        if (State != PluginState.WAIT)
                            Logger.Information("--- Plugin changed to WAIT ---");
                        State = PluginState.WAIT;
                        ResetImageTime = DateTime.Now + TimeSpan.FromSeconds(5);
                        ScriptManager.StopGlobalScripts();
                        RemoveUnusedResources(true);
                    }

                    if (SimController.SimState == SimulatorState.RUNNING)
                    {
                        Logger.Information("--- Sim changed to RUNNING ---");
                        if (State != PluginState.WAIT)
                            Logger.Information("--- Plugin changed to WAIT ---");
                        State = PluginState.WAIT;
                    }

                    if (SimController.SimState == SimulatorState.LOADING)
                    {
                        Logger.Information("--- Sim changed to LOADING ---");
                        if (State != PluginState.WAIT)
                            Logger.Information("--- Plugin changed to WAIT ---");
                        State = PluginState.WAIT;
                        if (LastSimState == SimulatorState.SESSION)
                        {
                            ScriptManager.StopGlobalScripts();
                            RemoveUnusedResources();
                            ActionManager.ProfileSwitcherManager.ResetAircraft();
                        }
                    }

                    if (SimController.SimState == SimulatorState.SESSION)
                    {
                        Logger.Information("--- Sim changed to SESSION ---");
                        if (State != PluginState.READY)
                            Logger.Information("--- Plugin changed to READY ---");
                        State = PluginState.READY;

                        ActionManager.ProfileSwitcherManager.SwitchProfiles();
                        Task.Run(async () =>
                        {
                            await Task.Delay(500);
                            RemoveUnusedResources(true);
                            await Task.Delay(500);
                            ScriptManager.StartGlobalScripts();
                        });
                        ForcedRefresh = true;
                    }

                    LastSimState = SimController.SimState;
                }

                if (LastSimReadyCmd != SimController.IsReadyCommand)
                {
                    LastSimReadyCmd = SimController.IsReadyCommand;
                    if (LastSimReadyCmd)
                    {
                        Logger.Information("--- Sim READY for Commands ---");
                        ForcedRefresh = true;
                    }
                    else
                        Logger.Information("--- Sim NOT READY for Commands ---");
                }

                if (SimController.SimState <= SimulatorState.STOPPED && State != PluginState.IDLE && ResetImageTime <= DateTime.Now)
                {
                    Logger.Information("--- Plugin changed to IDLE ---");
                    State = PluginState.IDLE;
                    ActionManager.ProfileSwitcherManager.SwitchBack();
                    RemoveUnusedResources();
                    LastSimReadyCmd = false;
                    LastSimState = SimulatorState.UNKNOWN;
                    LastAircraftString = "";
                }


                //MAIN - Run, Refresh, Update
                if (SimController.SimState == SimulatorState.SESSION && State == PluginState.READY)
                    ScriptManager.RunGlobalScripts();

                if (ForcedRefresh)
                    VariableManager.ResetChangedState(true);
                else
                    VariableManager.CheckChanged();
                ActionManager.Refresh(ForcedRefresh);
                ActionManager.Redraw(State);


                //Ressource Checks
                if (DateTime.Now - LastRemovedUnused > TimeSpan.FromMilliseconds(App.Configuration.IntervalUnusedRessources))
                {
                    if (State != PluginState.READY)
                        RemoveUnusedResources();
                    else
                    {
                        LastRemovedUnused = DateTime.Now;
                        StatisticManager.PrintRessourceStatistics();
                    }
                }

                if (DateTime.Now - LastCheckedScripts > TimeSpan.FromMilliseconds(App.Configuration.IntervalCheckScripts))
                {
                    ScriptManager.CheckFiles();
                    LastCheckedScripts = DateTime.Now;
                }

                if (!DevicesExport && DeckController.ReceivedDevices > 0)
                {
                    ExportDeviceInfo();
                    DevicesExport = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            
            ForcedRefresh = false;
            StatisticManager.EndTrack(StatisticID.PLUGIN_REFRESH);
        }

        protected static void ExportDeviceInfo()
        {
            try
            {
                if (!Directory.Exists(AppConfiguration.DirProfilesName))
                    Directory.CreateDirectory(AppConfiguration.DirProfilesName);

                string strJson = JsonSerializer.Serialize(DeckController.DeckInfo.devices, new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText($"{AppConfiguration.DirProfilesName}/DeviceInfo.json", strJson);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
