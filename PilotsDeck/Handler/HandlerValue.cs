﻿using Serilog;

namespace PilotsDeck
{
    public abstract class HandlerValue : HandlerBase, IHandlerValue
    {        
        public override string ActionID { get { return $"\"{Title}\" [HandlerValue] Read: {Address}"; } }

        public virtual string CurrentValue { get; protected set; } = null;
        public virtual string LastAddress { get; protected set; }
        public virtual bool IsChanged { get; protected set; } = false;


        public HandlerValue(string context, ModelDisplay settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            
        }

        public virtual void RefreshValue(IPCManager ipcManager)
        {
            IsChanged = RefreshValue(ipcManager, Address, out string currentValue);
            CurrentValue = currentValue;
        }

        public static bool RefreshValue(IPCManager ipcManager, string address, out string value)
        {
            value = null;

            if (!string.IsNullOrEmpty(address))
            {
                IPCValue ipcValue = ipcManager[address];
                value = ipcValue?.Value;
                if (ipcValue != null)
                    return ipcValue.IsChanged;
                else
                    return false;

            }
            else
                return false;
        }

        public virtual void RegisterAddress(IPCManager ipcManager)
        {
            ipcManager.RegisterAddress(Address, AppSettings.groupStringRead);
            LastAddress = Address;
        }

        public virtual void UpdateAddress(IPCManager ipcManager)
        {
            LastAddress = UpdateAddress(ipcManager, LastAddress, Address);
        }

        public virtual void DeregisterAddress(IPCManager ipcManager)
        {
            ipcManager.DeregisterValue(Address);
            if (Address != LastAddress)
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Address} != {LastAddress} ] ");
        }

        public static string UpdateAddress(IPCManager ipcManager, string lastAddress, string address)
        {
            if (lastAddress != address)
            {
                ipcManager.DeregisterValue(lastAddress);
                ipcManager.RegisterAddress(address, AppSettings.groupStringRead);
                return address;
            }
            else
                return lastAddress;
        }
    }
}
