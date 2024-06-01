using Neo.IronLua;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PilotsDeck
{
    public class ManagedScript
    {
        public Lua LuaEngine { get; set; } = null;
        public bool IsRunning { get { return LuaEngine != null; } }
        public LuaGlobal LuaEnv { get; set; }
        public LuaChunk LuaChunk { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public List<string> Variables { get; set; } = [];
        public uint Registrations { get; set; } = 1;
        public bool IsGlobal { get; set; }
        public bool IsActiveGlobal { get; set; } = false;
        public int Interval { get; set; } = 1000;
        public string CallbackFunction { get; set; } = "OnTick";
        public DateTime NextRun { get; set; } = DateTime.Now;
        public string Aircraft { get; set; } = "";
        private IPCManager IPCManager { get; set; }
        private SimulatorConnector SimConnector { get { return IPCManager.SimConnector; } }

        public ManagedScript(string file, IPCManager manager, bool isGlobal = false)
        {
            FileName = file;
            IPCManager = manager;
            IsGlobal = isGlobal;

            Start();
        }

        public void DoChunk()
        {
            LuaEnv.DoChunk(LuaChunk);
        }

        public void DoChunk(string code)
        {
            LuaEnv.DoChunk(code, FileName);
        }

        public LuaResult DoChunkWithResult(string code)
        {
            return LuaEnv.DoChunk(code, FileName);
        }

        private string GetScriptFolder(string file = "")
        {
            if (IsGlobal)
                return ScriptManager.GlobalScriptFolder + file;
            else
                return ScriptManager.ScriptFolder + file;
        }

        public void Start()
        {
            FileSize = new FileInfo(GetScriptFolder(FileName)).Length;

            LuaEngine = new();
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.SimVar = new Func<string, bool>(RegisterVariable);
            _env.SimRead = new Func<string, double>(SimRead);
            _env.SimReadString = new Func<string, string>(SimReadString);
            _env.SimWrite = new Func<string, double, bool>(SimWrite);
            _env.SimWriteString = new Func<string, string, bool>(SimWriteString);
            _env.SimCommand = new Func<string, double, bool>(SimCommand);
            _env.SimCalculator = new Func<string, bool>(SimCalculator);
            _env.SharpFormat = new Func<string, object[], string>(SharpFormat);
            _env.SharpFormatLocale = new Func<string, object[], string>(SharpFormatLocale);
            _env.RunInterval = new Action<int, string>(RunInterval);
            _env.RunAircraft = new Action<string>(RunAircraft);
            _env.Sleep = new Action<int>(ScriptSleep);

            string code = File.ReadAllText(GetScriptFolder(FileName));
            LuaChunk = LuaEngine.CompileChunk(code, FileName, new LuaCompileOptions());
            DoChunk();

            SetNextRun();

            Logger.Log(LogLevel.Debug, "ManagedScript:Start", $"Script started: {FileName}");
        }

        private void SetNextRun()
        {
            if (IsGlobal)
                NextRun = DateTime.Now.AddMilliseconds(Interval);
        }

        public void Stop()
        {
            DeregisterAllVariables();

            LuaChunk = null;
            
            IsActiveGlobal = false;

            if (LuaEnv != null)
            {
                LuaEnv.Clear();
                LuaEnv = null;
            }

            if (LuaEngine != null)
            {
                LuaEngine.Clear();
                LuaEngine.Dispose();
                LuaEngine = null;
            }

            Logger.Log(LogLevel.Debug, "ManagedScript:Stop", $"Script stopped: {FileName}");
        }

        public void Run(DateTime now)
        {
            if (!IsGlobal || !IsRunning)
                return;

            if (now >= NextRun)
            {
                DoChunk($"{CallbackFunction}()");
                SetNextRun();
            }

        }

        public void Reload()
        {
            Stop();
            Start();
        }

        public bool FileHasChanged()
        {
            return new FileInfo(GetScriptFolder(FileName)).Length != FileSize;
        }

        public void DeregisterAllVariables()
        {
            if (!IsRunning)
                return;

            foreach (var variable in Variables)
            {
                IPCManager.DeregisterAddress(variable);
            }
        }

        public void RegisterAllVariables()
        {
            if (!IsRunning)
                return;

            foreach (var variable in Variables)
            {
                IPCManager.RegisterAddress(variable);
            }
        }

        #region ScriptFunctions
        private bool RegisterVariable(string name)
        {
            ActionSwitchType type = IPCTools.GetReadType(name, ActionSwitchType.MACRO);

            if (IPCTools.IsActionReadable(type) && IPCTools.IsReadAddressForType(name, type))
            {
                if (IPCManager.RegisterAddress(name) != null)
                {
                    if (!Variables.Contains(name))
                        Variables.Add(name);
                    Logger.Log(LogLevel.Debug, "ScriptManager:RegisterVariable", $"Registered Variable '{name}' for Script File '{FileName}'");
                    return true;
                }
                else
                    Logger.Log(LogLevel.Error, "ScriptManager:RegisterVariable", $"Could not register Variable '{name}' for Script File '{FileName}' (Type {type})");
            }

            return false;
        }

        private double SimRead(string name)
        {
            IPCValue value = IPCManager[name];
            if (value == null)
                return 0;
            else
            {
                try
                {
                    return value.RawValue();
                }
                catch
                {
                    return 0;
                }
            }
        }

        private string SimReadString(string name)
        {
            IPCValue value = IPCManager[name];
            if (value == null)
                return "";
            else
            {
                try
                {
                    return value.Value;
                }
                catch
                {
                    return "";
                }
            }
        }

        private static string ConvertValue(double value)
        {
            string num = Convert.ToString(value, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                return num;
            else
                return string.Format("{0:F1}", value);
        }

        private string SharpFormat(string pattern, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, pattern, values);
        }

        private string SharpFormatLocale(string pattern, params object[] values)
        {
            return string.Format(CultureInfo.CurrentUICulture.NumberFormat, pattern, values);
        }

        private bool SimWrite(string name, double value)
        {
            return SimWriteString(name, ConvertValue(value));
        }

        private bool SimWriteString(string name, string value)
        {
            ActionSwitchType actionType = IPCTools.GetReadType(name, ActionSwitchType.MACRO);
            if (actionType == ActionSwitchType.MACRO)
                return false;
            else
                return SimConnector.RunAction(name, actionType, value, new ModelSwitch());
        }

        private bool SimCommand(string name, double value = 0)
        {
            ActionSwitchType actionType = IPCTools.GetCommandType(name, ActionSwitchType.LVAR);
            if (actionType == ActionSwitchType.LVAR)
                return false;
            else
                return SimConnector.RunAction(name, actionType, ConvertValue(value), new ModelSwitch());
        }

        private bool SimCalculator(string code)
        {
            if (SimConnector is ConnectorMSFS)
                return (SimConnector as ConnectorMSFS).RunAction(code, ActionSwitchType.CALCULATOR, "0", new ModelSwitch());

            return false;
        }

        private void RunInterval(int interval, string function = "OnTick")
        {
            Interval = interval;
            if (function == null)
                function = "OnTick";
            CallbackFunction = function;
        }

        private void RunAircraft(string aircraft = "")
        {
            if (aircraft == null)
                aircraft = "";
            Aircraft = aircraft;
        }

        private void ScriptSleep(int msec)
        {
            Thread.Sleep(msec);
        }
        #endregion
    }
}
