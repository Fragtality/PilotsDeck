﻿using Neo.IronLua;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace PilotsDeck
{
    public class ManagedScript
    {
        public virtual Lua LuaEngine { get; set; } = null;
        public virtual bool IsRunning { get { return LuaEngine != null; } }
        public virtual LuaGlobal LuaEnv { get; set; }
        public virtual LuaChunk LuaChunk { get; set; }
        public virtual string FileName { get; set; }
        public virtual long FileSize { get; set; }
        public virtual Dictionary<string, bool> Variables { get; set; } = [];
        public virtual uint Registrations { get; set; } = 1;
        public virtual bool LogUseDefault { get; set; } = true;
        protected virtual Serilog.Core.Logger Log { get; set; }
        protected virtual IPCManager IPCManager { get; set; }
        protected virtual SimulatorConnector SimConnector { get { return IPCManager.SimConnector; } }

        public ManagedScript(string file, Serilog.Core.Logger log)
        {
            Log = log;
            Init(file);
        }

        protected virtual void Init(string file)
        {
            FileName = file;
            IPCManager = Plugin.ActionController.ipcManager;
            Start(new FileInfo(GetScriptFolder(FileName)).Length);
        }

        public virtual void DoChunk()
        {
            if (IsRunning)
                LuaEnv.DoChunk(LuaChunk);
            else
                Logger.Log(LogLevel.Debug, "ManagedScript:DoChunk", $"DoChunk while Script '{FileName}' is not running");
        }

        public virtual void DoChunk(string code)
        {
            if (IsRunning)
                LuaEnv.DoChunk(code, FileName);
            else
                Logger.Log(LogLevel.Debug, "ManagedScript:DoChunk", $"DoChunk while Script '{FileName}' is not running. Code: {code}");
        }

        public virtual LuaResult DoChunkWithResult(string code)
        {
            if (IsRunning)
                return LuaEnv.DoChunk(code, FileName);
            else
            {
                Logger.Log(LogLevel.Debug, "ManagedScript:DoChunk", $"DoChunk while Script '{FileName}' is not running. Code: {code}");
                return new LuaResult();
            }
        }

        protected virtual string GetScriptFolder(string file = "")
        {
            if (this is ManagedGlobalScript)
                return ScriptManager.GlobalScriptFolder + file;
            else if (this is ManagedImageScript)
                return ScriptManager.ImageScriptFolder + file;
            else
                return ScriptManager.ScriptFolder + file;
        }

        protected virtual void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.SimVar = new Func<string, bool>(RegisterVariable);
            _env.SimRead = new Func<string, dynamic>(SimRead);
            _env.SimReadString = new Func<string, string>(SimReadString);
            _env.SimWrite = new Func<string, dynamic, bool>(SimWrite);
            _env.SimCommand = new Func<string, double, bool>(SimCommand);
            _env.SimCalculator = new Func<string, bool>(SimCalculator);
            _env.SharpFormat = new Func<string, object[], string>(SharpFormat);
            _env.SharpFormatLocale = new Func<string, object[], string>(SharpFormatLocale);
            _env.Sleep = new Action<int>(ScriptSleep);
            _env.UseLog = new Action<string>(UseLog);
            _env.Log = new Action<string>(WriteLog);
        }

        public virtual void Start(long fileSize = -1, Serilog.Core.Logger log = null)
        {
            try
            {
                if (LuaEngine != null)
                    return;

                if (fileSize == -1)
                    FileSize = new FileInfo(GetScriptFolder(FileName)).Length;
                else
                    FileSize = fileSize;

                if (log != null && (LogUseDefault || Log == null))
                    Log = log;

                LuaEngine = new();
                CreateEnvironment();

                string code = File.ReadAllText(GetScriptFolder(FileName));
                LuaChunk = LuaEngine.CompileChunk(code, FileName, new LuaCompileOptions() { ClrEnabled = false });
                DoChunk();

                Log?.Information(ScriptManager.FormatLogMessage(FileName, $"Script started by ScriptManager"));
                Logger.Log(LogLevel.Debug, "ManagedScript:Start", $"Script started: {FileName}");

                CheckVariables();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedScript:Start", $"Exception '{ex.GetType()}' while starting Script '{FileName}': {ex.Message}");
            }
        }

        public virtual void Stop()
        {
            DeregisterAllVariables();

            LuaChunk?.Lua?.Clear();
            LuaChunk = null;

            if (LuaEnv != null)
            {
                LuaEnv?.Clear();
                LuaEnv = null;
            }

            if (LuaEngine != null)
            {
                LuaEngine.Clear();
                LuaEngine.Dispose();
                LuaEngine = null;
            }

            Log?.Information(ScriptManager.FormatLogMessage(FileName, $"Script stopped by ScriptManager"));
            Logger.Log(LogLevel.Debug, "ManagedScript:Stop", $"Script stopped: {FileName}");
        }

        public virtual void Reload()
        {
            Stop();
            Start();
        }

        public virtual bool FileHasChanged()
        {
            return new FileInfo(GetScriptFolder(FileName)).Length != FileSize;
        }

        public virtual void DeregisterAllVariables()
        {
            Logger.Log(LogLevel.Verbose, "ManagedScript:DeregisterAllVariables", $"Deregistering Variables and Events for Script '{FileName}'");
            foreach (var variable in Variables)
            {
                IPCManager.DeregisterAddress(variable.Key);
                Variables[variable.Key] = false;
            }
        }

        public virtual void CheckVariables()
        {
            if (!IsRunning)
                return;

            foreach (var variable in Variables)
            {
                if (!variable.Value)
                    Variables[variable.Key] = IPCManager.RegisterAddress(variable.Key) != null;
            }
        }

        #region ScriptFunctions
        protected virtual bool RegisterVariable(string name)
        {
            Logger.Log(LogLevel.Verbose, "ManagedScript:RegisterVariable", $"SimVar Request from Script File '{FileName}' for Variable '{name}'");
            ActionSwitchType type = IPCTools.GetReadType(name, ActionSwitchType.MACRO);

            if (IPCTools.IsActionReadable(type) && IPCTools.IsReadAddressForType(name, type))
            {
                if (Variables.ContainsKey(name))
                {
                    if (Variables[name])
                        Logger.Log(LogLevel.Warning, "ManagedScript:RegisterVariable", $"Variable '{name}' is already registered for Script File '{FileName}'");
                    else
                        Variables[name] = IPCManager.RegisterAddress(name) != null;
                }
                else if (IPCManager.RegisterAddress(name) != null)
                {
                    Variables.Add(name, true);
                    Logger.Log(LogLevel.Debug, "ManagedScript:RegisterVariable", $"Registered Variable '{name}' for Script File '{FileName}'");
                    return true;
                }
                else
                {
                    Variables.Add(name, false);
                    Logger.Log(LogLevel.Error, "ManagedScript:RegisterVariable", $"Could not register Variable '{name}' for Script File '{FileName}' (Type {type})");
                }
            }

            return false;
        }

        protected virtual dynamic SimRead(string name)
        {
            IPCValue value = IPCManager[name];
            if (value == null)
            {
                Log?.Warning(ScriptManager.FormatLogMessage(FileName, $"The requested Variable '{name}' is not registered!"));
                return 0;
            }
            else
            {
                try
                {
                    return value.RawValue();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ScriptManager.FormatLogMessage(FileName, $"Exception '{ex.GetType()}' on SimRead for '{name}': {ex.Message}"));
                    return 0;
                }
            }
        }

        protected virtual string SimReadString(string name)
        {
            IPCValue value = IPCManager[name];
            if (value == null)
            {
                Log?.Warning(ScriptManager.FormatLogMessage(FileName, $"The requested Variable '{name}' is not registered!"));
                return "";
            }
            else
            {
                try
                {
                    return value.Value;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ScriptManager.FormatLogMessage(FileName, $"Exception '{ex.GetType()}' on SimReadString for '{name}': {ex.Message}"));
                    return "";
                }
            }
        }

        protected static string ConvertValue(double value)
        {
            string num = Convert.ToString(value, CultureInfo.InvariantCulture.NumberFormat);

            int idxE = num.IndexOf('E');
            if (idxE < 0)
                return num;
            else
                return string.Format("{0:F1}", value);
        }

        protected virtual string SharpFormat(string pattern, params object[] values)
        {
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, pattern, values);
        }

        protected virtual string SharpFormatLocale(string pattern, params object[] values)
        {
            return string.Format(CultureInfo.CurrentUICulture.NumberFormat, pattern, values);
        }

        protected static bool IsNumberType(object value, out double number)
        {
            number = 0;
            try
            {
                if (value is double || value is float || value is int || value is uint || value is long || value is short || value is ushort || value is ulong)
                {
                    number = Convert.ToDouble(value);
                    return true;
                }
                else
                    return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        protected virtual bool SimWrite(string name, dynamic value)
        {
            if (value == null)
                return SimWrite(name, "");
            else if (IsNumberType(value, out double numValue))
                return SimWrite(name, numValue);
            else if (value is string)
                return SimWrite(name, value as string);
            else
                return SimWrite(name, value.ToString());
        }

        protected virtual bool SimWrite(string name, double value)
        {
            return SimWrite(name, ConvertValue(value));
        }

        protected virtual bool SimWrite(string name, string value)
        {
            ActionSwitchType actionType = IPCTools.GetReadType(name, ActionSwitchType.MACRO);
            if (actionType == ActionSwitchType.MACRO)
                return false;
            else if (actionType == ActionSwitchType.INTERNAL && IPCManager.TryGetValue(name, out IPCValue ipcValue))
            {
                ipcValue.SetValue(value);
                return true;
            }
            else
                return SimConnector.RunAction(name, actionType, value, new ModelSwitch());
        }

        protected virtual bool SimCommand(string name, double value = 0)
        {
            ActionSwitchType actionType = IPCTools.GetCommandType(name, ActionSwitchType.LVAR);
            if (actionType == ActionSwitchType.LVAR)
                return false;
            else
                return SimConnector.RunAction(name, actionType, ConvertValue(value), new ModelSwitch());
        }

        protected virtual bool SimCalculator(string code)
        {
            if (SimConnector is ConnectorMSFS)
                return (SimConnector as ConnectorMSFS).RunAction(code, ActionSwitchType.CALCULATOR, "0", new ModelSwitch());

            return false;
        }

        protected virtual void ScriptSleep(int msec)
        {
            Thread.Sleep(msec);
        }

        public static Serilog.Core.Logger CreaterLogger(ref string name, string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                name = name.Trim().ToLower();
                if (!name.Contains(".log"))
                    name = $"{name}.log";

                name = $"log\\{name}";
                try
                {
                    if (File.Exists(name))
                        File.Delete(name);
                }
                catch (IOException)
                {
                    Logger.Log(LogLevel.Critical, "ManagedScript:CreaterLogger", $"IOException while clearing old Log File '{name}' for Script '{id}'");
                }

                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                            .WriteTo.File(name, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} - {Message} {NewLine}")
                            .MinimumLevel.Information();
                return loggerConfiguration.CreateLogger();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedScript:CreaterLogger", $"Exception '{ex.GetType()}' while starting Log for Script '{id}': {ex.Message}");
                return null;
            }
        }

        protected virtual void UseLog(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !LogUseDefault)
                return;

            Log = CreaterLogger(ref name, FileName);
            LogUseDefault = false;
        }

        protected virtual void WriteLog(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                    message = "";

                if (Log != null)
                    Log.Information(ScriptManager.FormatLogMessage(FileName, message));
                else
                    Logger.Log(LogLevel.Information, FileName, message);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedScript:WriteLog", $"Exception '{ex.GetType()}' while writing Message to Log for Script '{FileName}': {message}");
            }
        }
        #endregion
    }
}
