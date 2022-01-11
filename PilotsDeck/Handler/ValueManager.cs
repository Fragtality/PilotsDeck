using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace PilotsDeck
{
    using ValuePair = KeyValuePair<string, IPCValue>;

    public static class ID
    {
        public static readonly string SwitchState = "SwitchState";
        public static readonly string SwitchStateLong = "SwitchStateLong";
        
        public static readonly string ControlState = "ControlState";
        
        public static readonly string Top = "ControlState";
        public static readonly string Bot = "ControlSateBot";
        
        public static readonly string Active = "ControlState";
        public static readonly string Standby = "ControlSateBot";

        public static readonly string First = "ControlState";
        public static readonly string Second = "ControlSateBot";
    };

    public class AddressValueManager
    {
        //ID (Current, Last, ...) => Address = Value
        protected Dictionary<string, ValuePair> managedValues = new Dictionary<string, ValuePair>();
        protected Dictionary<string, string> staticValues = new Dictionary<string, string>();
        protected IPCManager ipcManager = null;

        public void RegisterManager(IPCManager manager)
        {
            if (ipcManager == null)
                ipcManager = manager;
        }

        public bool RegisterValue(string id, string address)
        {
            IPCValue value = ipcManager.RegisterAddress(address, AppSettings.groupStringRead);
            
            if (value != null)
            {
                if (!managedValues.ContainsKey(id))
                {
                    managedValues.Add(id, new ValuePair(address, value));
                    return true;
                }
                else
                {
                    Log.Logger.Error($"RegisterValue: Variable {id} already exists!");
                }
            }
            else
            {
                Log.Logger.Error($"RegisterValue: Could not Register Address {address} for Variable {id}!");
            }

            return false;
        }

        protected bool UpdateIPC(string id, string newAddress)
        {
            if (managedValues[id].Key != newAddress)
            {
                ipcManager.DeregisterAddress(managedValues[id].Key);
                IPCValue value = ipcManager.RegisterAddress(newAddress, AppSettings.groupStringRead);

                if (value != null)
                {
                    Log.Logger.Debug($"UpdateIPC: Updated Variable {id} with new Address {newAddress}. (Old: {managedValues[id].Key}");
                    managedValues[id] = new ValuePair(newAddress, value);

                    return true;
                }
                else
                {
                    Log.Logger.Error($"UpdateIPC: Udapte for Variable {id} failed! The new Address {newAddress} could not be registered!");
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
                    Log.Logger.Debug($"UpdateValueAddress: Variable {id} did not exist! Added new Registration to {newAddress}");
                    return RegisterValue(id, newAddress);
                }
            }
            else
            {
                Log.Logger.Error($"UpdateValueAddress: Variable {id} does not exist!");
            }

            return false;
        }

        public bool DeregisterValue(string id)
        {
            if (!string.IsNullOrEmpty(id) && managedValues.ContainsKey(id))
            {
                ipcManager.DeregisterAddress(managedValues[id].Key);
                return true;
            }
            else
            {
                Log.Logger.Error($"DeregisterValue: Variable {id} does not exist!");
            }

            return false;
        }

        public bool ContainsValue(string id)
        {
            if (!string.IsNullOrEmpty(id))
                return managedValues.ContainsKey(id);

            return false;
        }

        public string GetValueManaged(string id)
        {
            if (managedValues.ContainsKey(id))
                return managedValues[id].Value.Value;
            else
                return null;
        }

        public bool IsChanged(string id)
        {
            if (managedValues.ContainsKey(id))
                return managedValues[id].Value.IsChanged;
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

        public void SetVariable(string id, string value)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (!staticValues.ContainsKey(id))
                {
                    Log.Logger.Debug($"SetVariable: Added new static Variable {id}");
                    staticValues.Add(id, value);
                }
                else
                {
                    Log.Logger.Debug($"SetVariable: static Variable {id} was updated");
                    staticValues[id] = value;
                }
            }
            else
            {
                Log.Logger.Error($"SetVariable: Could not set Variable - the id was empty!");
            }
        }

        public string GetVariable(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (staticValues.ContainsKey(id))
                    return staticValues[id];
                else
                    Log.Logger.Error($"GetVariable: Variable {id} does not exist!");
            }
            else
            {
                Log.Logger.Error($"GetVariable: Could not get Variable - the id was empty!");
            }

            Log.Logger.Debug($"GetVariable: Returning empty Value for static Variable {id}");
            return "";
        }

        public void RemoveVariable(string id)
        {
            if (!string.IsNullOrEmpty(id) && staticValues.ContainsKey(id))
            {
                staticValues.Remove(id);
                Log.Logger.Debug($"RemoveVariable: Removed static Variable {id}");
            }
        }

        public bool ContainsVariable(string id)
        {
            if (!string.IsNullOrEmpty(id))
                return staticValues.ContainsKey(id);
            else
                return false;
        }

        protected string GetValue(string id)
        {
            if (managedValues.ContainsKey(id))
                return managedValues[id].Value.Value;
            else if (staticValues.ContainsKey(id))
                return staticValues[id];
            else
            {
                Log.Logger.Debug($"GetValue: Returning empty Value for Variable {id}");
                return "";
            }
        }

        protected void SetValue(string id, string value)
        {
            if (!string.IsNullOrEmpty(id) && staticValues.ContainsKey(id))
                staticValues[id] = value;
        }

        public string this[string id]
        {
            get => (!string.IsNullOrEmpty(id) ? GetValue(id) : "");
            set => SetValue(id, value);
        }
    }
}
