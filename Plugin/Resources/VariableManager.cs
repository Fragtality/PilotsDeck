using CFIT.AppLogger;
using PilotsDeck.Resources.Variables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.Resources
{
    public class VariableManager : IDisposable
    {
        protected static ScriptManager ScriptManager { get { return App.PluginController.ScriptManager; } }
        public ConcurrentDictionary<ManagedAddress, ManagedVariable> ManagedVariables { get; } = [];
        public IEnumerable<ManagedVariable> VariableList { get { return ManagedVariables.Values; } }

        public ManagedVariable this[ManagedAddress address]
        {
            get
            {
                if (!ManagedVariables.TryGetValue(address, out ManagedVariable variable))
                {
                    Logger.Error($"The Address '{address}' is not registered");
                    variable = null;
                }
                return variable;
            }
        }

        public bool TryGet(ManagedAddress address, out ManagedVariable variable)
        {
            return ManagedVariables.TryGetValue(address, out variable);
        }

        public bool Contains(ManagedAddress address)
        {
            return ManagedVariables.ContainsKey(address);
        }

        public static ManagedVariable CreateEmptyVariable()
        {
            return new VariableNumeric(ManagedAddress.CreateEmpty());
        }

        public static ManagedVariable CreateVariable(ManagedAddress address)
        {
            ManagedVariable value;

            if (address == null || address?.IsEmpty == true)
                value = CreateEmptyVariable();
            else if (address == SimValueType.OFFSET)
                value = new VariableOffset(address, AppConfiguration.IpcGroupRead);
            else if (address == SimValueType.XPDREF && address.IsStringType)
                value = new VariableString(address);
            else if (address == SimValueType.XPDREF)
                value = new VariableNumeric(address);
            else if (address == SimValueType.BVAR)
                value = new VariableNumeric(address);
            else if (address == SimValueType.LUAFUNC && ScriptManager.HasScript(address))
                value = new VariableLua(address);
            else if (address == SimValueType.INTERNAL)
                value = new VariableString(address);
            else if (address == SimValueType.CALCULATOR)
                value = new VariableNumeric(address);
            else if (address == SimValueType.AVAR)
            {
                if (address.IsStringType)
                    value = new VariableString(address);
                else
                    value = new VariableNumeric(address);
            }
            else if (address == SimValueType.KVAR)
                value = new VariableNumeric(address);
            else if (address == SimValueType.LVAR)
                value = new VariableNumeric(address);
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

        public ManagedVariable RegisterVariable(ManagedAddress address)
        {
            ManagedVariable variable = null;
            try
            {
                if (address == SimValueType.LUAFUNC)
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

                    if (variable.Address != address?.Address && variable.Address.IsEmpty)
                    {
                        Logger.Warning($"Could not create Variable for Address '{address}'!");
                        return null;
                    }
                    
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

        public void DeregisterVariable(ManagedAddress address)
        {
            try
            {
                if (address == SimValueType.LUAFUNC)
                {
                    ScriptManager.DeregisterScript(address);
                }

                if (address != null && ManagedVariables.TryGetValue(address, out ManagedVariable variable))
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
