using CFIT.AppLogger;
using Neo.IronLua;
using PilotsDeck.Resources.Scripts;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PilotsDeck.Resources
{
    public class ScriptManager
    {
        public readonly static string ScriptFolder = AppConfiguration.DirScripts;
        public readonly static string GlobalScriptFolder = AppConfiguration.DirScriptsGlobal;
        public readonly static string ImageScriptFolder = AppConfiguration.DirScriptsImage;
        private static SimController SimController { get { return App.SimController; } }
        public ConcurrentDictionary<string, ManagedScript> ManagedScripts { get; private set; } = [];
        public ConcurrentDictionary<string, ManagedGlobalScript> ManagedGlobalScripts { get; private set; } = [];
        public ConcurrentDictionary<string, ManagedImageScript> ManagedImageScripts { get; private set; } = [];
        private static string LogFile = "lua.log";
        protected Serilog.Core.Logger Log { get; set; } = ManagedScript.CreaterLogger(ref LogFile, "ScriptManager");
        public int Count {  get { return ManagedScripts.Count; } }
        public int CountGlobal { get { return ManagedGlobalScripts.Where(kv => kv.Value.IsActiveGlobal).Count(); } }
        public int CountImages { get { return ManagedImageScripts.Count; } }
        public int CountTotal { get { return Count + CountGlobal + CountImages; } }
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

        public static bool HasFunctionParams(string address)
        {
            return address.Contains('(');
        }

        public static string FormatLogMessage(string context, string message)
        {
            return string.Format("[ {0,-20} ] {1}", (context.Length <= 20 ? context : context[0..20]), message.Replace("\n", "").Replace("\r", "").Replace("\t", ""));
        }

        private static string GetAircraft()
        {
            return SimController.AircraftString;
        }

        protected static bool ToggleGlobalScript(ManagedGlobalScript script, bool reload = false)
        {
            bool result = false;

            if (reload)
                script.Reload();

            bool isAllowedToRun = !string.IsNullOrWhiteSpace(GetAircraft()) && GetAircraft().ToLowerInvariant().Contains(script.Aircraft.ToLowerInvariant())
                                  && (SimController.SimMainType == script.SimulatorType || script.SimulatorType == SimulatorType.NONE);

            if (isAllowedToRun)
            {
                Logger.Debug($"MATCH for Aircraft '{script.Aircraft}' / Type '{script.SimulatorType}' in Script '{script.FileName}' (reload: {reload}) for current AicraftString '{GetAircraft()}' / Simulator '{SimController.SimMainType}'");

                if (!script.IsActiveGlobal)
                    Logger.Information($"Starting Global Script '{script.FileName}'");
                script.IsActiveGlobal = true;

                if (!script.IsRunning)
                    script.Start();
            }
            else
            {
                if (script.IsRunning)
                    script.Stop();

                if (script.IsActiveGlobal)
                    Logger.Information($"Stopping Global Script '{script.FileName}'");
                script.IsActiveGlobal = false;
            }

            return result;
        }

        public void StartGlobalScripts()
        {
            try
            {
                Logger.Information($"Starting Global Scripts");
                foreach (var script in ManagedGlobalScripts)
                    ToggleGlobalScript(script.Value);

                GlobalScriptsStopped = false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void StopGlobalScripts()
        {
            try
            {
                if (ManagedGlobalScripts.Where(g => g.Value.IsRunning).Any())
                    Logger.Information($"Stopping Global Scripts");

                foreach (var script in ManagedGlobalScripts)
                {
                    script.Value.Stop();
                    script.Value.IsActiveGlobal = false;
                }

            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
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
                        Logger.Information($"Script File '{script.Key}' has changed in Size - Reloading ...");
                        
                        script.Value.Reload();
                        result++;
                    }
                }

                var globalDir = new DirectoryInfo(GlobalScriptFolder);
                foreach (var file in globalDir.GetFiles())
                {
                    string fileName = file.Name.ToLowerInvariant();

                    if (!ManagedGlobalScripts.TryGetValue(fileName, out ManagedGlobalScript script))
                    {
                        Logger.Information($"Adding Global Script File '{fileName}'");
                        
                        script = new ManagedGlobalScript(fileName, Log);
                        script.Stop();
                        ManagedGlobalScripts.TryAdd(fileName, script);
                        if (ToggleGlobalScript(script))
                            result++;
                    }
                    else if (script.FileHasChanged())
                    {
                        Logger.Information($"Script File '{script.FileName}' has changed in Size - Reloading ...");
                        
                        if (ToggleGlobalScript(script, true))
                            result++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
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
                        _ = Task.Run(() => script.Value.RunGlobal(now));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static bool HasScript(ManagedAddress address)
        {
            string file = address.FormatLuaFile();
            return File.Exists(ScriptFolder + file) || File.Exists(GlobalScriptFolder + file);
        }

        public ManagedScript RegisterScript(ManagedAddress address)
        {
            ManagedScript script = null;
            try
            {
                string file = address.FormatLuaFile();
                bool foundScript = File.Exists(ScriptFolder + file);
                bool foundGlobal = File.Exists(GlobalScriptFolder + file);

                if (!foundScript && !foundGlobal)
                {
                    Logger.Warning($"Script File '{file}' does not exist!");
                    return script;
                }

                if (foundScript)
                {
                    if (!ManagedScripts.TryGetValue(file, out script))
                    {
                        script = new(file, Log);
                        ManagedScripts.TryAdd(file, script);
                        Logger.Debug($"Script File '{file}' added to managed Scripts");
                    }
                    else
                    {
                        script.AddRegistration();
                        Logger.Verbose($"Added Registration for Script File '{file}': {script.Registrations}");
                    }
                }
                else if (foundGlobal)
                {
                    if (ManagedGlobalScripts.TryGetValue(file, out ManagedGlobalScript? value))
                    {
                        script = value;
                        script.AddRegistration();
                        Logger.Debug($"Added Registration for Global Script File '{file}': {script.Registrations}");
                        return script;
                    }
                    else
                    {
                        Logger.Warning($"Global Script '{file}' is not registered!");
                        return script;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return script;
        }

        public ManagedImageScript RegisterImageScript(string file)
        {
            ManagedImageScript script = null;
            try
            {
                if (!File.Exists(ImageScriptFolder + file))
                    Logger.Error($"Script File '{file}' does not exist!");

                if (!ManagedImageScripts.TryGetValue(file, out script))
                {
                    script = new(file, Log);
                    ManagedImageScripts.TryAdd(file, script);
                    Logger.Information($"Script File '{file}' added to managed Scripts");
                }
                else
                {
                    script.AddRegistration();
                    Logger.Debug($"Added Registration for Script File '{file}': {script.Registrations}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return script;
        }

        public void DeregisterScript(ManagedAddress address)
        {
            try
            {
                string file = address.FormatLuaFile();

                if (ManagedScripts.TryGetValue(file, out ManagedScript script))
                {
                    script.RemoveRegistration();
                    if (script.Registrations <= 0)
                        Logger.Debug($"Removed Registration for Script File '{file}': {script.Registrations}");
                    else
                        Logger.Verbose($"Removed Registration for Script File '{file}': {script.Registrations}");
                }
                else if (!ManagedGlobalScripts.TryGetValue(file, out ManagedGlobalScript globalScript))
                {
                    Logger.Warning($"Script File '{file}' is not registered!");
                }
                else if (globalScript != null)
                {
                    globalScript.RemoveRegistration();
                    if (globalScript.Registrations <= 0)
                        Logger.Debug($"Removed Registration for Global Script File '{file}': {globalScript.Registrations}");
                    else
                        Logger.Verbose($"Removed Registration for Global Script File '{file}': {globalScript.Registrations}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

        }

        public void DeregisterImageScript(string file)
        {
            try
            {
                if (ManagedImageScripts.TryGetValue(file, out ManagedImageScript script))
                {
                    script.RemoveRegistration();
                    Logger.Debug($"Removed Registration for Script File '{file}': {script.Registrations}");
                }
                else
                {
                    Logger.Warning($"Script File '{file}' is not registered!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

        }

        public int RemoveUnused()
        {
            var unusedScripts = ManagedScripts.Where(v => v.Value.Registrations <= 0);
            int count = unusedScripts.Count();
            if (unusedScripts.Any())
                Logger.Information($"Removing {unusedScripts.Count()} unused Scripts ...");

            foreach (var script in unusedScripts)
            {
                script.Value.Stop();
                ManagedScripts.Remove(script.Key, out _);
            }

            var unusedImageScripts = ManagedImageScripts.Where(v => v.Value.Registrations <= 0);
            count += unusedImageScripts.Count();
            if (unusedImageScripts.Any())
                Logger.Information($"Removing {unusedImageScripts.Count()} unused Image-Scripts ...");

            foreach (var script in unusedImageScripts)
            {
                script.Value.Stop();
                ManagedImageScripts.Remove(script.Key, out _);
            }

            return count;
        }

        public string RunFunction(ManagedAddress address, out bool hasError, bool noReturn = true)
        {
            string result = "";
            hasError = false;
            if (!SimController.IsReadyProcess)
            {
                hasError = true;
                return result;
            }

            string file = address.FormatLuaFile();
            string function = address.Parameter;
            if (!function.EndsWith(')'))
                function = $"{function}()";

            if (!ManagedScripts.TryGetValue(file, out ManagedScript script))
            {
                if (!ManagedGlobalScripts.TryGetValue(file, out ManagedGlobalScript globalScript) || globalScript?.IsRunning == false)
                {
                    Logger.Warning($"The Script '{file}' is not registered or running");
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
            catch (LuaRuntimeException ex)
            {
                hasError = true;
                Log.Fatal(FormatLogMessage(script.FileName, $"{ex.GetType()}: {ex.Message}"));
            }
            catch (Exception ex)
            {
                hasError = true;
                Logger.LogException(ex);
            }


            return result;
        }
    }
}
