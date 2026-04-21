using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.XP;
using PilotsDeck.Simulator.XP.UDP;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public class ConnectorXP : ISimConnector
    {
        public SimulatorType Type { get; } = SimulatorType.XP;
        public SimulatorState SimState { get; protected set; } = SimulatorState.RUNNING;
        public bool IsPrimary { get; set; } = true;
        public bool IsRunning { get { return ISimConnector.GetRunning(Type) || RemoteRunning; } }
        public bool IsLoading => ConnectionUDP.IsLoading;
        public bool IsReadyProcess { get { return ConnectionUDP.ReceivedAircraftString && IsRunning && !IsLoading && !ConnectionUDP.SocketError; } }
        public bool IsReadyCommand { get { return IsReadyProcess && !ConnectionUDP.SocketError; } }
        public bool IsReadySession { get { return !IsLoading && !ConnectionUDP.SocketError; } }
        public bool IsPaused => ConnectionUDP.IsPaused;
        public string AircraftString => ConnectionUDP.AircraftString;
        public bool RemoteRunning { get; set; } = false;
        public int SimVersion => ConnectionUDP.SimVersion;
        protected ConnectionManagerUDP ConnectionUDP { get; }
        protected ConnectionManagerREST ConnectionREST { get; }
        protected Task RestTask { get; set; } = null;
        public DispatcherTimerAsync TimerCheckLoading { get; } = new();
        public bool UseWebAPI => App.Configuration.XPlaneUseWebApi && SimVersion >= ConnectionManagerREST.VERSIONWEBAPI;
        public bool WebApiCmdOnly => UseWebAPI && App.Configuration.XPlaneUseWebApi;
        protected virtual DateTime LastKeepAlive { get; set; } = DateTime.Now;

        public ConnectorXP()
        {
            ConnectionUDP = new(this);
            ConnectionREST = new(this);
        }

        public static async Task<bool> CheckRemoteXP(SimulatorType type)
        {
            if (type != SimulatorType.XP || App.Configuration.XPlaneIP == "127.0.0.1")
                return false;

            bool result = false;
            using UdpClient udpSend = new();
            try
            {
                Logger.Verbose($"XP Remote check");
                udpSend.Connect(new IPEndPoint(App.Configuration.ParsedXPlaneIP, App.Configuration.XPlanePort));
                var ipEndPoint = (IPEndPoint)udpSend.Client.LocalEndPoint;
                using UdpClient udpReceive = new(ipEndPoint);

                var sendResult = await udpSend.SendAsync(DatagramXP.MessageRequest(ConnectionManagerUDP.TIMEPAUSED_REF, ConnectionManagerUDP.TIMEPAUSED_INDEX, (int)(1000 / (App.Configuration.IntervalSimProcess))));
                Logger.Verbose($"XP Remote check: send RREF");

                var cts = new CancellationTokenSource(App.Configuration.XPlaneRemoteCheckTimeout);
                Logger.Verbose($"XP Remote check: receive Answer");
                var response = await udpReceive.ReceiveAsync(cts.Token);
                Logger.Verbose($"XP Remote check: Answer received");
                var header = DatagramXP.GetHeader(response.Buffer);
                if (header == DatagramXP.XP_STR_RREF)
                {
                    Logger.Verbose($"XP Remote check: Header is RREF");
                    result = true;
                }
                else
                    Logger.Verbose($"XP Remote check: Header is Unkown");

                udpReceive?.Close();
                udpReceive?.Dispose();
            }
            catch { }
            finally
            {
                udpSend?.Close();
                udpSend?.Dispose();
            }

            return result;
        }

        public async Task Run()
        {
            try
            {
                TimerCheckLoading.Interval = TimeSpan.FromMilliseconds(App.Configuration.XPlaneStateCheckInterval);
                TimerCheckLoading.Tick += CheckLoadingTask;

                if (App.Configuration.XPlaneIP != "127.0.0.1")
                    RemoteRunning = true;

                await ConnectionUDP.Run();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException && ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            TimerCheckLoading?.Stop();
            Logger.Information($"ConnectorXP Task ended");
        }

        protected async Task CheckLoadingTask()
        {
            if (!IsRunning || App.CancellationTokenSource.IsCancellationRequested || ConnectionUDP.ReceiveTokenSource.IsCancellationRequested)
            {
                TimerCheckLoading.Stop();
                if (App.CancellationTokenSource.IsCancellationRequested)
                    ConnectionUDP.ReceiveTokenSource.Cancel();
                return;
            }

            ConnectionUDP.CheckLoading();

            if (SimState == SimulatorState.RUNNING && ConnectionUDP.IsConnected && ConnectionUDP.RefsInternalSubscribed)
            {
                Logger.Debug("Changed to LOADING");
                SimState = SimulatorState.LOADING;
            }

            if (SimState == SimulatorState.LOADING && !IsLoading && !string.IsNullOrWhiteSpace(AircraftString) && !IsPaused
                && (!UseWebAPI || (UseWebAPI && ConnectionREST.HasEnumeratedRefs)))
            {
                Logger.Debug("Changed to SESSION");
                SimState = SimulatorState.SESSION;
            }

            if (SimState == SimulatorState.SESSION && IsLoading && string.IsNullOrWhiteSpace(AircraftString))
            {
                Logger.Debug("Changed to LOADING");
                SimState = SimulatorState.LOADING;
                if (UseWebAPI)
                    await ConnectionREST.OnSessionLeave();
            }

            if (SimState >= SimulatorState.LOADING && (!IsRunning && App.Configuration.XPlaneIP == "127.0.0.1") || (!RemoteRunning && App.Configuration.XPlaneIP != "127.0.0.1"))
            {
                Logger.Debug("Changed to STOPPED");
                SimState = SimulatorState.STOPPED;
            }

            if (UseWebAPI && SimState >= SimulatorState.LOADING && RestTask == null)
            {
                Logger.Debug($"Starting X-Plane WebAPI");
                RestTask = Task.Factory.StartNew(async () => await ConnectionREST.Run(), App.CancellationToken, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
            }

            if (UseWebAPI && SimState >= SimulatorState.LOADING && ConnectionREST.IsConnected && !ConnectionUDP.IsLoading)
            {
                if (!ConnectionREST.HasEnumeratedRefs)
                    await ConnectionREST.OnSessionEnter();

                if (DateTime.Now - LastKeepAlive > TimeSpan.FromMilliseconds(App.Configuration.XPlaneWebApiKeepAlive))
                {
                    LastKeepAlive = DateTime.Now;
                    await ConnectionREST.WebSocket.CheckConnected();
                }
            }
        }

        public void UnsubscribeAllRefs()
        {
            if (!UseWebAPI || WebApiCmdOnly)
            {
                foreach (var variable in App.PluginController.VariableManager.VariableList.Where(v => v.IsValueXP()))
                    variable.IsSubscribed = false;
            }
            else
                _ = ConnectionREST.UnsubscribeAllRefs();
        }

        public Task Stop()
        {
            UnsubscribeAllRefs();

            ConnectionUDP.Stop();
            if (UseWebAPI)
                return ConnectionREST?.Stop();
            else
                return Task.CompletedTask;
        }

        public Task Process()
        {
            if (UseWebAPI && SimState == SimulatorState.SESSION)
                return ConnectionREST.WebSocket.CheckRefCounts();
            else
                return Task.CompletedTask;
        }

        public Task SubscribeVariable(ManagedVariable managedVariable)
        {
            if (!UseWebAPI || WebApiCmdOnly)
            {
                ConnectionUDP.SubscribeVariable(managedVariable);
                return Task.CompletedTask;
            }
            else
                return ConnectionREST.SubscribeVariables([managedVariable]);
        }

        public Task SubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!UseWebAPI || WebApiCmdOnly)
            {
                ConnectionUDP.SubscribeVariables(managedVariables);
                return Task.CompletedTask;
            }
            else
                return ConnectionREST.SubscribeVariables(managedVariables);
        }

        public virtual Task UnsubscribeVariable(ManagedVariable managedVariable)
        {
            if (!UseWebAPI || WebApiCmdOnly)
            {
                ConnectionUDP.UnsubscribeVariable(managedVariable);
                return Task.CompletedTask;
            }
            else
                return ConnectionREST.UnsubscribeVariables([managedVariable]);
        }

        public Task UnsubscribeVariables(ManagedVariable[] managedVariables)
        {
            if (!UseWebAPI || WebApiCmdOnly)
            {
                ConnectionUDP.UnsubscribeVariables(managedVariables);
                return Task.CompletedTask;
            }
            else
                return ConnectionREST.UnsubscribeVariables(managedVariables);
        }

        public Task RemoveUnusedResources(bool force)
        {
            return Task.CompletedTask;
        }

        public static bool IsCommandXP(SimCommandType? type)
        {
            return type == SimCommandType.XPCMD || type == SimCommandType.XPWREF;
        }

        public bool CanRunCommand(SimCommand command)
        {
            return IsCommandXP(command?.Type);
        }

        public async Task<bool> RunCommand(SimCommand command)
        {
            if (!IsReadyCommand)
                return false;

            bool result = false;

            if (command.Type == SimCommandType.XPCMD)
            {
                if (!UseWebAPI)
                    result = await ConnectionUDP.SendCommand(command);
                else
                    result = await ConnectionREST.SendCommand(command);

            }
            else if (command.Type == SimCommandType.XPWREF)
            {
                if (!UseWebAPI || WebApiCmdOnly)
                    result = await ConnectionUDP.WriteRef(command);
                else
                    result = await ConnectionREST.WriteRef(command);
            }

            return result;
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ConnectionUDP.Dispose();
                    ConnectionREST.Dispose();
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
