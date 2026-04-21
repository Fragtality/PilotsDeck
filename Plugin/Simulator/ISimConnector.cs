using PilotsDeck.Resources.Variables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator
{
    public enum SimulatorType
    {
        NONE = -1,
        FSX = 0,
        P3D = 1,
        MSFS = 2,
        XP = 3,
        FSUIPC7 = 4
    }

    public enum SimulatorState
    {
        UNKNOWN = -1,
        STOPPED = 0,
        RUNNING = 1,
        LOADING = 2,
        SESSION = 3
    }

    public interface ISimConnector : IDisposable
    {
        public SimulatorType Type { get; }
        public SimulatorState SimState { get; }
        public bool IsPrimary { get; set; }
        public bool IsRunning { get; }
        public bool IsLoading { get; }
        public bool IsReadyProcess { get; }
        public bool IsReadyCommand { get; }
        public bool IsReadySession { get; }
        public bool IsPaused { get; }
        public string AircraftString { get; }

        public static DateTime NextBinaryCheck { get; set; } = DateTime.MinValue;
        protected static List<Process> Processes { get; set; } = [.. System.Diagnostics.Process.GetProcesses()];

        public static void RefreshProcesses()
        {
            Processes = [.. System.Diagnostics.Process.GetProcesses()];
            NextBinaryCheck = DateTime.Now + TimeSpan.FromMilliseconds(App.Configuration.IntervalSimMonitor);
        }

        public static bool GetRunning(SimulatorType type)
        {
            return App.Configuration.SimBinaries.ContainsKey(type) && App.Configuration.SimBinaries[type].Where(b => GetRunning(b)).Any();
        }

        public static bool GetRunning(string binaryName)
        {
            if (NextBinaryCheck < DateTime.Now)
                RefreshProcesses();

            return Processes.Where((p) => p?.ProcessName?.Equals(binaryName, StringComparison.InvariantCultureIgnoreCase) == true).Any();
        }

        public Task Run();
        public Task Stop();
        public Task Process();
        public Task SubscribeVariable(ManagedVariable managedVariable);
        public Task SubscribeVariables(ManagedVariable[] managedVariables);
        public Task UnsubscribeVariable(ManagedVariable managedVariable);
        public Task UnsubscribeVariables(ManagedVariable[] managedVariables);
        public Task RemoveUnusedResources(bool force);
        public bool CanRunCommand(SimCommand command);
        public Task<bool> RunCommand(SimCommand command);
    }
}
