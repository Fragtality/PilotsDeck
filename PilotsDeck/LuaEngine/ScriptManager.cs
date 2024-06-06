using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PilotsDeck
{
    public class ScriptManager(IPCManager manager)
    {
        public readonly static string ScriptFolder = @"Scripts\";
        public readonly static string GlobalScriptFolder = ScriptFolder + @"global\";
        public readonly static string ImageScriptFolder = ScriptFolder + @"image\";
        private IPCManager IPCManager { get; set; } = manager;
        private Dictionary<string, ManagedScript> ManagedScripts { get; set; } = [];
        private Dictionary<string, ManagedGlobalScript> ManagedGlobalScripts { get; set; } = [];
        private Dictionary<string, ManagedImageScript> ManagedImageScripts { get; set; } = [];
        private static string LogFile = "lua.log";
        protected Serilog.Core.Logger Log { get; set; } = ManagedScript.CreaterLogger(ref LogFile, "ScriptManager");
        public int Count {  get { return ManagedScripts.Count; } }
        public int CountGlobal { get { return ManagedGlobalScripts.Where(kv => kv.Value.IsActiveGlobal).Count(); } }
        public int CountImages { get { return ManagedImageScripts.Count; } }
        public bool GlobalScriptsStopped { get; private set; } = true;

        public static string GetRealFileName(string file)
        {
            if (file.StartsWith("lua:", StringComparison.InvariantCultureIgnoreCase))
                file = file.Replace("lua:", "", StringComparison.InvariantCultureIgnoreCase);
            string[] parts = file.Split(':');
            if (parts.Length > 1)
                file = parts[0].ToLower();
            else
                file = file.ToLower();
            if (!file.Contains(".lua", StringComparison.InvariantCultureIgnoreCase))
                file = $"{file}.lua";

            return file;
        }

        public static string GetFunctionName(string address)
        {
            if (address.StartsWith("lua:", StringComparison.InvariantCultureIgnoreCase))
                address = address.Replace("lua:", "", StringComparison.InvariantCultureIgnoreCase);
            string[] parts = address.Split(':');
            if (parts.Length == 2)
                address = parts[1];

            return address;
        }

        public static bool HasFunctionParams(string address)
        {
            return address.Contains('(');
        }

        public static string FormatLogMessage(string context, string message)
        {
            return string.Format("[ {0,-20} ] {1}", (context.Length <= 20 ? context : context[0..20]), message.Replace("\n", "").Replace("\r", "").Replace("\t", ""));
        }

        protected void CheckVariables()
        {
            try
            {
                foreach (var script in ManagedScripts)
                {
                    Logger.Log(LogLevel.Debug, "ScriptManager:CheckVariables", $"Checking Variables and Events for Script '{script.Value.FileName}' (Count: {script.Value.Variables.Count} | Subscribed: {script.Value.Variables.Where(kv => kv.Value).Count()})");
                    script.Value.CheckVariables();
                }

                foreach (var script in ManagedGlobalScripts)
                {
                    Logger.Log(LogLevel.Debug, "ScriptManager:CheckVariables", $"Checking Variables and Events for Script '{script.Value.FileName}' (Count: {script.Value.Variables.Count} | Subscribed: {script.Value.Variables.Where(kv => kv.Value).Count()})");
                    script.Value.CheckVariables();
                }

                foreach (var script in ManagedImageScripts)
                {
                    Logger.Log(LogLevel.Debug, "ScriptManager:CheckVariables", $"Checking Variables and Events for Script '{script.Value.FileName}' (Count: {script.Value.Variables.Count} | Subscribed: {script.Value.Variables.Where(kv => kv.Value).Count()})");
                    script.Value.CheckVariables();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:CheckVariables", $"Exception '{ex.GetType()}' while checking Variables & Events: {ex.Message}");
            }
        }

        private string GetAircraft()
        {
            return IPCManager.SimConnector.AicraftPathString;
        }

        protected bool ToggleGlobalScript(ManagedGlobalScript script, bool reload = false)
        {
            bool nameMatched = !string.IsNullOrWhiteSpace(GetAircraft()) && GetAircraft().Contains(script.Aircraft);
            bool result = false;

            if (nameMatched)
            {
                if (!script.IsActiveGlobal || reload)
                {
                    Logger.Log(LogLevel.Debug, "ScriptManager:ToggleGlobalScript", $"MATCH for Aircraft '{script.Aircraft}' in Script '{script.FileName}' (reload: {reload}) for current AicraftString '{GetAircraft()}'");

                    if (script.IsRunning)
                        script.Stop();

                    script.IsActiveGlobal = true;
                    script.Start();
                    result = true;
                }
                else
                    Logger.Log(LogLevel.Debug, "ScriptManager:ToggleGlobalScript", $"NO CHANGE for Script '{script.FileName}' for current AicraftString '{GetAircraft()}'");
            }
            else if (script.IsActiveGlobal || script.IsRunning)
            {
                Logger.Log(LogLevel.Debug, "ScriptManager:ToggleGlobalScript", $"NO MATCH for Aircraft '{script.Aircraft}' in Script '{script.FileName}' for current AicraftString '{GetAircraft()}'");

                if (script.IsRunning)
                    script.Stop();

                script.IsActiveGlobal = false;
                result = true;
            }
            else
                Logger.Log(LogLevel.Debug, "ScriptManager:ToggleGlobalScript", $"NO CHANGE for Script '{script.FileName}' for current AicraftString '{GetAircraft()}'");

            return result;
        }

        public void StartGlobalScripts()
        {
            try
            {
                Logger.Log(LogLevel.Information, "ScriptManager:StartGlobalScripts", $"Starting Global Scripts");
                foreach (var script in ManagedGlobalScripts)
                    ToggleGlobalScript(script.Value);

                GlobalScriptsStopped = false;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:StartGlobalScripts", $"Exception '{ex.GetType()}' while starting global Scripts: {ex.Message}");
            }
        }

        public void StopGlobalScripts()
        {
            try
            {
                Logger.Log(LogLevel.Information, "ScriptManager:StopGlobalScripts", $"Stopping Global Scripts");

                foreach (var script in ManagedGlobalScripts)
                {
                    script.Value.Stop();
                    script.Value.IsActiveGlobal = false;
                }

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:RunGlobalScripts", $"Exception '{ex.GetType()}' while stopping global Scripts: {ex.Message}");
            }
            finally
            {
                GlobalScriptsStopped = true;
            }
        }

        public int CheckFiles()
        {
            int result = 0;
            try
            {
                foreach (var script in ManagedScripts)
                {
                    if (script.Value.FileHasChanged())
                    {
                        Logger.Log(LogLevel.Information, "ScriptManager:CheckFiles", $"Script File '{script.Key}' has changed in Size - Reloading ...");
                        
                        script.Value.Reload();
                        result++;
                    }
                }

                var globalDir = new DirectoryInfo(GlobalScriptFolder);
                foreach (var file in globalDir.GetFiles())
                {
                    string fileName = GetRealFileName(file.Name);

                    if (!ManagedGlobalScripts.TryGetValue(fileName, out ManagedGlobalScript script))
                    {
                        Logger.Log(LogLevel.Information, "ScriptManager:CheckFiles", $"Adding Global Script File '{fileName}'");
                        
                        script = new ManagedGlobalScript(fileName, Log);
                        script.Stop();
                        ManagedGlobalScripts.Add(fileName, script);
                        if (ToggleGlobalScript(script))
                            result++;
                    }
                    else if (script.FileHasChanged())
                    {
                        Logger.Log(LogLevel.Information, "ScriptManager:CheckFiles", $"Script File '{script.FileName}' has changed in Size - Reloading ...");
                        
                        if (ToggleGlobalScript(script, true))
                            result++;
                    }
                }

                if (result > 0)
                    CheckVariables();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:CheckFiles", $"Exception '{ex.GetType()}' while updating Scripts: {ex.Message}");
            }

            return result;
        }

        public void RunGlobalScripts()
        {
            try
            {
                if (GlobalScriptsStopped)
                    return;

                var now = DateTime.Now;
                foreach (var script in ManagedGlobalScripts)
                {
                    if (script.Value.IsActiveGlobal)
                        script.Value.RunGlobal(now);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(FormatLogMessage("RunGlobalScripts", $"Exception '{ex.GetType()}' while running global Scripts: {ex.Message}"));
            }
        }

        public ManagedScript RegisterScript(string file)
        {
            ManagedScript value = null;
            try
            {
                file = GetRealFileName(file);
                if (ManagedGlobalScripts.ContainsKey(file))
                    return ManagedGlobalScripts[file];

                if (!File.Exists(ScriptFolder + file))
                {
                    Logger.Log(LogLevel.Warning, "ScriptManager:RegisterScript", $"Script File '{file}' does not exist!");
                    return value;
                }

                if (!ManagedScripts.TryGetValue(file, out value))
                {
                    value = new(file, Log);
                    ManagedScripts.Add(file, value);
                    Logger.Log(LogLevel.Information, "ScriptManager:RegisterScript", $"Script File '{file}' added to managed Scripts");
                }
                else
                {
                    value.Registrations++;
                    Logger.Log(LogLevel.Debug, "ScriptManager:RegisterScript", $"Added Registration for Script File '{file}': {value.Registrations}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:RegisterScript", $"Exception '{ex.GetType()}' while registering Script '{file}': {ex.Message}");
            }

            return value;
        }

        public ManagedImageScript RegisterImageScript(string file)
        {
            ManagedImageScript value = null;
            try
            {
                file = GetRealFileName(file);
                if (!File.Exists(ImageScriptFolder + file))
                    Logger.Log(LogLevel.Error, "ScriptManager:RegisterImageScript", $"Script File '{file}' does not exist!");

                if (!ManagedImageScripts.TryGetValue(file, out value))
                {
                    value = new(file, Log);
                    ManagedImageScripts.Add(file, value);
                    Logger.Log(LogLevel.Information, "ScriptManager:RegisterImageScript", $"Script File '{file}' added to managed Scripts");
                }
                else
                {
                    value.Registrations++;
                    Logger.Log(LogLevel.Debug, "ScriptManager:RegisterImageScript", $"Added Registration for Script File '{file}': {value.Registrations}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:RegisterImageScript", $"Exception '{ex.GetType()}' while registering Script '{file}': {ex.Message}");
            }

            return value;
        }

        public void DeregisterScript(string file)
        {
            try
            {
                file = GetRealFileName(file);
                if (ManagedGlobalScripts.ContainsKey(file))
                    return;

                if (ManagedScripts.TryGetValue(file, out ManagedScript script))
                {
                    script.Registrations--;
                    Logger.Log(LogLevel.Debug, "ScriptManager:DeregisterScript", $"Removed Registration for Script File '{file}': {script.Registrations}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "ScriptManager:DeregisterScript", $"Script File '{file}' is not registered!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:DeregisterScript", $"Exception '{ex.GetType()}' while deregistering Script '{file}': {ex.Message}");
            }

        }

        public void DeregisterImageScript(string file)
        {
            try
            {
                file = GetRealFileName(file);

                if (ManagedImageScripts.TryGetValue(file, out ManagedImageScript script))
                {
                    script.Registrations--;
                    Logger.Log(LogLevel.Debug, "ScriptManager:DeregisterImageScript", $"Removed Registration for Script File '{file}': {script.Registrations}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "ScriptManager:DeregisterImageScript", $"Script File '{file}' is not registered!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:DeregisterImageScript", $"Exception '{ex.GetType()}' while deregistering Script '{file}': {ex.Message}");
            }

        }

        public void RemoveUnusedScripts()
        {
            var unusedScripts = ManagedScripts.Where(v => v.Value.Registrations <= 0);
            foreach (var script in unusedScripts)
            {
                script.Value.Stop();
                ManagedScripts.Remove(script.Key);
            }

            var unusedImageScripts = ManagedImageScripts.Where(v => v.Value.Registrations <= 0);
            foreach (var script in unusedImageScripts)
            {
                script.Value.Stop();
                ManagedImageScripts.Remove(script.Key);
            }
        }

        public string RunFunction(string address, bool noReturn = true)
        {
            string result = "";
            if (!IPCManager.SimConnector.IsReady)
                return result;

            string file = GetRealFileName(address);
            string function = GetFunctionName(address);
            if (!HasFunctionParams(address))
                function = $"{function}()";

            if (!ManagedScripts.TryGetValue(file, out ManagedScript script))
            {
                if (!ManagedGlobalScripts.TryGetValue(file, out ManagedGlobalScript globalScript))
                {
                    Logger.Log(LogLevel.Warning, "ScriptManager:RunFunction", $"No managed Script for File '{file}'!");
                    return result;
                }
                else
                    script = globalScript;
            }

            try
            {
                if (noReturn)
                {
                    script.DoChunk(function);
                }
                else
                {
                    LuaResult luaResult = script.DoChunkWithResult($"return {function}");
                    result = $"{luaResult[0]}";
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(FormatLogMessage("RunFunction", $"Exception '{ex.GetType()}' while running Function '{function}' in Script '{script.FileName}': {ex.Message}"));
            }


            return result;
        }
    }
}
