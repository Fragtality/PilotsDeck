﻿using CFIT.AppLogger;
using CFIT.AppTools;
using Neo.IronLua;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace PilotsDeck.Resources.Scripts
{
    public class ManagedScript : IManagedRessource
    {
        public virtual string UUID { get { return FileName; } }
        public virtual Lua LuaEngine { get; set; } = null;
        public virtual bool IsRunning { get { return LuaEngine != null; } }
        public virtual LuaGlobal LuaEnv { get; set; }
        public virtual LuaChunk LuaChunk { get; set; }
        public virtual string FileName { get; set; }
        public virtual DateTime LastWriteTime { get; set; }
        public virtual Dictionary<ManagedAddress, bool> Variables { get; set; } = [];
        public virtual int Registrations { get; set; } = 1;
        public virtual bool LogUseDefault { get; set; } = true;
        protected virtual Serilog.Core.Logger Log { get; set; }
        protected virtual VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        protected static SimController SimController { get { return App.SimController; } }


        public ManagedScript(string file, Serilog.Core.Logger log)
        {
            Log = log;
            Init(file);
        }

        protected virtual void Init(string file)
        {
            FileName = file;
            Start(new FileInfo(GetScriptFolder(FileName)));
        }

        public virtual void DoChunk()
        {
            if (IsRunning)
                LuaEnv.DoChunk(LuaChunk);
            else
                Logger.Debug($"DoChunk while Script '{FileName}' is not running");
        }

        public virtual void DoChunk(string code)
        {
            if (IsRunning)
                LuaEnv.DoChunk(code, FileName);
            else
                Logger.Debug($"DoChunk while Script '{FileName}' is not running. Code: {code}");
        }

        public virtual LuaResult DoChunkWithResult(string code)
        {
            if (IsRunning)
                return LuaEnv.DoChunk(code, FileName);
            else
            {
                Logger.Debug($"DoChunk while Script '{FileName}' is not running. Code: {code}");
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
            _env.Sleep = new Action<int>(ScriptSleep);
            _env.UseLog = new Action<string>(UseLog);
            _env.Log = new Action<string>(WriteLog);
        }

        public virtual void Start(FileInfo fileInfo = null, Serilog.Core.Logger log = null)
        {
            try
            {
                if (LuaEngine != null)
                    return;

                fileInfo ??= new FileInfo(GetScriptFolder(FileName));
                LastWriteTime = fileInfo.LastWriteTime;

                if (log != null && (LogUseDefault || Log == null))
                    Log = log;

                LuaEngine = new();
                CreateEnvironment();

                string code = File.ReadAllText(GetScriptFolder(FileName));
                LuaChunk = LuaEngine.CompileChunk(code, FileName, new LuaCompileOptions() { ClrEnabled = false, DebugEngine = LuaExceptionDebugger.Default });
                DoChunk();

                Log?.Information(ScriptManager.FormatLogMessage(FileName, $"Script started by ScriptManager"));
                Logger.Debug($"Script started: {FileName}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual void Stop()
        {
            if (IsRunning)
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
            Logger.Debug($"Script stopped: {FileName}");
        }

        public virtual void Reload()
        {
            Stop();
            Start();
        }

        public virtual bool FileHasChanged()
        {
            return new FileInfo(GetScriptFolder(FileName)).LastWriteTime != LastWriteTime;
        }

        public virtual void DeregisterAllVariables()
        {
            Logger.Debug($"Deregistering Variables and Events for Script '{FileName}'");
            foreach (var variable in Variables)
            {
                if (variable.Value)
                {
                    VariableManager.DeregisterVariable(variable.Key);
                    Variables[variable.Key] = false;
                }
            }
        }

        #region ScriptFunctions
        protected virtual bool RegisterVariable(string name)
        {
            Logger.Verbose($"SimVar Request from Script File '{FileName}' for Variable '{name}'");
            var address = new ManagedAddress(name);

            if (Variables.TryGetValue(address, out bool value))
            {
                if (value)
                    Logger.Warning($"Variable '{address}' is already registered for Script File '{FileName}'");
                else
                    Variables[address] = VariableManager.RegisterVariable(address) != null;
            }
            else if (VariableManager.RegisterVariable(address) != null)
            {
                Variables.Add(address, true);
                Logger.Verbose($"Registered Variable '{address}' for Script File '{FileName}'");
                return true;
            }
            else
            {
                Variables.Add(address, false);
                Logger.Error($"Could not register Variable '{address}' for Script File '{FileName}'");
            }

            return false;
        }

        protected virtual dynamic SimRead(string name)
        {
            ManagedVariable value = VariableManager[new ManagedAddress(name)];
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
            ManagedVariable value = VariableManager[new ManagedAddress(name)];
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
                else if (value is bool b)
                {
                    number = b ? 1 : 0;
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
            else if (value is LuaTable)
                return SimWrite(name, value as LuaTable);
            else
                return SimWrite(name, value.ToString());
        }

        protected virtual bool SimWrite(string name, LuaTable value)
        {
            return SimWrite(name, "[" + string.Join(",", value.ArrayList.Select(v => v.ToString())) + "]");
        }


        protected virtual bool SimWrite(string name, double value)
        {
            return SimWrite(name, Conversion.ToString(value));
        }

        protected virtual bool SimWrite(string name, string value)
        {
            SimCommandType? actionType = TypeMatching.GetCommandValueType(name);
            if (actionType == null)
                return false;

            SimCommand command = new()
            {
                Address = new ManagedAddress(name, (SimCommandType)actionType, true),
                Type = (SimCommandType)actionType,
                Value = value,
            };
            _ = SimController.CommandChannel.WriteAsync(command).AsTask();
            return true;
        }

        protected virtual bool SimCommand(string name)
        {
            SimCommandType? actionType = TypeMatching.GetCommandOnlyType(name);
            if (actionType == null || actionType == SimCommandType.VJOY || actionType == SimCommandType.VJOYDRV)
                return false;

            SimCommand command = new()
            {
                Address = new ManagedAddress(name, (SimCommandType)actionType, true),
                Type = (SimCommandType)actionType,
                IsUp = true,
            };
            SimController.CommandChannel.TryWrite(command);

            return true;
        }

        protected virtual bool JoystickCommand(string name, string operation)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(operation))
                return false;
            operation = operation.ToLowerInvariant();
            if (!name.StartsWith("vjoy:"))
                name = $"vjoy:{name}";

            SimCommandType? actionType = TypeMatching.GetCommandOnlyType(name);
            if (actionType == null || (actionType != SimCommandType.VJOY && actionType != SimCommandType.VJOYDRV))
                return false;

            bool isUp = true;
            if (operation == "down")
                isUp = false;
            else if (operation == "toggle")
                name = $"{name}:t";

            SimCommand command = new()
            {
                Address = new ManagedAddress(name, (SimCommandType)actionType, true),
                Type = (SimCommandType)actionType,
                IsUp = isUp,
            };
            SimController.CommandChannel.TryWrite(command);

            return true;
        }

        protected virtual bool SimCalculator(string code)
        {
            SimCommand command = new()
            {
                Address = new ManagedAddress(code, SimCommandType.CALCULATOR, true),
                Type = SimCommandType.CALCULATOR
            };
            _ = SimController.CommandChannel.WriteAsync(command).AsTask();
            return true;
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
                    Logger.Error($"IOException while clearing old Log File '{name}' for Script '{id}'");
                }

                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                            .WriteTo.File(name, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} - {Message} {NewLine}")
                            .MinimumLevel.Information();
                return loggerConfiguration.CreateLogger();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
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
                Logger.LogException(ex);
            }
        }

        protected virtual string GetRegistryValue(string path, string value)
        {
            return Sys.GetRegistryValue<string>(path, value) ?? "";
        }

        public override string ToString()
        {
            return FileName ?? "";
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
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
                }
                _disposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
