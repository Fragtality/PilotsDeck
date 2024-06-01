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
        private IPCManager IPCManager { get; set; } = manager;
        private Dictionary<string, ManagedScript> ManagedScripts { get; set; } = [];
        private Dictionary<string, ManagedScript> ManagedGlobalScripts { get; set; } = [];
        public int Count {  get { return ManagedScripts.Count; } }
        public int CountGlobal { get { return ManagedGlobalScripts.Where(kv => kv.Value.IsActiveGlobal).Count(); } }
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

        public void RegisterAllVariables()
        {
            foreach (var script in ManagedScripts)
                script.Value.RegisterAllVariables();

            foreach (var script in ManagedGlobalScripts)
                script.Value.RegisterAllVariables();
        }

        private string GetAircraft()
        {
            return IPCManager.SimConnector.AicraftPathString;
        }

        public void StartGlobalScripts()
        {
            Logger.Log(LogLevel.Information, "ScriptManager:StartGlobalScripts", $"Starting Global Scripts");

            GlobalScriptsStopped = false;
            foreach (var script in ManagedGlobalScripts)
            {
                if (GetAircraft().Contains(script.Value.Aircraft))
                {
                    Logger.Log(LogLevel.Debug, "ScriptManager:CheckFiles", $"Script Aircraft '{script.Value.Aircraft}' matched current AicraftString '{GetAircraft()}'");
                    script.Value.IsActiveGlobal = true;
                    script.Value.Start();
                }
                else
                {
                    Logger.Log(LogLevel.Debug, "ScriptManager:CheckFiles", $"Script Aircraft '{script.Value.Aircraft}' NOT matched current AicraftString '{GetAircraft()}'");
                    script.Value.Stop();
                }
            }
        }

        public void StopGlobalScripts()
        {
            Logger.Log(LogLevel.Information, "ScriptManager:StopGlobalScripts", $"Stopping Global Scripts");

            foreach (var script in ManagedGlobalScripts)
            {
                script.Value.Stop();
            }
            GlobalScriptsStopped = true;
        }

        public void RunGlobalScripts()
        {
            if (GlobalScriptsStopped)
                return;

            var now = DateTime.Now;
            foreach (var script in ManagedGlobalScripts)
            {
                if (script.Value.IsActiveGlobal)
                    script.Value.Run(now);
            }
        }

        public void RegisterScript(string file)
        {
            try
            {
                file = GetRealFileName(file);
                if (!File.Exists(ScriptFolder + file))
                    Logger.Log(LogLevel.Error, "ScriptManager:RegisterScript", $"Script File '{file}' does not exist!");

                if (!ManagedScripts.TryGetValue(file, out ManagedScript value))
                {
                    ManagedScripts.Add(file, new(file, IPCManager));
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
        }

        public void DeregisterScript(string file)
        {
            try
            {
                file = GetRealFileName(file);

                if (ManagedScripts.TryGetValue(file, out ManagedScript script))
                {
                    script.Registrations--;
                    Logger.Log(LogLevel.Debug, "ScriptManager:RegisterScript", $"Removed Registration for Script File '{file}': {script.Registrations}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, "ScriptManager:RegisterScript", $"Script File '{file}' is not registered!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:DeregisterScript", $"Exception '{ex.GetType()}' while deregistering Script '{file}': {ex.Message}");
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
        }

        public string RunFunction(string address, bool noReturn = true)
        {
            string result = "";
            if (!IPCManager.SimConnector.IsReady)
                return result;

            string file = GetRealFileName(address);
            string function = GetFunctionName(address);
            if (!ManagedScripts.TryGetValue(file, out ManagedScript script))
            {
                Logger.Log(LogLevel.Warning, "ScriptManager:RunFunction", $"No managed Script for File '{file}'!");
                return result;
            }

            try
            {
                if (noReturn)
                {
                    script.DoChunk($"{function}()");
                }
                else
                {
                    LuaResult luaResult = script.DoChunkWithResult($"return {function}()");
                    result = $"{luaResult[0]}";
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:RunFunction", $"Exception '{ex.GetType()}' while running Function '{function}': {ex.Message}");
            }


            return result;
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
                    //ADD/UPDATE GLOBAL SCRIPTS
                    string fileName = GetRealFileName(file.Name);

                    if (ManagedGlobalScripts.TryGetValue(fileName, out ManagedScript script))
                    {
                        bool wasActiveGlobal = script.IsActiveGlobal;
                        if (script.FileHasChanged())
                        {
                            Logger.Log(LogLevel.Information, "ScriptManager:CheckFiles", $"Global Script File '{script.FileName}' has changed in Size - Reloading ...");
                            script.Reload();
                            result++;
                            if (wasActiveGlobal && !GlobalScriptsStopped)
                                script.IsActiveGlobal = true;
                            else if (!GlobalScriptsStopped && GetAircraft().Contains(script.Aircraft))
                            {
                                Logger.Log(LogLevel.Debug, "ScriptManager:CheckFiles", $"Script Aircraft '{script.Aircraft}' matched current AicraftString '{GetAircraft()}'");
                                script.IsActiveGlobal = true;
                            }
                        }
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "ScriptManager:CheckFiles", $"Adding Global Script File '{fileName}'");
                        script = new ManagedScript(fileName, IPCManager, true);
                        ManagedGlobalScripts.Add(fileName, script);
                        if (GetAircraft().Contains(script.Aircraft) && !GlobalScriptsStopped)
                        {
                            Logger.Log(LogLevel.Debug, "ScriptManager:CheckFiles", $"Script Aircraft '{script.Aircraft}' matched current AicraftString '{GetAircraft()}'");
                            script.IsActiveGlobal = true;
                        }
                        else
                        {
                            Logger.Log(LogLevel.Debug, "ScriptManager:CheckFiles", $"Script Aircraft '{script.Aircraft}' NOT matched current AicraftString '{GetAircraft()}'");
                            script.Stop();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:CheckFiles", $"Exception '{ex.GetType()}' while updating Scripts: {ex.Message}");
            }

            return result;
        }
    }
}
