using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck
{
    public sealed class IPCManager : IDisposable
    {
        private Dictionary<string, IPCValue> currentValues = new();
        private Dictionary<string, int> currentRegistrations = new();
        public SimulatorConnector SimConnector { get; set; }
 
        public int Length => currentValues.Count;

        public IPCValue this[string address]
        {
            get
            {
                if (currentValues.TryGetValue(address, out IPCValue value))
                    return value;
                else
                    return null;
            }
        }

        public List<string> AddressList { get { return currentValues.Keys.ToList(); } }

        public IPCManager()
        {

        }

        public void Dispose()
        {
            try
            {
                currentValues.Clear();
                currentRegistrations.Clear();
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while removing Registrations!");
            }

            try
            {
                SimConnector.Close();
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while closing Connections");
            }
        }

        public IPCValue RegisterAddress(string address)
        {
            IPCValue value = null;
            try
            {
                if (!IPCTools.IsReadAddress(address))
                {
                    Log.Logger.Error($"IPCManager: Not an Read-Address! [{address}]");
                    return value;
                }

                if (currentValues.TryGetValue(address, out value))
                {
                    currentRegistrations[address]++;
                    Log.Logger.Debug($"IPCManager: Added Registration for Address {address}, Registrations: {currentRegistrations[address]}");
                    if (value == null)
                        Log.Logger.Error($"IPCManager: Registered Address {address} has NULL-Reference Value !!");
                }
                else
                {
                    value = SimulatorConnector.CreateIPCValue(address);

                    if (value != null)
                    {
                        currentValues.Add(address, value);
                        currentRegistrations.Add(address, 1);
                        SimConnector.SubscribeAddress(address);
                        Log.Logger.Debug($"IPCManager: Added Registration for Address {address}, Registrations: {currentRegistrations[address]}");
                    }
                    else
                        Log.Logger.Error($"IPCManager: Registered Address {address} has NULL-Reference Value !!");
                }
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while registering Address {address}");
            }

            return value;
        }

        public void DeregisterAddress(string address)
        {
            try
            { 
                if (!string.IsNullOrEmpty(address) && currentValues.ContainsKey(address))
                {
                    if (currentRegistrations[address] == 1)
                    {
                        currentRegistrations.Remove(address);
                        SimConnector.UnsubscribeAddress(address);
                        currentValues[address].Dispose();
                        currentValues[address] = null;
                        currentValues.Remove(address);

                        Log.Logger.Debug($"DeregisterAddress: Removed Address {address}");
                    }
                    else
                    {
                        currentRegistrations[address]--;
                        Log.Logger.Debug($"DeregisterAddress: Deregistered Address {address}, Registrations open: {currentRegistrations[address]}");
                    }
                }
                else
                    Log.Logger.Error($"DeregisterAddress: Could not find Address {address}");
            }
            catch
            {
                Log.Logger.Error($"DeregisterAddress: Exception while deregistering Address {address}");
            }
        }

        public bool Process()
        {
            bool result;

            try
            {
                if (SimConnector.FirstProcessSuccessfull())
                    SimConnector.SubscribeAllAddresses();

                result = SimConnector.Process();
                foreach (var value in currentValues.Values) //read Lvars
                {
                    value.Process(SimConnector.SimType);
                }
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception in Process() Call");
                result = false;
            }

            return result;
        }

        public bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, string offValue = null)
        {
            bool result = false;

            if (SimConnector.IsConnected && IPCTools.IsWriteAddress(Address, actionType))
            {
                if (actionType == ActionSwitchType.VJOY || actionType == ActionSwitchType.VJOYDRV)
                {
                    Log.Logger.Debug($"IPCManager: Running vJoy Toggle Action for '{Address}', Type: {actionType}");
                    result = IPCTools.VjoyToggle(actionType, Address);
                }
                else
                {
                    //CHANGE: Switch Address (Action) based on NewValue
                    Log.Logger.Debug($"IPCManager: Running Action '{Address}' on Connector '{SimConnector.GetType().Name}'");
                    result = SimConnector.RunAction(Address, actionType, newValue, switchSettings, offValue);
                }
            }
            else
                Log.Logger.Error($"IPCManager: not connected or Address not passed {Address}");

            return result;
        }
    }
}
