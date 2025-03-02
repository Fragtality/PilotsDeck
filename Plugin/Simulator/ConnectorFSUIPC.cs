using CFIT.AppLogger;
using CFIT.AppTools;
using FSUIPC;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.FSUIPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public class ConnectorFSUIPC(SimulatorType type = SimulatorType.P3D) : ISimConnector
    {
        public SimulatorType Type { get; protected set; } = type;
        public SimulatorState SimState { get; protected set; } = SimulatorState.RUNNING;
        public bool IsPrimary { get; set; } = true;
        public bool IsRunning { get { return ISimConnector.GetRunning(Type) || (Type == SimulatorType.FSUIPC7 && (Sys.GetProcessRunning(App.Configuration.BinaryFSUIPC7) || App.Configuration.SimBinaries[SimulatorType.MSFS].Where(b => Sys.GetProcessRunning(b)).Any())); } }
        public bool IsLoading { get { return SimLoading; } }
        public bool IsReadyProcess { get { return FSUIPCConnection.IsOpen && FsuipcManager.IsInitialized; } }
        protected bool LastProcess { get; set; } = false;
        public bool IsReadyCommand { get { return IsReadyProcess && /*!IsPaused &&*/ !SimLoading && LastProcess; } }
        public bool IsReadySession { get { return IsReadyProcess && !IsPaused && !SimLoading && LastProcess; } }
        public bool IsPaused { get { return FsuipcManager?.IsPaused == true || IsInMenu; } }
        private bool SimLoading { get; set; } = false;
        private bool LastPaused { get; set; } = true;
        public bool IsInMenu { get { return FsuipcManager?.IsInMenu == true || FsuipcManager?.IsInMenuFsx == true; } }
        private bool LastMenu { get; set; } = true;
        public string AircraftString { get { return FsuipcManager?.AircraftString; } }
        private string LastAircraft { get; set; } = "";
        protected static VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        protected FsuipcManager FsuipcManager { get; } = new();

        public Dictionary<string, List<ISimConnector.EventRegistration>> RegisteredEvents { get; private set; } = [];

        public async void Run()
        {
            try
            {
                while (IsRunning && !App.CancellationToken.IsCancellationRequested)
                {
                    if (!FSUIPCConnection.IsOpen)
                    {
                        ResetVariables();
                        if (Type != SimulatorType.FSUIPC7)
                        {
                            if (await FsuipcManager.Connect())
                                SimLoading = true;
                        }
                        else if (Type == SimulatorType.FSUIPC7 && Sys.GetProcessRunning(App.Configuration.BinaryFSUIPC7))
                            await FsuipcManager.Connect();
                    }
                    if (!FSUIPCConnection.IsOpen)
                        await Task.Delay(App.Configuration.FsuipcRetryDelay, App.CancellationToken);

                    if (SimState == SimulatorState.RUNNING && SimLoading)
                    {
                        Logger.Debug("Changed to LOADING");
                        SimState = SimulatorState.LOADING;
                    }

                    if (SimState == SimulatorState.LOADING && !SimLoading && !string.IsNullOrWhiteSpace(AircraftString) && !IsPaused)
                    {
                        Logger.Debug("Changed to SESSION");
                        SimState = SimulatorState.SESSION;
                    }

                    if (SimState == SimulatorState.SESSION && SimLoading)
                    {
                        Logger.Debug("Changed to LOADING");
                        SimState = SimulatorState.LOADING;
                    }

                    if (SimState >= SimulatorState.LOADING && !IsRunning)
                    {
                        Logger.Debug("Changed to STOPPED");
                        SimState = SimulatorState.STOPPED;
                    }

                    await Task.Delay(App.Configuration.FsuipcStateCheckInterval, App.CancellationToken);
                }
                FsuipcManager.Disconnect();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Information($"ConnectorFSUIPC Task ended");
        }

        public void Stop()
        {
            ResetVariables();
        }

        protected static void ResetVariables()
        {
            foreach (var variable in App.PluginController.VariableManager.VariableList.Where(v => v.IsValueFSUIPC()))
                variable.IsSubscribed = false;
        }

        public bool IsCommandFSUIPC(SimCommandType? type)
        {
            return (type == SimCommandType.LVAR && Type != SimulatorType.FSUIPC7) || type == SimCommandType.CONTROL || type == SimCommandType.MACRO || type == SimCommandType.OFFSET || type == SimCommandType.SCRIPT
                || type == SimCommandType.VJOY;
        }

        public bool CanRunCommand(SimCommand command)
        {
            return IsCommandFSUIPC(command?.Type);
        }

        public void Process()
        {
            try
            {
                LastProcess = FsuipcManager.Process();

                if (LastProcess)
                {
                    var offsets = VariableManager.VariableList.Where(v => v.Type == SimValueType.OFFSET && !v.IsConnected && v.Registrations > 0);
                    foreach (var variable in offsets)
                        variable.Connect();
                }

                if (LastProcess && Type != SimulatorType.FSUIPC7)
                {
                    if (SimLoading && FsuipcManager.IsInitialized && LastProcess && !IsInMenu)
                    {
                        Logger.Debug($"SimLoading to false");
                        SimLoading = false;
                    }

                    if (!SimLoading && (!LastProcess || (LastProcess && LastAircraft != AircraftString)))
                    {
                        Logger.Debug($"SimLoading to true");
                        LastAircraft = AircraftString;
                        SimLoading = true;
                    }

                    if (LastMenu != IsInMenu)
                    {
                        Logger.Debug($"Menu changed: {IsInMenu}");
                        LastMenu = IsInMenu;
                    }

                    if (LastPaused != IsPaused)
                    {
                        Logger.Debug($"Pause changed: {IsPaused}");
                        LastPaused = IsPaused;
                    }

                    var lvars = VariableManager.VariableList.Where(v => v.Type == SimValueType.LVAR && v.Registrations > 0);
                    string address;
                    foreach (var variable in lvars)
                    {
                        address = variable.Address.FormatFsuipcLvar();
                        if (!string.IsNullOrWhiteSpace(address))
                        {
                            try { variable.SetValue(FSUIPCConnection.ReadLVar(address)); }
                            catch (FSUIPCException) { Logger.Error($"FSUIPCException during Read for '{address}'"); }
                        }
                        else
                            Logger.Debug($"Could not format Address for FSUIPC: '{variable.Address}'");
                    }
                }

                if (!LastProcess)
                    Logger.Warning("FSUIPC is not ready to process");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                LastProcess = false;
            }
        }

        public async Task<bool> RunCommand(SimCommand command)
        {
            if (!IsReadyCommand)
                return false;

            bool result = false;

            if (command.Type == SimCommandType.CONTROL)
                result = await SendControls(command);
            else if (command.Type == SimCommandType.MACRO)
                result = await RunMacros(command);
            else if (command.Type == SimCommandType.OFFSET)
                result = await WriteOffset(command);
            else if (command.Type == SimCommandType.LVAR)
                result = await WriteLvar(command);
            else if (command.Type == SimCommandType.SCRIPT)
                result = await RunScripts(command);
            else if (command.Type == SimCommandType.VJOY)
                result = await RunVjoy(command);

            return result;
        }

        protected static async Task<bool> SendControls(SimCommand command)
        {
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (await ToolsFSUIPC.SendControls(command))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        protected static async Task<bool> RunMacros(SimCommand command)
        {
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (await ToolsFSUIPC.RunMacros(command))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        protected static async Task<bool> WriteOffset(SimCommand command)
        {
            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = ToolsFSUIPC.WriteOffset(command.Address, command.Value);
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (ToolsFSUIPC.WriteOffset(command.Address, command.ResetValue) && result)
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

        protected static async Task<bool> WriteLvar(SimCommand command)
        {
            int success = 0;
            int i;
            for (i = 0; i < command.Ticks; i++)
            {
                bool result = ToolsFSUIPC.WriteLvar(command.Address, command.NumValue);
                if (command.IsValueReset && command.ResetDelay > 0)
                {
                    await Task.Delay(command.ResetDelay, App.CancellationToken);
                    if (ToolsFSUIPC.WriteLvar(command.Address, Conversion.ToDouble(command.ResetValue)) && result)
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

        protected static async Task<bool> RunScripts(SimCommand command)
        {
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (await ToolsFSUIPC.RunScripts(command))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        protected static async Task<bool> RunVjoy(SimCommand command)
        {
            bool result;

            int success = 0;
            for (int n = 0; n < command.Ticks; n++)
            {
                if (await ToolsFSUIPC.RunVjoy(command))
                {
                    success++;
                    if (command.Ticks > 1)
                        await Task.Delay(command.TickDelay, App.CancellationToken);
                }
            }
            result = success == command.Ticks;

            return result;
        }

        public bool SubscribeSimEvent(string evtName, string receiverID, ISimConnector.EventCallback callbackFunction)
        {
            return false;
        }

        public void SubscribeVariable(ManagedVariable managedVariable)
        {
            try
            {
                if (managedVariable is VariableOffset && !managedVariable.IsSubscribed)
                    managedVariable.Connect();
                else if (managedVariable.IsValueFSUIPC() && !managedVariable.IsSubscribed)
                    managedVariable.IsSubscribed = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void SubscribeVariables(ManagedVariable[] managedVariables)
        {
            try
            {
                foreach (var variable in managedVariables)
                    if (variable is VariableOffset && !variable.IsSubscribed)
                        variable.Connect();
                    else if (variable.IsValueFSUIPC() && !variable.IsSubscribed)
                        variable.IsSubscribed = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public bool UnsubscribeSimEvent(string evtName, string receiverID)
        {
            return false;
        }

        public void UnsubscribeVariable(ManagedVariable managedVariable)
        {
            try
            {
                if (managedVariable?.IsValueFSUIPC() == true && managedVariable.IsSubscribed)
                {
                    if (managedVariable is VariableOffset offset)
                        offset.Disconnect();
                    else
                        managedVariable.IsSubscribed = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            try
            {
                foreach (var variable in managedVariables)
                {
                    if (variable?.IsValueFSUIPC() == true && variable.IsSubscribed)
                    {
                        if (variable is VariableOffset offset)
                            offset.Disconnect();
                        else
                            variable.IsSubscribed = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void RemoveUnusedResources(bool force)
        {
            
        }


        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    FsuipcManager?.Disconnect();
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
