using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public abstract class HandlerValue : HandlerBase, IHandlerValue //, IHandlerDisplay
    {
        //public ModelDisplay CommonSettings { get; }

        //public override string DefaultImage { get { return CommonSettings.DefaultImage; } }
        //public override string ErrorImage { get { return CommonSettings.ErrorImage; } }
        //public override bool IsInitialized { get { return CommonSettings.IsInitialized; } }

        //public virtual string Address { get { return CommonSettings.Address; } }
        //public override string ActionID { get { return $"{Title} | Read: {CommonSettings.Address}"; } }
        
        public override string ActionID { get { return $"{Title} | Read: {Address}"; } }

        //public override bool NeedRegistration { get; } = true;
        //protected virtual IPCValue ValueRef { get; set; } = null;
        public virtual string CurrentValue { get; protected set; } = null;
        public virtual string LastAddress { get; protected set; }
        public virtual bool IsChanged { get; protected set; } = false;


        public HandlerValue(string context, ModelDisplay settings) : base(context, settings)
        {
            
        }

        //public virtual void SetTitleParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters)
        //{
        //    CommonSettings.SetTitleParameters(titleParameters);
        //}

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
            //ValueRef = RegisterValue(ipcManager, Context, Address);
            ipcManager.RegisterAddress(Address, AppSettings.groupStringRead);
            LastAddress = Address;
        }

        //public static IPCValue RegisterValue(IPCManager ipcManager, string context, string address)
        //{
        //    return ipcManager.RegisterValue(context, address, AppSettings.groupStringRead);
        //}

        public virtual void UpdateAddress(IPCManager ipcManager)
        {
            //ValueRef = UpdateValue(ipcManager, Context, Address);
            LastAddress = UpdateAddress(ipcManager, LastAddress, Address);
        }

        //public static IPCValue UpdateValue(IPCManager ipcManager, string context, string address)
        //{
        //    return ipcManager.UpdateValue(context, address, AppSettings.groupStringRead);
        //}

        public virtual void DeregisterAddress(IPCManager ipcManager)
        {
            ipcManager.DeregisterValue(Address);
            if (Address != LastAddress)
                throw new Exception($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Address} != {LastAddress} ] ");
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

        //public static void DeregisterValue(IPCManager ipcManager, string context)
        //{
        //    ipcManager.DeregisterValue(context);
        //}
    }
}
