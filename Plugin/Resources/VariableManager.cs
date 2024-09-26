using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.Resources
{
    public class VariableManager : IDisposable
    {
        protected static SimController SimController { get { return App.SimController; } }
        protected static ScriptManager ScriptManager { get { return App.PluginController.ScriptManager; } }
        public ConcurrentDictionary<string, ManagedVariable> ManagedVariables { get; } = [];
        public IEnumerable<ManagedVariable> VariableList { get { return ManagedVariables.Values; } }
        public static readonly string ADDRESS_EMPTY = "Z:NULL";

        public static string FormatAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return ADDRESS_EMPTY;

            if (TypeMatching.rxOffset.IsMatch(address))
            {
                string[] parts = address.Split(':');
                int idx = 0;
                if (parts[0].StartsWith("0x"))
                    idx = 2;
                string sub = parts[0].Substring(idx, 4).ToUpper();
                parts[0] = "0x" + sub;
                address = string.Join(":", parts);
            }
            else if (TypeMatching.rxAvar.IsMatch(address))
            {
                if (!address.StartsWith("(A:"))
                    address = address.Insert(1, "A:");
                address = address.Replace(", ", ",");
            }
            else if (TypeMatching.rxLuaFunc.IsMatch(address))
            {
                string[] parts = address.Split(':');
                address = $"lua:{parts[1].ToLower().Replace(".lua", "")}:{parts[2]}";
            }
            else if (TypeMatching.rxInternal.IsMatch(address))
                address = address.ToUpperInvariant();
            else if (TypeMatching.rxCalcRead.IsMatch(address))
            {

            }
            else if (TypeMatching.rxLvar.IsMatch(address))
            {
                //var matches = TypeMatching.rxLvarMobiMatch.Matches(address);
                //if (matches != null && matches.Count > 0)
                //    address = $"(L:{matches[0].Value},number)";
                if (!address.StartsWith("L:"))
                    address = $"(L:{address})";
                else
                    address = $"({address})";
            }

            return address;
        }

        public ManagedVariable this[string address]
        {
            get
            {
                address = FormatAddress(address);
                if (!ManagedVariables.TryGetValue(address, out ManagedVariable variable))
                {
                    Logger.Error($"The Address '{address}' is not registered");
                    variable = null;
                }
                return variable;
            }
        }

        public bool TryGet(string address, out ManagedVariable variable)
        {
            address = FormatAddress(address);
            return ManagedVariables.TryGetValue(address, out variable);
        }

        public bool Contains(string address)
        {
            address = FormatAddress(address);
            return ManagedVariables.ContainsKey(address);
        }

        public static ManagedVariable CreateEmptyVariable()
        {
            return new VariableNumeric(ADDRESS_EMPTY, SimValueType.NONE);
        }

        public static ManagedVariable CreateVariable(string address)
        {
            ManagedVariable value;

            if (address == ADDRESS_EMPTY)
                value = CreateEmptyVariable();
            else if (TypeMatching.rxOffset.IsMatch(address))
                value = new VariableOffset(address, AppConfiguration.IpcGroupRead);
            else if (TypeMatching.IsStringDataRef(address))
                value = new VariableString(address, SimValueType.XPDREF);
            else if (TypeMatching.rxDref.IsMatch(address))
                value = new VariableNumeric(address, SimValueType.XPDREF);
            else if (TypeMatching.rxBvarValue.IsMatch(address))
                value = new VariableInputEvent(address);
            else if (TypeMatching.rxLuaFunc.IsMatch(address) && ScriptManager.HasScript(address))
                value = new VariableLua(address);
            else if (TypeMatching.rxInternal.IsMatch(address))
                value = new VariableString(address, SimValueType.INTERNAL);
            else if (TypeMatching.rxCalcRead.IsMatch(address))
                value = new VariableString(address, SimValueType.CALCULATOR);
            else if (TypeMatching.rxAvar.IsMatch(address))
            {
                var matches = TypeMatching.rxAvar.Matches(address);
                if (matches != null && matches.Count > 0 && matches[0]?.Groups?.Count >= 4 && matches[0]?.Groups[3]?.Value?.ToLowerInvariant() == "string")
                    value = new VariableString(address, SimValueType.AVAR);
                else
                    value = new VariableNumeric(address, SimValueType.AVAR);
            }
            else if (TypeMatching.rxLvar.IsMatch(address) || TypeMatching.rxLvarMobi.IsMatch(address))
                value = new VariableNumeric(address, SimValueType.LVAR);
            else
                value = CreateEmptyVariable();

            return value;
        }

        public void ResetChangedState(bool changed)
        {
            ManagedVariables.Values.ToList().ForEach(v => v.IsChanged = changed);
        }

        public void CheckChanged()
        {
            ManagedVariables.Values.ToList().ForEach(v => v.CheckChanged());
        }

        public ManagedVariable RegisterVariable(string address)
        {
            ManagedVariable variable = null;
            try
            {
                address = FormatAddress(address);
                SimValueType type = TypeMatching.GetReadType(address);

                if (type == SimValueType.LUAFUNC)
                {
                    ScriptManager.RegisterScript(address);
                }

                if (ManagedVariables.TryGetValue(address, out variable))
                {
                    variable.AddRegistration();
                    if (variable.IsValueInternal())
                        variable.IsSubscribed = true;
                    Logger.Verbose($"Added Registration for Address '{address}'. (Registrations: {variable.Registrations})");
                    if (variable == null)
                        Logger.Error($"Registered Address '{address}' has NULL-Reference Variable! (Registrations: {variable.Registrations})");
                }
                else
                {
                    variable = CreateVariable(address);

                    if (variable.Address != address && variable.Address == ADDRESS_EMPTY)
                        Logger.Warning($"Could not create Variable for Address '{address}'!");                    

                    if (ManagedVariables.TryGetValue(variable.Address, out ManagedVariable existing))
                    {
                        Logger.Warning($"Attempted to create Variable for existing Address: '{address}' -> '{variable.Address}'");
                        variable = existing;
                        variable.AddRegistration();
                    }
                    else
                        ManagedVariables.TryAdd(variable.Address, variable);

                    if (variable.IsValueInternal())
                        variable.IsSubscribed = true;
                    Logger.Verbose($"Added {variable.GetType().Name} for Address '{variable.Address}'. (Registrations: {variable.Registrations})");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return variable;
        }

        public void DeregisterVariable(string address)
        {
            try
            {
                address = FormatAddress(address);
                SimValueType type = TypeMatching.GetReadType(address);

                if (type == SimValueType.LUAFUNC)
                {
                    ScriptManager.DeregisterScript(address);
                }

                if (!string.IsNullOrWhiteSpace(address) && ManagedVariables.TryGetValue(address, out ManagedVariable variable))
                {
                    variable.RemoveRegistration();
                    if (variable.Registrations >= 1)
                        Logger.Verbose($"Deregistered Address '{address}'. (Registrations: {variable.Registrations})");
                    else
                    {
                        Logger.Verbose($"Deregistered Address '{address}'. (Registrations: {variable.Registrations})");
                        if (variable.IsValueInternal())
                            variable.IsSubscribed = false;
                    }
                }
                else
                    Logger.Error($"Could not find Address '{address}'!");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public int RemoveUnused()
        {
            var unusedVariables = ManagedVariables.Where(v => !v.Value.IsSubscribed && v.Value.Registrations <= 0);
            int count = unusedVariables.Count();
            if (count != 0)
                Logger.Information($"Removing {count} unused Variables ...");

            foreach (var variable in unusedVariables)
            {
                variable.Value.Dispose();
                ManagedVariables.Remove(variable.Key, out _);
                Logger.Verbose($"Removed Variable {variable.Key} from Cache.");
            }

            if (App.PluginController.State == Plugin.PluginState.IDLE && !ManagedVariables.IsEmpty && App.PluginController.ActionManager.Count == 0 && App.PluginController.ScriptManager.CountTotal == 0)
            {
                Logger.Warning($"Force Removal of {ManagedVariables.Count} Variables (no other Ressources active)");
                foreach (var variable in ManagedVariables)
                {
                    Logger.Warning($"{variable.Key} ({variable.Value?.Address}) (Registrations: {variable.Value?.Registrations}) (Subscribed {variable.Value?.IsSubscribed})");
                    variable.Value?.Dispose();
                    count++;
                }
                ManagedVariables.Clear();
            }

            return count;
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ManagedVariables.Values.ToList().ForEach(v => v.Dispose());
                    ManagedVariables.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
