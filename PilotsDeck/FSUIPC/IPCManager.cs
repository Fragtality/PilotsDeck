using System;
using System.Collections.Generic;
using FSUIPC;
using Serilog;

namespace PilotsDeck
{
    public class IPCManager : IDisposable
    {
        private Dictionary<string, IPCValue> currentValues = new Dictionary<string, IPCValue>();
        private Dictionary<string, string> currentRegistrations = new Dictionary<string, string>();
        private static readonly string inMenuAddr = "3365:1";
        private IPCValueOffset inMenuValue;

        public bool IsConnected
        {
            get
            {
                return FSUIPCConnection.IsOpen;
            }
        }

        public bool IsReady
        {
            get
            {
                return inMenuValue.Value == "0";
            }
        }

        public int Length => currentValues.Count;

        public IPCValue this[string context]
        {
            get
            {
                if (currentRegistrations.ContainsKey(context) && currentValues.ContainsKey(currentRegistrations[context]))
                    return currentValues[currentRegistrations[context]];
                else
                    return null;
            }
        }

        public IPCManager(string group)
        {
            inMenuValue = new IPCValueOffset(inMenuAddr, group);
            currentValues.Add(inMenuAddr, inMenuValue);
        }       

        public bool Connect()
        {
            try
            {
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Log.Logger.Information("IPCManager: FSUIPC Connected");
                }
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while opening FSUIPC");
            }

            return IsConnected;
        }

        public void Dispose()
        {
            Close();
        }

        public bool Close()
        {
            try
            {
                foreach (var value in currentValues.Values)
                    value.Dispose();
                currentValues.Clear();
                currentRegistrations.Clear();

                FSUIPCConnection.Close();

                if (!FSUIPCConnection.IsOpen)
                {
                    Log.Logger.Information("IPCManager: FSUIPC Closed");
                }
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while closing FSUIPC");
            }

            return !IsConnected;
        }

        public IPCValue RegisterValue(string context, string address, string group)
        {
            IPCValue value = null;
            if (!IPCTools.IsReadAddress(address))
                return value;

            if (currentValues.ContainsKey(address) && !currentRegistrations.ContainsKey(context)) //address Registered, button Unregistered
            {
                value = currentValues[address];
                currentRegistrations.Add(context, address);
            }
            else if (!currentValues.ContainsKey(address) && !currentRegistrations.ContainsKey(context)) //address Unregistered, button Unregistered
            {
                if (IPCTools.rxOffset.IsMatch(address))
                    value = new IPCValueOffset(address, group);
                else
                    value = new IPCValueLvar(address);

                currentValues.Add(address, value);
                currentRegistrations.Add(context, address);
            }
            else
                Log.Logger.Verbose($"RegisterValue: Context already registered! ({context} | {address})");

            return value;
        }

        public void DeregisterValue(string context)
        {
            if (currentRegistrations.ContainsKey(context)) //button Registered
            {
                string address = currentRegistrations[context];
                currentRegistrations.Remove(context);

                if (!currentRegistrations.ContainsValue(address)) //no other Button Registered for that Address
                {
                    currentValues[address].Dispose();
                    currentValues[address] = null;
                    currentValues.Remove(address);
                }
            }
            else
                Log.Logger.Error($"DeregisterValue: Could not find Context {context}");
        }

        public IPCValue UpdateValue(string context, string newAddress, string group)
        {
            if (!IPCTools.IsReadAddress(newAddress))
                return null;

            DeregisterValue(context);
            return RegisterValue(context, newAddress, group);
        }

        public bool Process(string group)
        {
            try
            {
                FSUIPCConnection.Process(group); //should update all offsets in currentOffsets (type OFFSETx and SCRIPT)
                if (!IsReady)
                    return false;

                foreach (var value in currentValues.Values) //read Lvars
                    value.Process();

                return true;
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while process call to FSUIPC");
                return false;
            }
        }

        public bool ProcessWithConnect(string group)
        {
            if (!IsConnected)
            {
                if (Connect())
                    return Process(group);
                else
                    return false;
            }
            else
                return Process(group);
        }

        public bool WriteOffset(string address, string value)
        {
            bool result = false;
            IPCValueOffset offset = null;
            try
            {
                offset = new IPCValueOffset(address, AppSettings.groupStringWrite, OffsetAction.Write);
                offset.Write(value, AppSettings.groupStringWrite);
                offset.Dispose();
                offset = null;
                result = true;
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while writing Offset <{address}> (size:{offset?.Size}/float:{offset?.IsFloat}/string:{offset?.IsString}/signed:{offset?.IsSigned}) to FSUIPC");
                if (offset != null)
                {
                    offset.Dispose();
                    offset = null;
                }
            }

            return result;
        }

        public bool WriteLvar(string name, double value)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.WriteLVar(name, value);
                result = true;
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while writing LVar <{name}:{value}> to FSUIPC");
            }

            return result;
        }

        public bool RunScriptMacro(string name)
        {
            try
            {
                Offset request = new Offset(0x0D70, 128);
                request.SetValue(name);
                FSUIPCConnection.Process();
                request.Disconnect();
                request = null;
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while Executing Macro/Script: {name}");
                return false;
            }

            return true;
        }

        public bool SendControl(string control, string param = "0")
        {
            return SendControl(Convert.ToInt32(control), Convert.ToInt32(param));
        }

        public bool SendControl(int control, int param = 0)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.SendControlToFS(control, param);
                    result = true;
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while sending Control <{control}:{param}> to FSUIPC");
            }

            return result;
        }
    }
}
