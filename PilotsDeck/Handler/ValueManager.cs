using Serilog;
using System.Collections.Generic;

namespace PilotsDeck
{
    using ValuePair = KeyValuePair<string, IPCValue>;

    public static class ID
    {
        public static readonly string SwitchState = "SwitchState";
        public static readonly string SwitchStateLong = "SwitchStateLong";
        
        public static readonly string ControlState = "ControlState";
        
        public static readonly string Top = "ControlState";
        public static readonly string Bot = "ControlStateBot";
        
        public static readonly string Active = "ControlState";
        public static readonly string Standby = "ControlStateBot";

        public static readonly string First = "ControlState";
        public static readonly string Second = "ControlStateBot";
    };

    public class AddressValueManager
    {
        //ID (Current, Last, ...) => Address = Value
        protected Dictionary<string, ValuePair> managedValues = new ();
        protected IPCManager ipcManager = null;

        public void RegisterManager(IPCManager manager)
        {
            if (ipcManager == null)
                ipcManager = manager;
        }

        public bool RegisterValue(string id, string address)
        {
            IPCValue value = ipcManager.RegisterAddress(address);
            
            if (value != null)
            {
                if (!managedValues.ContainsKey(id))
                {
                    managedValues.Add(id, new ValuePair(address, value));
                    return true;
                }
                else
                {
                    Log.Logger.Error($"ValueManager: Variable {id} already exists!");
                }
            }
            else
            {
                Log.Logger.Error($"ValueManager: Could not Register Address {address} for Variable {id}!");
            }

            return false;
        }

        protected bool UpdateIPC(string id, string newAddress)
        {
            if (managedValues[id].Key != newAddress)
            {
                ipcManager.DeregisterAddress(managedValues[id].Key);
                IPCValue value = ipcManager.RegisterAddress(newAddress);

                if (value != null)
                {
                    Log.Logger.Debug($"ValueManager: Updated Variable {id} with new Address {newAddress}. (Old: {managedValues[id].Key}");
                    managedValues[id] = new ValuePair(newAddress, value);

                    return true;
                }
                else
                {
                    Log.Logger.Error($"ValueManager: Udapte for Variable {id} failed! The new Address {newAddress} could not be registered!");
                }
            }

            return false;
        }

        public bool UpdateValueAddress(string id, string newAddress)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (managedValues.ContainsKey(id))
                {
                    if (managedValues[id].Key != newAddress)
                        return UpdateIPC(id, newAddress);
                }
                else
                {
                    Log.Logger.Debug($"ValueManager: Variable {id} did not exist! Added new Registration to {newAddress}");
                    return RegisterValue(id, newAddress);
                }
            }
            else
            {
                Log.Logger.Error($"ValueManager: Variable {id} does not exist!");
            }

            return false;
        }

        public bool DeregisterValue(string id)
        {
            if (!string.IsNullOrEmpty(id) && managedValues.TryGetValue(id, out ValuePair value))
            {
                ipcManager.DeregisterAddress(value.Key);
                return true;
            }
            else
            {
                Log.Logger.Error($"ValueManager: Variable {id} does not exist!");
            }

            return false;
        }

        public bool ContainsValue(string id)
        {
            if (!string.IsNullOrEmpty(id))
                return managedValues.ContainsKey(id);

            return false;
        }

        public bool IsChanged(string id)
        {
            if (managedValues.TryGetValue(id, out ValuePair value))
                return value.Value.IsChanged;
            else
                return false;
        }

        public bool HasChangedValues()
        {
            foreach (var val in managedValues.Values)
            {
                if (val.Value.IsChanged)
                    return true;
            }

            return false;
        }

        protected string GetValue(string id)
        {
            if (managedValues.TryGetValue(id, out ValuePair value))
                return value.Value.Value;
            else
            {
                if (id != ID.SwitchState && id != ID.SwitchStateLong)
                    Log.Logger.Debug($"ValueManager: Returning empty Value for Variable {id}");
                return "";
            }
        }

        public string this[string id]
        {
            get => (!string.IsNullOrEmpty(id) ? GetValue(id) : "");
        }
    }
}
