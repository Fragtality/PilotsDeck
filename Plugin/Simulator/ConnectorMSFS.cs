using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.MSFS;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public class ConnectorMSFS : ISimConnector
    {
        public SimulatorType Type { get; } = SimulatorType.MSFS;
        public SimulatorState SimState { get; protected set; } = SimulatorState.RUNNING;
        public bool IsPrimary { get; set; } = true;
        public bool IsRunning { get { return ISimConnector.GetRunning(Type); } }
        public bool IsLoading { get { return !IsReadySession && SimConnectManager.IsConnected && SimConnectManager.IsReceiveRunning; } }
        public bool IsReadyProcess { get { return SimConnectManager.IsReceiveRunning && SimConnectManager.IsConnected; } }
        public bool IsReadyCommand { get { return IsReadyProcess && !IsPaused; } }
        public bool IsReadySession { get { return SimConnectManager.IsSessionReady && IsReadyProcess && !string.IsNullOrWhiteSpace(AircraftString); } }              
        public bool IsPaused { get { return SimConnectManager.IsPaused; } }
        public string AircraftString { get { return SimConnectManager.AircraftString; } }
        public static SimConnectManager SimConnectManager { get; protected set; }
        public Dictionary<string, List<ISimConnector.EventRegistration>> RegisteredEvents { get; private set; } = [];

        public ConnectorMSFS()
        {
            SimConnectManager = new(OnReceiveEvent);
        }

        public async void Run()
        {
            try
            {
                while (IsRunning && !App.CancellationToken.IsCancellationRequested)
                {
                    if (!SimConnectManager.IsSimConnected && SimState != SimulatorState.STOPPED)
                    {
                        if (!SimConnectManager.Connect())
                        {
                            await Task.Delay(App.Configuration.MsfsRetryDelay, App.CancellationToken);
                            continue;
                        }
                    }

                    if (!SimConnectManager.IsReceiveRunning && SimConnectManager.IsSimConnected)
                    {
                        SimConnectManager.Disconnect();
                        await Task.Delay(App.Configuration.MsfsRetryDelay / 2, App.CancellationToken);
                        continue;
                    }

                    if (SimConnectManager.IsSimConnected)
                            SimConnectManager.MobiModule?.CheckConnection(SimConnectManager.IsSessionReady);

                    if (SimState == SimulatorState.RUNNING && SimConnectManager.IsSimConnected && SimConnectManager.IsReceiveRunning)
                    {
                        Logger.Debug("Changed to LOADING");
                        SimState = SimulatorState.LOADING;
                    }

                    if (SimState == SimulatorState.LOADING && IsReadySession)
                    {
                        Logger.Debug("Changed to SESSION");
                        SimState = SimulatorState.SESSION;
                    }

                    if (SimState == SimulatorState.SESSION && IsLoading)
                    {
                        Logger.Debug("Changed to LOADING");
                        SimState = SimulatorState.LOADING;
                    }

                    if (SimState >= SimulatorState.LOADING && (!IsRunning || SimConnectManager.QuitReceived))
                    {
                        Logger.Debug("Changed to STOPPED");
                        SimState = SimulatorState.STOPPED;
                    }

                    SimConnectManager.CheckAircraftString();

                    await Task.Delay(App.Configuration.MsfsStateCheckInterval, App.CancellationToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Information($"ConnectorMSFS Task ended");
        }

        public void Stop()
        {
            foreach (var variable in App.PluginController.VariableManager.VariableList.Where(v => v.IsValueMSFS()))
                variable.IsSubscribed = false;
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

        public void Process()
        {
            try
            {
                SimConnectManager.InputEvents.RequestInputEventValues();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
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

        protected static async Task<bool> WriteSimVariable(SimCommand command)
        {
            Logger.Debug($"Running WriteSimVariable for Command ({command})");
            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = MobiModule.SetSimVar(command.Address, command.Value);
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (MobiModule.SetSimVar(command.Address, command.ResetValue) && result)
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

        protected static async Task<bool> WriteEventVariable(SimCommand command)
        {
            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = await ToolsMSFS.WriteKvar(command);
                if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }


        protected static async Task<bool> SendInputEvent(SimCommand command)
        {
            if (command.DoNotRequest && TypeMatching.rxBvarCmd.IsMatch(command.Address))
                return await WriteInputEventCommand(command);
            else if (!command.DoNotRequest && TypeMatching.rxBvarValue.IsMatch(command.Address))
                return await WriteInputEventVariable(command);
            else
                return false;
        }

        protected static async Task<bool> WriteInputEventVariable(SimCommand command)
        {
            Logger.Verbose($"Running SendInputEvent for (Value) Command ({command})");
            if (string.IsNullOrWhiteSpace(command.Value))
                command.Value = "1";

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = SimConnectManager.InputEvents.SendInputEvent(command.Address, command.NumValue);
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (SimConnectManager.InputEvents.SendInputEvent(command.Address, Conversion.ToDouble(command.ResetValue, 1)) && result)
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

        protected static async Task<bool> WriteInputEventCommand(SimCommand command)
        {
            Logger.Verbose($"Running SendInputEvent for (Non-Value) Command ({command})");

            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = await WriteBvar(command);
                if (result)
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }

            return success == command.Ticks;
        }

        protected static async Task<bool> WriteBvar(SimCommand command)
        {
            bool result = false;
            string[] bVars = command.Address.Split(':');

            for (int idx = 0; idx < bVars.Length; idx++)
            {
                if (bVars[idx] == "B")
                    continue;

                if (!bVars[idx].StartsWith("B:"))
                    bVars[idx] = "B:" + bVars[idx];

                if (idx + 1 < bVars.Length && Conversion.IsNumber(bVars[idx + 1], out double numValue))
                {
                    result = SimConnectManager.InputEvents.SendInputEvent(bVars[idx], numValue);
                    idx++;
                }
                else
                    result = SimConnectManager.InputEvents.SendInputEvent(bVars[idx], 1);

                if (!result)
                    break;

                if (command.CommandDelay > 0)
                    await Task.Delay(command.CommandDelay, App.CancellationToken);
            }

            return result;
        }

        protected static async Task<bool> WriteHtmlVariable(SimCommand command)
        {
            Logger.Verbose($"Running WriteHtmlVariable for Command ({command})");
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (await ToolsMSFS.WriteHvar(command))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        protected static async Task<bool> ExecuteCalculatorCode(SimCommand command)
        {
            Logger.Debug($"Running ExecuteCalculatorCode for Command ({command})");
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (MobiModule.ExecuteCode(command.Address))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        public void OnReceiveEvent(string evtName, object evtData)
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
                    SimConnectManager.SubscribeSimConnectEvent(evtName);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public void SubscribeVariable(ManagedVariable managedVariable)
        {
            if (!IsReadyProcess)
                return;

            if (managedVariable == null)
                return;

            if (managedVariable is VariableInputEvent && IsReadySession)
                SimConnectManager.InputEvents.SubscribeInputEvent(managedVariable.Address, managedVariable as VariableInputEvent);
            else if (managedVariable.IsValueMSFS())
                SimConnectManager.MobiModule.SubscribeAddress(managedVariable.Address, managedVariable);
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
                if (variable is VariableInputEvent && IsReadySession)
                    SimConnectManager.InputEvents.SubscribeInputEvent(variable.Address, variable as VariableInputEvent);
                else
                    SimConnectManager.MobiModule.SubscribeAddress(variable.Address, variable);
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

                if (doSub)
                    SimConnectManager.UnsubscribeSimConnectEvent(evtName);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public void UnsubscribeVariable(ManagedVariable managedVariable)
        {
            if (!IsReadyProcess)
                return;

            if (managedVariable == null)
                return;

            if (managedVariable is VariableInputEvent)
                SimConnectManager.InputEvents.UnsubscribeInputEvent(managedVariable.Address);
            else if (managedVariable.IsValueMSFS())
                SimConnectManager.MobiModule.UnsubscribeAddress(managedVariable.Address);
        }

        public void UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!IsReadyProcess)
                return;

            if (managedVariables == null || managedVariables.Length == 0)
                return;

            foreach (var variable in managedVariables)
            {
                if (variable is VariableInputEvent)
                    SimConnectManager.InputEvents.UnsubscribeInputEvent(variable.Address);
                else if (variable.IsValueMSFS())
                    SimConnectManager.MobiModule.UnsubscribeAddress(variable.Address);
            }
        }

        public void RemoveUnusedVariables(bool force)
        {
            if (!IsReadyProcess)
                return;

            SimConnectManager.MobiModule.MobiVariables.ReorderRegistrations(force);
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    SimConnectManager.Disconnect();
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
