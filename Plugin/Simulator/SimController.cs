using PilotsDeck.Plugin;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PilotsDeck.Simulator
{
    public class SimController
    {
        protected Channel<SimCommand> ChannelCommandsSend { get; } = Channel.CreateUnbounded<SimCommand>();
        public ChannelWriter<SimCommand> CommandChannel { get { return ChannelCommandsSend.Writer; } }
        
        protected DispatcherTimer TimerProcess { get; set; } = new();
        protected DateTime LastUnusedCheck { get; set; } = DateTime.Now;
        protected bool FirstProcess { get; set; } = true;
        protected DispatcherTimer TimerMonitor { get; set; } = new();
        protected static VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        public ConcurrentDictionary<SimulatorType, ISimConnector> ActiveConnectors { get; protected set; } = [];

        public SimulatorType? SimMainType { get { return ActiveConnectors.Values.Where(c => c.IsPrimary)?.FirstOrDefault()?.Type; } }
        public SimulatorState SimState { get { return ActiveConnectors.Values.Where(c => c.IsPrimary)?.FirstOrDefault()?.SimState ?? SimulatorState.UNKNOWN; } }
        public bool IsRunning { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsRunning).Any(); } }
        public bool IsLoading { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsLoading).Any(); } }
        public bool IsReadyProcess { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsReadyProcess).Any(); } }
        public bool IsReadyCommand { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsReadyCommand).Any(); } }
        public bool IsReadySession { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsReadySession).Any(); } }
        public bool IsPaused { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsPaused).Any(); } }
        public string AircraftString { get { return ActiveConnectors.Values.Where(c => c.IsPrimary && c.IsReadySession).FirstOrDefault()?.AircraftString; } }

        public async void Run()
        {
            StatisticManager.AddTracker(StatisticID.SIM_PROCESS);
            StatisticManager.AddTracker(StatisticID.SIM_COMMANDS);

            TimerProcess.Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalSimProcess);
            TimerProcess.Tick += Process;

            TimerMonitor.Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalSimMonitor / 2);
            TimerMonitor.Tick += MonitorSimulators;
            Logger.Information($"Starting Monitor Timer");
            TimerMonitor.Start();

            await CommandTask();
            TimerProcess?.Stop();
            TimerMonitor?.Stop();
            Logger.Information("SimController ended");
        }

        protected async void MonitorSimulators(object sender, EventArgs e)
        {
            if (App.CancellationTokenSource.IsCancellationRequested)
            {
                Logger.Information($"Cancellation received - stopping Sim Monitor Timer");
                TimerProcess.Stop();
                return;
            }

            if (TimerMonitor.Interval.Milliseconds != App.Configuration.IntervalSimMonitor)
                TimerMonitor.Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalSimMonitor);

            foreach (var sim in App.Configuration.SimBinaries)
            {
                if (!ActiveConnectors.ContainsKey(sim.Key) && (sim.Value.Where(b => Sys.GetProcessRunning(b)).Any() || await ConnectorXP.CheckRemoteXP(sim.Key)))
                {
                    Logger.Information($"Simulator Binary '{string.Join(" | ", sim.Value)}' detected - finding Connector for Type '{sim.Key}'");
                    ISimConnector simConnector = null;
                    ISimConnector secondaryConnector = null;
                    SimulatorType secondaryType = SimulatorType.NONE;
                    switch (sim.Key)
                    {
                        case SimulatorType t when t == SimulatorType.FSX || t == SimulatorType.P3D:
                            if (t == SimulatorType.FSX)
                                simConnector = new ConnectorFSUIPC(SimulatorType.FSX);
                            else
                                simConnector = new ConnectorFSUIPC();
                            break;
                        case SimulatorType.MSFS:
                            simConnector = new ConnectorMSFS();
                            if (App.Configuration.UseFsuipcForMSFS)
                            {
                                secondaryConnector = new ConnectorFSUIPC(SimulatorType.FSUIPC7)
                                {
                                    IsPrimary = false
                                };
                                secondaryType = SimulatorType.FSUIPC7;
                            }
                            break;
                        case SimulatorType.XP:
                            simConnector = new ConnectorXP();
                            break;
                        default:
                            Logger.Error($"The Simulator is not supported!");
                            break;
                    }
                    ActiveConnectors.TryAdd(sim.Key, simConnector);
                    Task task = new(simConnector.Run, App.CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                    task.Start();
                    Logger.Information($"Connector Task for '{sim.Key}' started");

                    if (secondaryConnector != null)
                    {
                        ActiveConnectors.TryAdd(secondaryType, secondaryConnector);
                        task = new(secondaryConnector.Run, App.CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                        task.Start();
                        Logger.Information($"Secondary Connector Task for '{secondaryType}' started");
                    }

                    StartProcessTimer();
                }
            }

            var closed = ActiveConnectors.Where(s => !s.Value.IsRunning).Select(s => s.Key).ToList();
            foreach (var sim in closed)
            {
                Logger.Information($"Connector for '{sim}' has closed - remove from active");
                var connector = ActiveConnectors[sim];
                connector.Stop();
                ActiveConnectors[sim] = null;
                ActiveConnectors.Remove(sim, out _);
                _ = Task.Delay(App.Configuration.IntervalSimMonitor / 2).ContinueWith(s => DelayedDispose(connector));
            }
            
            if (!ActiveConnectors.Where(s => s.Value.IsRunning).Any() && TimerProcess.IsEnabled)
            {
                Logger.Debug($"Stopping Process Timer");
                TimerProcess.Stop();
            }
        }

        protected void StartProcessTimer()
        {
            if (!TimerProcess.IsEnabled)
            {
                Logger.Debug($"Starting Process Timer");
                FirstProcess = true;
                TimerProcess.Start();
            }
        }

        protected static void DelayedDispose(ISimConnector connector)
        {
            connector?.Dispose();
        }

        protected void Process(object sender, EventArgs e)
        {
            StatisticManager.StartTrack(StatisticID.SIM_PROCESS);

            if (App.CancellationTokenSource.IsCancellationRequested)
            {
                Logger.Information($"Cancellation received - stopping Process Timer");
                TimerProcess.Stop();
                return;
            }

            try
            {
                var readyConnectors = ActiveConnectors.Values.Where(c => c.IsReadyProcess);

                if (readyConnectors.Any())
                {
                    var query = VariableManager.ManagedVariables.Where(v => !v.Value.IsSubscribed && v.Value.Registrations >= 1 && !v.Value.IsValueInternal()).Select(v => v.Value).ToList();
                    if (query.Count > 0)
                    {
                        Logger.Verbose($"Subscribe {query.Count} Variables");
                        SubscribeVariables(query);
                    }

                    ConnectorNone.Process();
                    foreach (var connector in readyConnectors)
                        connector.Process();

                    if (FirstProcess)
                    {
                        LastUnusedCheck = DateTime.Now;
                        FirstProcess = false;
                    }
                    else if (SimState == SimulatorState.SESSION && DateTime.Now - LastUnusedCheck >= TimeSpan.FromMilliseconds(App.Configuration.IntervalUnusedVariables))
                    {
                        var list = VariableManager.ManagedVariables.Where(v => v.Value.IsSubscribed && v.Value.Registrations <= 0).Select(v => v.Value).ToList();
                        if (list.Count > 0)
                        {
                            Logger.Information($"Unsubscribing {list.Count} unused Variables");
                            foreach (var value in list)
                                Logger.Debug(value.Address);
                            UnsubscribeVariables(list);
                            ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedVariables(false);
                            ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedVariables(false);
                        }
                        LastUnusedCheck = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            StatisticManager.EndTrack(StatisticID.SIM_PROCESS);
        }

        public void RemoveUnusedVariables(bool force)
        {
            ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedVariables(force);
            ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedVariables(force);
        }

        public void SubscribeVariable(ManagedVariable managedVariable)
        {
            ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.SubscribeVariable(managedVariable);
            if (!managedVariable.IsSubscribed)
                ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.SubscribeVariable(managedVariable);

            if (!managedVariable.IsSubscribed)
                Logger.Error($"Could not subscribe Managed Value '{managedVariable.Address}' on any Connector");
        }

        public void SubscribeVariables(List<ManagedVariable> managedVariables)
        {
            var primaryConnector = ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary).FirstOrDefault();
            primaryConnector?.SubscribeVariables([.. managedVariables]);

            var query = ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary);
            if (query.Any())
            {
                managedVariables = managedVariables.Where(v => !v.IsSubscribed && v.Registrations >= 1).ToList();
                query.First().SubscribeVariables([.. managedVariables]);
            }
        }

        public void UnsubscribeVariable(ManagedVariable managedVariable)
        {
            ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.UnsubscribeVariable(managedVariable);
            if (managedVariable.IsSubscribed)
                ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.UnsubscribeVariable(managedVariable);

            if (managedVariable.IsSubscribed)
                Logger.Error($"Could not unsubscribe Managed Value '{managedVariable.Address}' on any Connector");
        }

        public void UnsubscribeVariables(List<ManagedVariable> managedVariables)
        {
            var primaryConnector = ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary).FirstOrDefault();
            primaryConnector?.UnsubscribeVariables([.. managedVariables]);

            var query = ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary);
            if (query.Any())
                query.First().UnsubscribeVariables([.. managedVariables]);
        }

        public void SubscribeSimEvent(string evtName, string receiverID, ISimConnector.EventCallback callbackFunction)
        {
            if (ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.SubscribeSimEvent(evtName, receiverID, callbackFunction) != true)
                if (ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.SubscribeSimEvent(evtName, receiverID, callbackFunction) != true)
                    Logger.Debug($"Could not subscribe SimEvent '{evtName}' for '{receiverID}' on any Connector");
        }

        public void UnsubscribeSimEvent(string evtName, string receiverID)
        {
            if (ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.UnsubscribeSimEvent(evtName, receiverID) != true)
                if (ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.UnsubscribeSimEvent(evtName, receiverID) != true)
                    Logger.Debug($"Could not unsubscribe SimEvent '{evtName}' for '{receiverID}' on any Connector");
        }

        protected async Task CommandTask()
        {
            try
            {
                Logger.Information("CommandTask started");
                while (!App.CancellationToken.IsCancellationRequested)
                {
                    SimCommand command = await ChannelCommandsSend.Reader.ReadAsync(App.CancellationToken);
                    bool handled = false;
                    StatisticManager.StartTrack(StatisticID.SIM_COMMANDS);

                    try
                    {
                        if (command == null || !command.IsValid)
                        {
                            Logger.Warning($"Command is not valid! ({command})");
                            handled = false;
                        }
                        else if (command is DelayCommand delay)
                        {
                            Logger.Debug($"Running Delay Command ({command.CommandDelay}ms)");
                            await Task.Delay(delay.CommandDelay, App.CancellationToken);
                            handled = true;
                        }
                        else if (ConnectorNone.IsNoSimCommand(command))
                        {
                            if (command.Type != SimCommandType.INTERNAL)
                                Logger.Debug($"Running Command ({command}) on ConnectorNone");
                            handled = await ConnectorNone.RunCommand(command);
                        }
                        else
                        {
                            var primary = ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary && c.CanRunCommand(command));
                            if (primary.Any())
                            {
                                Logger.Debug($"Running Command ({command}) on Connector {primary.First().Type}");
                                handled = await primary.First().RunCommand(command);
                            }

                            var secondary = ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary && c.CanRunCommand(command));
                            if (secondary.Any())
                            {
                                Logger.Debug($"Running Command ({command}) on Connector {secondary.First().Type}");
                                handled = await secondary.First().RunCommand(command);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Exception while running Command: '{command?.Address}'");
                        Logger.LogException(ex);
                    }

                    if (!handled)
                    {
                        Logger.Warning($"No active & ready Connector can run the Command '{command?.Type}' Command '{command?.Address}' (Valid-Check: {command?.IsValid})");
                        _ = App.DeckController.SendShowAlert(command?.Context);
                    }

                    StatisticManager.EndTrack(StatisticID.SIM_COMMANDS);
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Information("CommandTask ended");
        }
    }
}
