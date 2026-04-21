using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Plugin;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public class SimController
    {
        protected Channel<SimCommand> ChannelCommandsSend { get; } = Channel.CreateUnbounded<SimCommand>();
        public ChannelWriter<SimCommand> CommandChannel { get { return ChannelCommandsSend.Writer; } }

        protected DispatcherTimerAsync TimerProcess { get; set; } = new();
        protected DateTime LastUnusedCheck { get; set; } = DateTime.Now;
        protected bool FirstProcess { get; set; } = true;
        protected DispatcherTimerAsync TimerMonitor { get; set; } = new();
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
        protected bool ProcessIsRunning { get; set; } = false;

        public async Task Run()
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

        protected async Task MonitorSimulators()
        {
            if (App.CancellationTokenSource.IsCancellationRequested)
            {
                Logger.Information($"Cancellation received - stopping Sim Monitor Timer");
                TimerProcess.Stop();
                return;
            }

            if (TimerMonitor.Interval.Milliseconds != App.Configuration.IntervalSimMonitor)
                TimerMonitor.Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalSimMonitor);

            ISimConnector.RefreshProcesses();
            foreach (var sim in App.Configuration.SimBinaries)
            {
                if (!ActiveConnectors.ContainsKey(sim.Key) && (sim.Value.Where(b => ISimConnector.GetRunning(b)).Any() || await ConnectorXP.CheckRemoteXP(sim.Key) || await ConnectorMSFS.CheckRemoteMsfs(sim.Key)))
                {
                    Logger.Information($"Simulator Binary '{string.Join(" | ", sim.Value)}' detected - finding Connector for Type '{sim.Key}'");
                    ISimConnector simConnector = null;
                    ConnectorFSUIPC secondaryConnector = null;
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
                            if (App.Configuration.UseFsuipcForMSFS && !App.Configuration.MsfsRemoteConnection)
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
                    Task task = Task.Factory.StartNew(async () => await simConnector.Run(), App.CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
                    Logger.Information($"Connector Task for '{sim.Key}' started");

                    if (secondaryConnector != null)
                    {
                        ActiveConnectors.TryAdd(secondaryType, secondaryConnector);
                        task = Task.Factory.StartNew(async () => await secondaryConnector.Run(), App.CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
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
                await connector.Stop();
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

        protected async Task Process()
        {
            if (ProcessIsRunning)
                return;
            ProcessIsRunning = true;

            StatisticManager.StartTrack(StatisticID.SIM_PROCESS);

            if (App.CancellationTokenSource.IsCancellationRequested)
            {
                Logger.Information($"Cancellation received - stopping Process Timer");
                TimerProcess.Stop();
                ProcessIsRunning = false;
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
                        await SubscribeVariables(query);
                    }

                    ConnectorNone.Process();
                    foreach (var connector in readyConnectors)
                        await connector.Process();

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
                            await UnsubscribeVariables(list);
                            await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedResources(false) ?? Task.CompletedTask);
                            await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedResources(false) ?? Task.CompletedTask);
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
            if (TimerProcess.Interval.TotalMilliseconds != App.Configuration.IntervalSimProcess)
            {
                Logger.Debug($"Updating TimerProcess to {App.Configuration.IntervalSimProcess}ms");
                TimerProcess.Interval = TimeSpan.FromMilliseconds(App.Configuration.IntervalSimProcess);
            }
            ProcessIsRunning = false;
        }

        public async Task RemoveUnusedResources(bool force)
        {
            await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedResources(force) ?? Task.CompletedTask);
            await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.RemoveUnusedResources(force) ?? Task.CompletedTask);
        }

        public async Task SubscribeVariable(ManagedVariable managedVariable)
        {
            await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.SubscribeVariable(managedVariable) ?? Task.CompletedTask);
            if (!managedVariable.IsSubscribed)
                await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.SubscribeVariable(managedVariable) ?? Task.CompletedTask);

            if (!managedVariable.IsSubscribed)
                Logger.Error($"Could not subscribe Managed Value '{managedVariable.Address}' on any Connector");
        }

        public async Task SubscribeVariables(List<ManagedVariable> managedVariables)
        {
            var primaryConnector = ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary).FirstOrDefault();
            await (primaryConnector?.SubscribeVariables([.. managedVariables]) ?? Task.CompletedTask);

            var query = ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary);
            if (query.Any())
            {
                managedVariables = [.. managedVariables.Where(v => !v.IsSubscribed && v.Registrations >= 1)];
                await query.First().SubscribeVariables([.. managedVariables]);
            }
        }

        public async Task UnsubscribeVariable(ManagedVariable managedVariable)
        {
            await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary)?.FirstOrDefault()?.UnsubscribeVariable(managedVariable) ?? Task.CompletedTask);
            if (managedVariable.IsSubscribed)
                await (ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary)?.FirstOrDefault()?.UnsubscribeVariable(managedVariable) ?? Task.CompletedTask);

            if (managedVariable.IsSubscribed)
                Logger.Error($"Could not unsubscribe Managed Value '{managedVariable.Address}' on any Connector");
        }

        public async Task UnsubscribeVariables(List<ManagedVariable> managedVariables)
        {
            var primaryConnector = ActiveConnectors.Values.Where(c => c.IsReadyProcess && c.IsPrimary).FirstOrDefault();
            await (primaryConnector?.UnsubscribeVariables([.. managedVariables]) ?? Task.CompletedTask);

            var query = ActiveConnectors.Values.Where(c => c.IsReadyProcess && !c.IsPrimary);
            if (query.Any())
                await query.First().UnsubscribeVariables([.. managedVariables]);
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
                        Logger.Warning($"No active & ready Connector can run the '{command?.Type}' Command '{command?.Address}' (Valid-Check: {command?.IsValid})");
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
