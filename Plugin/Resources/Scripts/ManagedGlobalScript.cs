using CFIT.AppLogger;
using Neo.IronLua;
using PilotsDeck.Simulator;
using System;
using System.Collections.Generic;
using System.IO;

namespace PilotsDeck.Resources.Scripts
{
    public class ManagedGlobalScript(string file, Serilog.Core.Logger log) : ManagedScript(file, log)
    {
        public virtual bool IsActiveGlobal { get; set; } = false;
        public virtual int Interval { get; set; } = 1000;
        public virtual Dictionary<string, string> EventCallbacks { get; private set; } = [];
        public virtual string CallbackFunction { get; set; } = "OnTick";
        public virtual bool CallbacksIntialized { get; protected set; } = false;
        public virtual DateTime NextRun { get; set; } = DateTime.Now;
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
            _env.SimWrite = new Func<string, object, bool>(SimWrite);
            _env.SimCommand = new Func<string, bool>(SimCommand);
            _env.SimCalculator = new Func<string, bool>(SimCalculator);
            _env.SharpFormat = new Func<string, object[], string>(SharpFormat);
            _env.SharpFormatLocale = new Func<string, object[], string>(SharpFormatLocale);
            _env.GetRegistryValue = new Func<string, string, string>(GetRegistryValue);
            _env.RunInterval = new Action<int, string>(RunInterval);
            _env.RunSim = new Action<string>(RunSim);
            _env.RunAircraft = new Action<string>(RunAircraft);
            _env.RunEvent = new Action<string, string>(RunEvent);
            _env.Sleep = new Action<int>(ScriptSleep);
            _env.UseLog = new Action<string>(UseLog);
            _env.Log = new Action<string>(WriteLog);
        }

        public override void Start(FileInfo fileInfo = null, Serilog.Core.Logger log = null)
        {
            if (LuaEngine != null)
                return;

            base.Start(fileInfo, log);
            
            SetNextRun();
        }

        public override void Stop()
        {
            if (IsRunning)
            {
                foreach (var callback in EventCallbacks)
                    SimController.UnsubscribeSimEvent(callback.Key, FileName);
                EventCallbacks.Clear();
            }

            base.Stop();
        }

        private void SetNextRun()
        {
            if (IsRunning)
                NextRun = DateTime.Now.AddMilliseconds(Interval);
        }

        public virtual void RunGlobal(DateTime now)
        {
            if (!IsRunning || !CallbacksIntialized)
                return;

            try
            {
                if (now >= NextRun)
                {
                    DoChunk($"{CallbackFunction}()");
                    SetNextRun();
                }
            }
            catch (LuaRuntimeException ex)
            {
                Log.Fatal(ScriptManager.FormatLogMessage(FileName, $"{ex.GetType()}: {ex.Message}"));
            }
        }

        public virtual void DoEvent(string evtName, object evtData)
        {
            if (!IsActiveGlobal || !IsRunning)
                return;

            try
            {
                if (EventCallbacks.TryGetValue(evtName, out var callback))
                {
                    DoChunk($"{callback}({evtData})");
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
            Interval = interval;
            function ??= "OnTick";
            CallbackFunction = function;
            NextRun = DateTime.Now.AddMilliseconds(Interval);
            Logger.Debug($"Interval set to '{Interval}' for Function '{CallbackFunction}' in Script '{FileName}'");
            CallbacksIntialized = true;
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
            foreach (SimulatorType type in Enum.GetValues(typeof(SimulatorType)))
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
            if (!IsActiveGlobal)
                return;

            if (!string.IsNullOrWhiteSpace(callback) && !string.IsNullOrWhiteSpace(evtName) && !EventCallbacks.ContainsKey(evtName))
            {
                EventCallbacks.Add(evtName, callback);
                Logger.Debug($"Added Event '{evtName}' for Script '{FileName}'");
                SimController.SubscribeSimEvent(evtName, FileName, DoEvent);
            }
            else
                Logger.Warning($"Could not add Event '{evtName}' for Script '{FileName}'");

            CallbacksIntialized = true;
        }

        protected override void UseLog(string name)
        {
            if (!IsActiveGlobal)
                return;

            base.UseLog(name);
        }
        #endregion
    }
}
