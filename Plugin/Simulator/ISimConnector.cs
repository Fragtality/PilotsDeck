using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
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
        public static bool GetRunning(SimulatorType type)
        {
            return App.Configuration.SimBinaries.ContainsKey(type) && App.Configuration.SimBinaries[type].Where(b => Sys.GetProcessRunning(b)).Any();
        }
        public string AircraftString { get; }

        public delegate void EventCallback(string evtName, object evtData);
        public class EventRegistration(string evtName, string receiverID, EventCallback callbackFunction)
        {
            public string Name { get; set; } = evtName;
            public string ReceiverID { get; set; } = receiverID;
            public EventCallback Callback { get; set; } = callbackFunction;
        }
        public Dictionary<string, List<EventRegistration>> RegisteredEvents { get; }

        public void Run();
        public void Stop();
        public void Process();
        public void SubscribeVariable(ManagedVariable managedVariable);
        public void SubscribeVariables(ManagedVariable[] managedVariables);
        public void UnsubscribeVariable(ManagedVariable managedVariable);
        public void UnsubscribeVariables(ManagedVariable[] managedVariables);
        public void RemoveUnusedVariables();
        public bool SubscribeSimEvent(string evtName, string receiverID, EventCallback callbackFunction);
        public bool UnsubscribeSimEvent(string evtName, string receiverID);
        public bool CanRunCommand(SimCommand command);
        public Task<bool> RunCommand(SimCommand command);
    }
}
