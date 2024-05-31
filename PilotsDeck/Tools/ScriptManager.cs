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
        private IPCManager IPCManager { get; set; } = manager;
        private Dictionary<string, ManagedScript> ManagedScripts { get; set; } = [];
        public int Count {  get { return ManagedScripts.Count; } }

        public static string GetRealFileName(string file)
        {
            if (file.StartsWith("lua:", StringComparison.InvariantCultureIgnoreCase))
                file = file.Replace("lua:", "", StringComparison.InvariantCultureIgnoreCase);
            string[] parts = file.Split(':');
            if (parts.Length > 1)
                file = parts[0].ToLower();
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

        //public void DeregisterAllVariables()
        //{
        //    foreach (var script in ManagedScripts)
        //        script.Value.DeregisterAllVariables();
        //}

        public void RegisterAllVariables()
        {
            foreach (var script in ManagedScripts)
                script.Value.RegisterAllVariables();
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
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ScriptManager:CheckFiles", $"Exception '{ex.GetType()}' while updating Scripts: {ex.Message}");
            }

            return result;
        }
    }
}
