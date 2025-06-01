using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.InputEvents;
using CFIT.SimConnectLib.Modules.MobiFlight;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.MSFS;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public class ConnectorMSFS : ISimConnector
    {
        public static VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        public SimulatorType Type { get; } = SimulatorType.MSFS;
        public SimulatorState SimState { get; protected set; } = SimulatorState.RUNNING;
        public bool IsPrimary { get; set; } = true;
        public bool IsRunning { get { return ISimConnector.GetRunning(Type) || RemoteRunning; } }
        public bool RemoteRunning { get; set; } = false;
        public bool IsLoading { get { return !IsReadySession && SimConnect.IsReceiveRunning; } }
        public bool IsReadyProcess { get { return SimConnect.IsReceiveRunning && SimConnect.IsSimConnected; } }
        public bool IsReadyCommand { get { return IsReadyProcess && SimConnect.IsSimRunning; } }
        public bool IsReadySession { get { return SimConnect.IsSessionRunning; } }
        public bool IsPaused { get { return SimConnect.IsPaused; } }
        public string AircraftString { get { return SimConnect.AircraftString; } }
        public Dictionary<string, List<ISimConnector.EventRegistration>> RegisteredEvents { get; } = [];
        protected Dictionary<string, ISimResourceSubscription> EventSubscriptions { get; } = [];
        protected SimConnectManager SimConnect { get; }
        protected MobiModule MobiModule { get; }
        protected SubManager SubManager { get; }
        protected bool FirstRun { get; set; } = true;
        protected bool FirstConnect { get; set; } = true;
        protected DateTime LastConnectionAttempt { get; set; } = DateTime.MinValue;
        public const string SimConnectTemplate =
"""
[SimConnect]
Protocol=IPv4
Address={0}
Port={1}
MaxReceiveSize=41088
DisableNagle=1
""";

        public ConnectorMSFS()
        {
            CheckSimConnectFile();
            SimConnect = new(App.Configuration, typeof(IdAllocator), App.CancellationToken);
            MobiModule = (MobiModule)SimConnect.AddModule(typeof(MobiModule), App.Configuration);
            SubManager = new SubManager(SimConnect, MobiModule);
            SimConnect.InputManager.CallbackEventsEnumerated += InputEventsEnumerated;
        }

        protected virtual void CheckSimConnectFile()
        {
            try
            {
                string filePath = Path.Join(App.PLUGIN_PATH, "SimConnect.cfg");
                bool fileExists = File.Exists(filePath);

                if (fileExists && !App.Configuration.MsfsRemoteConnection)
                {
                    Logger.Information($"SimConnect.cfg File exits but not using remote Connection - deleting File");
                    File.Delete(filePath);
                    RemoteRunning = false;
                }
                else if (App.Configuration.MsfsRemoteConnection && !App.Configuration.MsfsRemoteHost.StartsWith("127.0.0") && App.Configuration.MsfsRemoteHost.Split(':').Length == 2)
                {
                    Logger.Information($"Setting SimConnect.cfg File for Remote Connection to {App.Configuration.MsfsRemoteHost}");
                    var address = App.Configuration.MsfsRemoteHost.Split(':');
                    string content = string.Format(SimConnectTemplate, address[0], address[1]);
                    File.WriteAllText(filePath, content);
                    RemoteRunning = true;
                }
                else
                    RemoteRunning = false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static async Task<bool> CheckRemoteMsfs(SimulatorType type)
        {
            bool success = false;

            if (type != SimulatorType.MSFS || !App.Configuration.MsfsRemoteConnection || App.Configuration.MsfsRemoteHost.StartsWith("127.0.0") || App.Configuration.MsfsRemoteHost.Split(':').Length != 2)
                return success;

            try
            {
                using var client = new TcpClient();
                var address = IPAddress.Parse(App.Configuration.MsfsRemoteHost.Split(':')[0]);
                var port = int.Parse(App.Configuration.MsfsRemoteHost.Split(':')[1]);
                await client.ConnectAsync(address, port);
                success = client.Connected;
                client.Close();
                if (!success)
                    Logger.Debug($"Connection to remote MSFS not successful");
            }
            catch
            {
                Logger.Verbose($"Error while connecting to remote MSFS");
            }

            return success;
        }

        public async void Run()
        {
            try
            {
                SimConnect.WindowHook.HelperWindow = App.HelperWindow;
                SimConnect.WindowHook.WindowHandle = App.WindowHandle;
                SimConnect.CreateMessageHook();
                LastConnectionAttempt = DateTime.Now;
                while (IsRunning && !App.CancellationToken.IsCancellationRequested && !SimConnect.QuitReceived)
                {
                    if (!SimConnect.IsSimConnected)
                    {
                        var diff = DateTime.Now - LastConnectionAttempt;
                        if (SimConnect.IsSimConnectInitialized && !SimConnect.IsReceiveRunning && diff >= TimeSpan.FromMilliseconds(App.Configuration.StaleTimeout))
                        {
                            if (!FirstConnect)
                            {
                                LastConnectionAttempt = DateTime.Now;
                                Logger.Warning($"Stale Connection detected - force reconnect");
                                SimConnect.Disconnect();
                                continue;
                            }
                            else if (FirstConnect && diff >= TimeSpan.FromMilliseconds(App.Configuration.StaleTimeout * 6))
                            {
                                LastConnectionAttempt = DateTime.Now;
                                Logger.Warning($"Stale initial Connection detected - force reconnect");
                                SimConnect.Disconnect();
                                continue;
                            }
                        }
                        else if (!SimConnect.Connect())
                        {
                            LastConnectionAttempt = DateTime.Now;
                            Logger.Information($"SimConnect not connected - Retry in {App.Configuration.RetryDelay / 1000}s");
                            await Task.Delay(App.Configuration.RetryDelay, App.CancellationToken);
                            continue;
                        }
                        else if (FirstRun)
                        {
                            FirstRun = false;
                            await Task.Delay(500, App.CancellationToken);
                        }
                    }

                    if (SimConnect.IsReceiveRunning && !SimConnect.IsSimConnected && FirstConnect)
                        SimConnect.CheckResources();

                    if (SimConnect.IsSimConnected && FirstConnect)
                    {
                        Logger.Debug($"First Connection established.");
                        FirstConnect = false;
                    }

                    if (!SimConnect.IsReceiveRunning && SimConnect.IsSimConnected)
                    {
                        if (!RemoteRunning)
                        {
                            Logger.Warning($"Receive not running while Connection established! Reconnecting in {(App.Configuration.RetryDelay / 2) / 1000}s");
                            SimConnect.Disconnect();
                            FirstRun = true;
                            await Task.Delay(App.Configuration.RetryDelay / 2, App.CancellationToken);
                            continue;
                        }
                        else
                        {
                            Logger.Warning($"Receive not running while Connection established! Assuming Remote MSFS has closed.");
                            SimConnect.Disconnect();
                            RemoteRunning = false;
                            continue;
                        }
                    }

                    SimConnect.CheckState();

                    if (SimState == SimulatorState.RUNNING && SimConnect.IsReceiveRunning)
                    {
                        Logger.Debug("Changed to LOADING");
                        SimState = SimulatorState.LOADING;
                    }

                    if (SimState == SimulatorState.LOADING && SimConnect.IsSessionStarted)
                    {
                        Logger.Debug("Changed to SESSION");
                        SimState = SimulatorState.SESSION;
                    }

                    if (SimState == SimulatorState.SESSION && SimConnect.IsSessionStopped)
                    {
                        Logger.Debug("Changed to LOADING");
                        SimState = SimulatorState.LOADING;
                    }

                    if (SimState >= SimulatorState.LOADING && (!IsRunning || SimConnect.QuitReceived))
                    {
                        Logger.Debug("Changed to STOPPED");
                        SimState = SimulatorState.STOPPED;
                    }

                    await Task.Delay(App.Configuration.MsfsStateCheckInterval, App.CancellationToken);
                }

                if (RemoteRunning && SimConnect.QuitReceived)
                {
                    if (!App.CancellationToken.IsCancellationRequested)
                        _ = Task.Delay(App.Configuration.IntervalSimMonitor * 3, App.CancellationToken).ContinueWith((_) => RemoteRunning = false);
                }

                if (IsRunning && !SimConnect.QuitReceived)
                    SimConnect.Disconnect();

                SimConnect.WindowHook.ClearHook();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Information($"ConnectorMSFS Task ended (simRunning: {IsRunning} | quitReceived: {SimConnect?.QuitReceived} | cancelled: {App.CancellationToken.IsCancellationRequested})");
        }


        protected virtual void InputEventsEnumerated(InputEventManager manager, bool enumerated)
        {
            try
            {
                if (enumerated)
                {
                    StringBuilder text = new();
                    var inputEvents = manager.InputEvents.Values.Select(i => i.Name).ToList();
                    inputEvents.Sort();
                    foreach (var inputEvent in inputEvents)
                        text.AppendLine($"B:{inputEvent}");
                    File.WriteAllText(AppConfiguration.FILE_BVAR, text.ToString());
                }
                else
                    File.Delete(AppConfiguration.FILE_BVAR);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void Stop()
        {
            foreach (var variable in VariableManager.VariableList.Where(v => v.IsValueMSFS()))
                variable.IsSubscribed = false;
        }

        public void Process()
        {
            int count = SimConnect.CheckResources();
            if (count > 0)
                Logger.Verbose($"Resources updated on Process: {count}");
        }

        public void SubscribeVariable(ManagedVariable managedVariable)
        {
            SubscribeVariables([managedVariable]);
        }

        public void SubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!IsReadyProcess)
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            foreach (var variable in managedVariables)
            {
                if (!variable.IsValueMSFS())
                    continue;

                Logger.Verbose($"Subscribe Variable '{variable.Address}' of Type '{variable.Type}'");
                SubManager.Subscribe(variable);
            }
        }

        public void UnsubscribeVariable(ManagedVariable managedVariable)
        {
            UnsubscribeVariables([managedVariable]);
        }

        public void UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!IsReadyProcess)
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            foreach (var variable in managedVariables)
            {
                if (!variable.IsValueMSFS() || !SubManager.TryGet(variable, out SubMapping mapping))
                    continue;

                Logger.Verbose($"Unsubscribe Variable '{variable.Address}'");
                SubManager.Unsubscribe(mapping);
            }
        }

        public void RemoveUnusedResources(bool force)
        {
            if (force)
                SubManager.Clear();

            SimConnect.ClearUnusedRessources(force);
        }

        protected void OnReceiveEvent(string evtName, object evtData)
        {
            try
            {
                if (RegisteredEvents.TryGetValue(evtName, out var events))
                {
                    foreach (var evt in events)
                        evt.Callback(evtName, evtData);
                }
                else
                    Logger.Warning($"The Event '{evtName}' is not subscribed!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public bool SubscribeSimEvent(string evtName, string receiverID, ISimConnector.EventCallback callbackFunction)
        {
            if (!IsReadyProcess)
                return false;

            if (string.IsNullOrWhiteSpace(evtName) || string.IsNullOrWhiteSpace(receiverID) || callbackFunction == null)
                return false;

            try
            {
                bool doSub = false;
                if (RegisteredEvents.TryGetValue(evtName, out var regList))
                {
                    if (regList.Any(e => e.Name == evtName && e.ReceiverID == receiverID))
                        Logger.Warning($"The Event '{evtName}' is already subscribed for '{receiverID}'!");
                    else
                    {
                        regList.Add(new ISimConnector.EventRegistration(evtName, receiverID, callbackFunction));
                        Logger.Debug($"Subscribed Event '{evtName}' for '{receiverID}'");
                    }
                }
                else
                {
                    RegisteredEvents.Add(evtName, []);
                    RegisteredEvents[evtName].Add(new ISimConnector.EventRegistration(evtName, receiverID, callbackFunction));
                    Logger.Debug($"Subscribed Event '{evtName}' for '{receiverID}'");
                    doSub = true;
                }

                if (doSub)
                {
                    var sub = SimConnect.EventManager.Subscribe(evtName);
                    if (sub == null)
                        return false;

                    sub.OnReceived += (sub, value) =>
                    {
                        OnReceiveEvent(sub.Name, value);
                    };
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public bool UnsubscribeSimEvent(string evtName, string receiverID)
        {
            if (!IsReadyProcess)
                return false;

            if (string.IsNullOrWhiteSpace(evtName) || string.IsNullOrWhiteSpace(receiverID))
                return false;

            try
            {
                bool doSub = false;
                if (RegisteredEvents.TryGetValue(evtName, out var regList))
                {
                    ISimConnector.EventRegistration reg = regList.FirstOrDefault(e => e.Name == evtName && e.ReceiverID == receiverID, null);
                    if (reg == null)
                        Logger.Warning($"The Event '{evtName}' is not subscribed for '{receiverID}'!");
                    else
                    {
                        regList.Remove(reg);
                        Logger.Debug($"Unsubscribed Event '{evtName}' for '{receiverID}'");
                        if (regList.Count == 0)
                        {
                            doSub = true;
                            RegisteredEvents.Remove(evtName);
                        }
                    }
                }
                else
                    Logger.Warning($"The Event '{evtName}' is not subscribed!");

                if (doSub && EventSubscriptions.TryGetValue(evtName, out var sub))
                {
                    if (sub == null)
                        return false;

                    sub.Unsubscribe();
                    EventSubscriptions.Remove(evtName);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public static bool IsCommandMSFS(SimCommandType? type)
        {
            return type == SimCommandType.AVAR || type == SimCommandType.BVAR || type == SimCommandType.HVAR || type == SimCommandType.KVAR || type == SimCommandType.LVAR
                || type == SimCommandType.CALCULATOR;
        }

        public bool CanRunCommand(SimCommand command)
        {
            return IsCommandMSFS(command?.Type);
        }

        public async Task<bool> RunCommand(SimCommand command)
        {
            if (!IsReadyCommand)
                return false;

            bool result = false;

            if (command.Type == SimCommandType.LVAR || command.Type == SimCommandType.AVAR)
                result = await WriteSimVariable(command);
            else if (command.Type == SimCommandType.KVAR)
                result = await WriteEventVariable(command);
            else if (command.Type == SimCommandType.BVAR)
                result = await SendInputEvent(command);
            else if (command.Type == SimCommandType.HVAR)
                result = await WriteHtmlVariable(command);
            else if (command.Type == SimCommandType.CALCULATOR)
                result = await ExecuteCalculatorCode(command);

            return result;
        }

        protected async Task<bool> WriteSimVariable(SimCommand command)
        {
            Logger.Debug($"Running WriteSimVariable for Command ({command})");
            if (!VariableManager.TryGet(command.Address, out var variable))
            {
                Logger.Debug($"Need to Register Variable for Command");
                variable = VariableManager.RegisterVariable(command.Address);
                if (variable != null)
                    SubscribeVariable(variable);
                else
                {
                    Logger.Warning($"Could not register Variable!");
                    return false;
                }
            }

            if (!SubManager.TryGet(variable, out var sub) || sub?.Subscription is not SimVarSubscription varSub)
            {
                Logger.Warning($"No SubMapping found for Variable '{variable}'");
                return false;
            }

            if (!varSub.Resource.IsRegistered)
            {
                Logger.Debug($"Need to Register SimVar for Command");
                varSub.Resource.Register();
            }

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = varSub.Resource.WriteValue(command.NumValue);
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (varSub.Resource.WriteValue(command.ResetNumValue) && result)
                        success++;
                }
                else if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }

        protected async Task<bool> WriteEventVariable(SimCommand command)
        {
            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = await ToolsMSFS.WriteKvar(command, SimConnect.EventManager);
                if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }

        protected async Task<bool> SendInputEvent(SimCommand command)
        {
            if (!command.DoNotRequest && TypeMatching.rxBvarValue.IsMatch(command.Address))
                return await WriteInputEventVariable(command);
            else if (command.DoNotRequest && TypeMatching.rxBvarCmd.IsMatch(command.Address))
                return await WriteInputEventCommand(command);            
            else
                return false;
        }

        protected async Task<bool> WriteInputEventVariable(SimCommand command)
        {
            Logger.Verbose($"Running SendInputEvent for (Value) Command ({command})");
            if (string.IsNullOrWhiteSpace(command.Value))
                command.Value = "1";
            if (string.IsNullOrWhiteSpace(command.ResetValue))
                command.ResetValue = "1";

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = SimConnect.InputManager.SendEvent(command.Address.Name, command.NumValue);
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (SimConnect.InputManager.SendEvent(command.Address.Name, command.ResetNumValue) && result)
                        success++;
                }
                else if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }

        protected async Task<bool> WriteInputEventCommand(SimCommand command)
        {
            Logger.Verbose($"Running SendInputEvent for (Non-Value) Command ({command})");

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = await WriteInputEvents(command);
                if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }

        protected async Task<bool> WriteInputEvents(SimCommand command)
        {
            bool result = false;
            string[] bVars = command.Address.Address.Split(':');

            for (int idx = 0; idx < bVars.Length; idx++)
            {
                if (bVars[idx] == "B")
                    continue;

                if (idx + 1 < bVars.Length && Conversion.IsNumber(bVars[idx + 1], out double numValue))
                {
                    result = SimConnect.InputManager.SendEvent(bVars[idx], numValue);
                    idx++;
                }
                else
                    result = SimConnect.InputManager.SendEvent(bVars[idx], 1);

                if (!result)
                    break;

                if (command.CommandDelay > 0)
                    await Task.Delay(command.CommandDelay, App.CancellationToken);
            }

            return result;
        }

        protected async Task<bool> WriteHtmlVariable(SimCommand command)
        {
            Logger.Verbose($"Running WriteHtmlVariable for Command ({command})");
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (await ToolsMSFS.WriteHvar(command, MobiModule))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        protected async Task<bool> ExecuteCalculatorCode(SimCommand command)
        {
            Logger.Debug($"Running ExecuteCalculatorCode for Command ({command})");
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (MobiModule.ExecuteCode(command.Address.Name))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    SimConnect.InputManager.CallbackEventsEnumerated -= InputEventsEnumerated;
                    SimConnect.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
