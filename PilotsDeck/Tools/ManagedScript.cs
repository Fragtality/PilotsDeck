using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

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
        private IPCManager IPCManager { get; set; }
        private SimulatorConnector SimConnector { get { return IPCManager.SimConnector; } }

        public ManagedScript(string file, IPCManager manager)
        {
            FileName = file;
            IPCManager = manager;

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

        public void Start()
        {
            FileSize = new FileInfo(ScriptManager.ScriptFolder + FileName).Length;

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

            string code = File.ReadAllText(ScriptManager.ScriptFolder + FileName);
            LuaChunk = LuaEngine.CompileChunk(code, FileName, new LuaCompileOptions());
            DoChunk();
        }

        public void Stop()
        {
            DeregisterAllVariables();

            LuaChunk = null;

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
        }

        public void Reload()
        {
            Stop();
            Start();
        }

        public bool FileHasChanged()
        {
            return new FileInfo(ScriptManager.ScriptFolder + FileName).Length != FileSize;
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
        #endregion
    }
}
