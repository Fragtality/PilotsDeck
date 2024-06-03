using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.IO;

namespace PilotsDeck
{
    public class ManagedGlobalScript(string file) : ManagedScript(file)
    {
        public virtual bool IsActiveGlobal { get; set; } = false;
        public virtual int Interval { get; set; } = 1000;
        public virtual Dictionary<string, string> EventCallbacks { get; private set; } = [];
        public virtual string CallbackFunction { get; set; } = "OnTick";
        public virtual DateTime NextRun { get; set; } = DateTime.Now;
        public virtual string Aircraft { get; set; } = "";

        protected override void Init(string file)
        {
            FileName = file;
            IPCManager = Plugin.ActionController.ipcManager;
            FileSize = new FileInfo(GetScriptFolder(FileName)).Length;
        }

        protected override void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.SimVar = new Func<string, bool>(RegisterVariable);
            _env.SimRead = new Func<string, double>(SimRead);
            _env.SimReadString = new Func<string, string>(SimReadString);
            _env.SimWrite = new Func<string, object, bool>(SimWrite);
            _env.SimCommand = new Func<string, double, bool>(SimCommand);
            _env.SimCalculator = new Func<string, bool>(SimCalculator);
            _env.SharpFormat = new Func<string, object[], string>(SharpFormat);
            _env.SharpFormatLocale = new Func<string, object[], string>(SharpFormatLocale);
            _env.RunInterval = new Action<int, string>(RunInterval);
            _env.RunAircraft = new Action<string>(RunAircraft);
            _env.RunEvent = new Action<string, string>(RunEvent);
            _env.Sleep = new Action<int>(ScriptSleep);
            _env.UseLog = new Action<string>(UseLog);
            _env.Log = new Action<string>(WriteLog);
        }

        public override void Start(long fileSize = -1)
        {
            if (LuaEngine != null)
                return;

            base.Start(fileSize);
            
            SetNextRun();
        }

        private void SetNextRun()
        {
            if (IsRunning)
                NextRun = DateTime.Now.AddMilliseconds(Interval);
        }

        public override void Stop()
        {
            IsActiveGlobal = false;
            base.Stop();
        }

        public virtual void RunGlobal(DateTime now)
        {
            if (!IsRunning)
                return;

            if (now >= NextRun)
            {
                DoChunk($"{CallbackFunction}()");
                SetNextRun();
            }

        }

        public virtual void DoEvent(string evtName, object evtData)
        {
            if (!IsRunning)
                return;

            try
            {
                if (EventCallbacks.TryGetValue(evtName, out var callback))
                {
                    DoChunk($"{callback}({evtData})");
                }
                else
                    Logger.Log(LogLevel.Debug, "ManagedGlobalScript:DoEvent", $"Script '{FileName}' has no Callback for Event '{evtName}'");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedGlobalScript:DoEvent", $"Exception '{ex.GetType()}' while running Callback for Event '{evtName}' ({evtData}) in '{FileName}': {ex.Message}");
            }
        }

        public override void DeregisterAllVariables()
        {
            base.DeregisterAllVariables();

            foreach (var callback in EventCallbacks)
            {
                SimConnector.UnsubscribeSimEvent(callback.Key, FileName);
            }
            EventCallbacks.Clear();
        }

        public override void RegisterAllVariables()
        {
            if (!IsRunning || VariablesRegistered)
                return;

            base.RegisterAllVariables();

            foreach (var callback in EventCallbacks)
            {
                SimConnector.SubscribeSimEvent(callback.Key, FileName, DoEvent);
            }
        }

        #region ScriptFunctions
        protected virtual void RunInterval(int interval, string function = "OnTick")
        {
            Interval = interval;
            if (function == null)
                function = "OnTick";
            CallbackFunction = function;
            NextRun = DateTime.Now.AddMilliseconds(Interval);
            Logger.Log(LogLevel.Debug, "ManagedGlobalScript:RunInterval", $"Interval set to '{Interval}' for Function '{CallbackFunction}' in Script '{FileName}'");
        }

        protected virtual void RunAircraft(string aircraft = "")
        {
            if (aircraft == null)
                aircraft = "";
            Aircraft = aircraft;
            Logger.Log(LogLevel.Debug, "ManagedGlobalScript:RunAircraft", $"Aircraft set to '{Aircraft}' for Script '{FileName}'");
        }

        protected virtual void RunEvent(string evtName, string callback)
        {
            if (!string.IsNullOrWhiteSpace(callback) && !string.IsNullOrWhiteSpace(evtName) && !EventCallbacks.ContainsKey(evtName))
            {
                EventCallbacks.Add(evtName, callback);
                Logger.Log(LogLevel.Debug, "ManagedGlobalScript:RunEvent", $"Added Event '{evtName}' for Script '{FileName}'");
                SimConnector.SubscribeSimEvent(evtName, FileName, DoEvent);
            }
            else
                Logger.Log(LogLevel.Warning, "ManagedGlobalScript:RunEvent", $"Could not add Event '{evtName}' for Script '{FileName}'");
        }
        #endregion
    }
}
