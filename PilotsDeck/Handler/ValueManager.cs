﻿using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck
{
    public class ValueManager(IPCManager m)
    {
        protected Dictionary<int, ManagedValue> ManagedValues = [];
        protected Dictionary<string, IPCValue> DynamicValues = [];
        protected IPCManager ipcManager = m;

        #region Add/Remove/Update
        public void AddValue(int id, string address, int type = 5)
        {
            AddValue(id, address, (ActionSwitchType)type);
        }

        public void AddValue(int id, string address)
        {
            AddValue(id, address, ActionSwitchType.READVALUE);
        }

        public void AddValue(int id, string address, ActionSwitchType type = ActionSwitchType.READVALUE)
        {
            if (!ManagedValues.ContainsKey(id))
            {
                var value = new ManagedValue(id, address, type);
                IPCValue ipcValue;

                if (IPCTools.IsActionReadable(type) && IPCTools.IsReadAddressForType(address, type))
                {
                    ipcValue = ipcManager.RegisterAddress(address, type);
                    if (ipcValue != null)
                    {
                        value.Value = ipcValue;
                        Logger.Log(LogLevel.Debug, "ValueManager:AddValue", $"The IPCValue for ID '{ID.str(id)}' was added. {Logger.ActionInfo(address, type)}");
                    }
                    else if (type != ActionSwitchType.LUAFUNC)
                        Logger.Log(LogLevel.Error, "ValueManager:AddValue", $"The IPCValue for ID '{ID.str(id)}' is a NULL-Reference! {Logger.ActionInfo(address, type)}");
                }
                ManagedValues.Add(id, value);
                Logger.Log(LogLevel.Verbose, "ValueManager:AddValue", $"The Value ID '{ID.str(id)}' is now managed. {Logger.ActionInfo(address, type)}");
            }
            else
            {
                Logger.Log(LogLevel.Error, "ValueManager:AddValue", $"The Value for ID '{ID.str(id)}' is already registered! {Logger.ActionInfo(address, type)}");
            }
        }

        public void AddDynamicValue(string id)
        {
            if (!DynamicValues.ContainsKey(id))
            {
                if (ipcManager.TryGetValue(id, out IPCValue value))
                {
                    DynamicValues.Add(id, value);
                    Logger.Log(LogLevel.Debug, "ValueManager:AddDynamicValue", $"Tge IPC Value for ID '{id}' was registered");
                }
                else
                {
                    Logger.Log(LogLevel.Error, "ValueManager:AddDynamicValue", $"No IPC Value for ID '{id}'!");
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "ValueManager:AddDynamicValue", $"The IPC Value for ID '{id}' is already registered!");
            }
        }

        public void RemoveValue(int id)
        {
            if (ManagedValues.TryGetValue(id, out var value))
            {
                if (IPCTools.IsActionReadable(value.Type) && (ipcManager.Contains(value.Address) || value.Type == ActionSwitchType.LUAFUNC))
                    ipcManager.DeregisterAddress(value.Address, value.Type);
                
                ManagedValues.Remove(id);
                Logger.Log(LogLevel.Debug, "ValueManager:RemoveValue", $"The Value for ID '{ID.str(id)}' was removed. {Logger.ActionInfo(value.Address, value.Type)}");
            }
            else
            {
                Logger.Log(LogLevel.Error, "ValueManager:RemoveValue", $"The Value for ID '{ID.str(id)}' does not exist!");
            }
        }

        public void RemoveDynamicValue(string id)
        {
            if (DynamicValues.Remove(id))
            {
                Logger.Log(LogLevel.Debug, "ValueManager:RemoveDynamicValue", $"The IPC Value for ID '{id}' was removed");
            }
            else
            {
                Logger.Log(LogLevel.Error, "ValueManager:RemoveDynamicValue", $"The IPC Value for ID '{id}' is not registered!");
            }
        }

        public void UpdateValue(int id, string newAddress, int newType = 5)
        {
            UpdateValue(id, newAddress, (ActionSwitchType)newType);
        }

        public void UpdateValue(int id, string newAddress)
        {
            UpdateValue(id, newAddress, ActionSwitchType.READVALUE);
        }

        public void UpdateValue(int id, string newAddress, ActionSwitchType newType = ActionSwitchType.READVALUE)
        {
            if (ManagedValues.TryGetValue(id, out var value))
            {
                bool updated = false;
                if (value.Address == newAddress && value.Type == newType)
                    return;

                bool readOld = IPCTools.IsActionReadable(value.Type);
                bool readNew = IPCTools.IsActionReadable(newType);
                bool validOld = IPCTools.IsReadAddressForType(value.Address, value.Type);
                bool validNew = IPCTools.IsReadAddressForType(newAddress, newType);

                if ((!readOld && !readNew) || (readOld && !validOld && !validNew))
                    return;

                if (readOld && validOld)
                {
                    if (ipcManager.Contains(value.Address) || value.Type == ActionSwitchType.LUAFUNC)
                        ipcManager.DeregisterAddress(value.Address);
                    value.Value = null;
                    updated = true;
                }

                if ((readNew && validNew) || (!readOld && readNew && validNew))
                {
                    value.Value = ipcManager.RegisterAddress(newAddress, newType);
                    if (value.Value == null)
                        Logger.Log(LogLevel.Error, "ValueManager:UpdateValue", $"The IPCValue for ID '{ID.str(id)}' is a NULL-Reference! {Logger.ActionInfo(newAddress, newType)}");
                    updated = true;
                }

                if (updated)
                {
                    Logger.Log(LogLevel.Debug, "ValueManager:UpdateValue", $"The Value for ID '{ID.str(id)}' was updated. {Logger.ActionInfo(value.Address, value.Type)} -> {Logger.ActionInfo(newAddress, newType)}");
                    value.Address = newAddress;
                    value.Type = newType;
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "ValueManager:UpdateValue", $"The Value for ID '{ID.str(id)}' does not exist!");
            }
        }       
        #endregion

        #region Value Properties
        public bool Contains(int id)
        {
            return ManagedValues.ContainsKey(id);
        }

        public bool HasRegisteredValue(int id)
        {
            return ManagedValues.TryGetValue(id, out var value) && value != null;
        }
        #endregion

        #region IPCValue
        public bool HasChangedValues()
        {
            return ManagedValues.Any(kv => kv.Value.Value != null && kv.Value.Value.IsChanged) || DynamicValues.Any(kv => kv.Value != null && kv.Value.IsChanged);
        }

        public bool IsChanged(int id)
        {
            return ManagedValues.TryGetValue(id, out var value) && value.Value != null && value.Value.IsChanged;
        }

        public bool IsChanged(string id)
        {
            return DynamicValues.TryGetValue(id, out var value) && value != null && value.IsChanged;
        }

        public string GetValue(int id)
        {
            if (ManagedValues.TryGetValue(id, out var value) && value.Value != null)
                return value.Value.Value;
            else
                return "";
        }

        public void SetValue(int id, IPCValue value)
        {
            if (ManagedValues.ContainsKey(id))
                ManagedValues[id].Value = value;
        }

        public string this[int id]
        {
            get { return GetValue(id); }
        }
        #endregion
    }
}
