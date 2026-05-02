using CFIT.AppLogger;
using Neo.IronLua;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PilotsDeck.Resources.Scripts
{
    public class TimerCallback
    {
        public TimerCallback() { }
        public int Interval { get; set; } = 1000;
        public string CallbackFunction { get; set; } = "OnTick";
        public DateTime NextRun { get; set; } = DateTime.Now;

        public void SetNextRun(DateTime now)
        {
            NextRun = now.AddMilliseconds(Interval);
        }
    }

    public class EventCallback(ManagedAddress evtAddress, string function, Func<object, Task> callbackHandler, bool onChanged)
    {
        public ManagedAddress Address { get; set; } = evtAddress;
        public string Function { get; set; } = function;
        public Func<object, Task> CallbackHandler { get; set; } = callbackHandler;
        public bool OnChanged { get; set; } = onChanged;

        public void Subscribe(ManagedVariable evtVariable)
        {
            if (OnChanged)
                evtVariable?.ValueChanged += CallbackHandler;
            else
                evtVariable?.ValueSet += CallbackHandler;
        }

        public void Unsubscribe(ManagedVariable evtVariable)
        {
            if (OnChanged)
                evtVariable?.ValueChanged -= CallbackHandler;
            else
                evtVariable?.ValueSet -= CallbackHandler;
        }
    }

    public class ManagedGlobalScript(string file, Serilog.Core.Logger log) : ManagedScript(file, log)
    {
        public virtual bool IsActiveGlobal { get; set; } = false;
        public virtual Dictionary<string, EventCallback> EventCallbacks { get; private set; } = [];
        public virtual Dictionary<string, TimerCallback> TimerCallbacks { get; private set; } = [];
        public virtual string Aircraft { get; set; } = "";
        public virtual SimulatorType SimulatorType { get; set; } = SimulatorType.NONE;

        protected override void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.GetAircraft = App.SimController.AircraftString;
            _env.SimVar = new Func<string, bool>(RegisterVariable);
            _env.SimRead = new Func<string, dynamic>(SimRead);
            _env.SimReadString = new Func<string, string>(SimReadString);
            _env.SimWrite = new Func<string, dynamic, bool>(SimWrite);
            _env.SimCommand = new Func<string, bool>(SimCommand);
            _env.JoystickCommand = new Func<string, string, bool>(JoystickCommand);
            _env.SimCalculator = new Func<string, bool>(SimCalculator);
            _env.SharpFormat = new Func<string, object[], string>(SharpFormat);
            _env.SharpFormatLocale = new Func<string, object[], string>(SharpFormatLocale);
            _env.GetRegistryValue = new Func<string, string, string>(GetRegistryValue);
            _env.HttpGet = new Func<string, string>(DoHttpGet);
            _env.Serialize = new Func<object, string>(JsonSerialize);
            _env.Deserialize = new Func<string, JsonNode>(JsonDeserialize);
            _env.RunInterval = new Action<int, string>(RunInterval);
            _env.RunSim = new Action<string>(RunSim);
            _env.RunAircraft = new Action<string>(RunAircraft);
            _env.RunEvent = new Action<string, string>(RunEvent);
            _env.RunEventChanged = new Action<string, string>(RunEventChanged);
            _env.Sleep = new Action<int>(ScriptSleep);
            _env.UseLog = new Action<string>(UseLog);
            _env.Log = new Action<string>(WriteLog);
        }

        public override void Start(FileInfo fileInfo = null)
        {
            if (LuaEngine != null)
                return;

            base.Start(fileInfo);
        }

        public override void Stop()
        {
            foreach (var callback in EventCallbacks)
                RemoveCallback(callback.Value);
            EventCallbacks.Clear();
            TimerCallbacks.Clear();

            base.Stop();
        }

        public virtual void RunGlobal(DateTime now)
        {
            if (!IsRunning || TimerCallbacks.Count == 0)
                return;

            try
            {
                foreach (var callback in TimerCallbacks)
                {
                    if (now >= callback.Value.NextRun)
                    {
                        DoChunk($"{callback.Value.CallbackFunction}()");
                        callback.Value.SetNextRun(now);
                    }
                }
            }
            catch (LuaRuntimeException ex)
            {
                Log.Fatal(ScriptManager.FormatLogMessage(FileName, $"{ex.GetType()}: {ex.Message}"));
            }
        }

        #region ScriptFunctions
        protected override bool RegisterVariable(string name)
        {
            if (IsActiveGlobal)
                return base.RegisterVariable(name);
            else
            {
                Logger.Verbose($"SimVar Request from Script File '{FileName}' while not active (Variable: '{name}')");
                return false;
            }
        }

        protected virtual void RunInterval(int interval, string function = "OnTick")
        {
            function ??= "OnTick";
            if (TimerCallbacks.ContainsKey(function))
            {
                Logger.Warning($"The Script '{FileName}' already registered a Callback for '{function}'");
                return;
            }

            var callback = new TimerCallback()
            {
                CallbackFunction = function,
                Interval = interval
            };

            TimerCallbacks.Add(function, callback);
            Logger.Debug($"Interval set to '{interval}' for Function '{function}' in Script '{FileName}'");
        }

        protected virtual void RunAircraft(string aircraft = "")
        {
            aircraft ??= "";
            Aircraft = aircraft;
            Logger.Debug($"Aircraft set to '{Aircraft}' for Script '{FileName}'");
        }

        protected virtual void RunSim(string sim = "NONE")
        {
            bool found = false;
            foreach (SimulatorType type in Enum.GetValues<SimulatorType>())
            {
                if (type.ToString().ToLowerInvariant() == sim.ToLowerInvariant())
                {
                    SimulatorType = type;
                    found = true;
                    break;
                }
            }
            if (!found)
                SimulatorType = SimulatorType.NONE;

            Logger.Debug($"Simulator set to '{SimulatorType}' for Script '{FileName}'");
        }

        protected virtual void RunEvent(string evtName, string callback)
        {
            HandleEvent(evtName, callback, false);
        }

        protected virtual void RunEventChanged(string evtName, string callback)
        {
            HandleEvent(evtName, callback, true);
        }

        protected virtual void HandleEvent(string evtName, string callback, bool onChanged)
        {
            if (!IsActiveGlobal || string.IsNullOrWhiteSpace(evtName) || string.IsNullOrWhiteSpace(callback))
                return;

            if (!string.IsNullOrWhiteSpace(evtName) && evtName.Length > 2 && !evtName.StartsWith('(') && evtName[1] != ':')
            {
                evtName = evtName.Insert(0, "K:");
            }

            var evtAddress = new ManagedAddress(evtName);
            if (evtAddress.ReadType == SimValueType.NONE)
            {
                Logger.Warning($"Failed to parse Address for Event '{evtName}' on Script '{FileName}'");
                return;
            }

            if (EventCallbacks.ContainsKey(evtAddress.Address))
            {
                Logger.Warning($"Script '{FileName}' already registered Event '{evtAddress.Address}'");
                return;
            }

            var evtVariable = VariableManager.RegisterVariable(evtAddress);
            if (evtVariable == null)
            {
                Logger.Warning($"Failed to register Variable for Event '{evtAddress.Address}' on Script '{FileName}'");
                return;
            }

            var eventRegistration = new EventCallback(evtAddress, callback, evtData => DoEvent(evtAddress.Address, evtData), onChanged);
            eventRegistration.Subscribe(evtVariable);
            EventCallbacks.TryAdd(evtAddress.Address, eventRegistration);
            Logger.Debug($"Added Event '{evtAddress.Address}' for Script '{FileName}'");
        }

        protected virtual void RemoveCallback(EventCallback eventRegistration)
        {
            eventRegistration.Unsubscribe(VariableManager.DeregisterVariable(eventRegistration.Address));
            Logger.Debug($"Removed Event '{eventRegistration.Address.Address}' for Script '{FileName}'");
        }

        public virtual Task DoEvent(string evtName, object evtData)
        {
            if (!IsActiveGlobal || !IsRunning)
                return Task.CompletedTask;

            try
            {
                if (EventCallbacks.TryGetValue(evtName, out var callback))
                {
                    if (evtData is string stringData)
                        DoChunk($"{callback.Function}('{stringData}')");
                    else
                        DoChunk($"{callback.Function}({evtData})");
                }
                else
                    Logger.Debug($"Script '{FileName}' has no Callback for Event '{evtName}'");
            }
            catch (LuaRuntimeException ex)
            {
                Log.Fatal(ScriptManager.FormatLogMessage(FileName, $"{ex.GetType()}: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return Task.CompletedTask;
        }
        #endregion
    }
}
