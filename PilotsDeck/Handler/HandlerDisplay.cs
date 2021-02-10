using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public abstract class HandlerDisplay : HandlerBase, IHandlerValue, IHandlerDisplay
    {
        public ModelDisplay CommonSettings { get; }

        public override string DefaultImage { get { return CommonSettings.DefaultImage; } }
        public override string ErrorImage { get { return CommonSettings.ErrorImage; } }
        public override bool IsInitialized { get { return CommonSettings.IsInitialized; } }

        public virtual string Address { get { return CommonSettings.Address; } }
        public override string ActionID { get { return $"{Title} | Read: {CommonSettings.Address}"; } }

        protected virtual IPCValue ValueRef { get; set; } = null;



        public HandlerDisplay(string context, ModelDisplay settings) : base(context, settings)
        {
            CommonSettings = settings;
        }

        public virtual void SetTitleParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters)
        {
            CommonSettings.SetTitleParameters(titleParameters);
        }

        public override void Update()
        {
            if (!string.IsNullOrEmpty(CommonSettings.Address))
                CommonSettings.IsInitialized = true;
            else
                CommonSettings.IsInitialized = false;

            CommonSettings.Update();
        }

        public virtual void RegisterValue(IPCManager ipcManager)
        {
            ValueRef = RegisterValue(ipcManager, Context, Address);
        }

        public static IPCValue RegisterValue(IPCManager ipcManager, string context, string address)
        {
            return ipcManager.RegisterValue(context, address, AppSettings.groupStringRead);
        }

        public virtual void UpdateValue(IPCManager ipcManager)
        {
            ValueRef = UpdateValue(ipcManager, Context, Address);
        }

        public static IPCValue UpdateValue(IPCManager ipcManager, string context, string address)
        {
            return ipcManager.UpdateValue(context, address, AppSettings.groupStringRead);
        }

        public virtual void DeregisterValue(IPCManager ipcManager)
        {
            DeregisterValue(ipcManager, Context);
        }

        public static void DeregisterValue(IPCManager ipcManager, string context)
        {
            ipcManager.DeregisterValue(context);
        }

        public static string ConvertFromBCD(string value)
        {
            if (!short.TryParse(value, out short numShort))
                return value;

            byte[] numBytes = BitConverter.GetBytes(numShort);
            int numOut = 0;
            for (int i = numBytes.Length - 1; i >= 0; i--)
            {
                numOut *= 100;
                numOut += (10 * (numBytes[i] >> 4));
                numOut += numBytes[i] & 0xf;
            }

            return Convert.ToString(numOut);
        }
    }
}
