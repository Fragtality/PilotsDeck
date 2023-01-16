using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

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
                {
                    Log.Logger.Error("$IPCManager: Tried to access non-existant Address {address}! Registering ...");
                    return RegisterAddress(address);
                }
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
                    if (currentRegistrations[address] >= 1)
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

        public void UnsubscribeUnusedAddresses()
        {
            var unusedAddresses = currentRegistrations.Where(v => v.Value <= 0);

            foreach (var address in unusedAddresses)
            {
                currentRegistrations.Remove(address.Key);
                SimConnector.UnsubscribeAddress(address.Key);
                currentValues[address.Key].Dispose();
                currentValues[address.Key] = null;
                currentValues.Remove(address.Key);

                Log.Logger.Debug($"UnsubscribeUnusedAddresses: Removed Address {address}");
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

        public static string CalculateSwitchValue(string lastValue, string offState, string onState, int ticks)
        {
            if (!string.IsNullOrEmpty(onState) && onState[0] == '$' && double.TryParse(lastValue, NumberStyles.Number, new RealInvariantFormat(lastValue), out _))
            {
                ValueManipulator valueManipulator = new();
                string newValue = valueManipulator.GetValue(lastValue, onState, ticks);
                Log.Logger.Debug($"IPCManager: Value calculated {lastValue} -> {newValue}");
                return newValue;
            }
            else
                return ToggleSwitchValue(lastValue, offState, onState);
        }

        public static string ToggleSwitchValue(string lastValue, string offState, string onState)
        {
            string newValue;
            if (lastValue == offState)
                newValue = onState;
            else
                newValue = offState;
            Log.Logger.Debug($"IPCManager: Value toggled {lastValue} -> {newValue}");
            return newValue;
        }

        public bool RunAction(string addressOn, string addressOff, ActionSwitchType actionType, string switchState, string controlState, string onValue, string offValue, IModelSwitch switchSettings, bool ignoreLvarReset = false, int ticks = 1)
        {
            bool result = false;

            if (SimConnector.IsConnected && IPCTools.IsWriteAddress(addressOn, actionType))
            {
                if (actionType == ActionSwitchType.VJOY || actionType == ActionSwitchType.VJOYDRV)
                {
                    Log.Logger.Debug($"IPCManager: Running vJoy Toggle Action for '{addressOn}', Type: {actionType}");
                    result = IPCTools.VjoyToggle(actionType, addressOn);
                }
                else
                {
                    string runAddress = addressOn;
                    if (switchSettings.ToggleSwitch && !string.IsNullOrEmpty(addressOff) && ModelBase.Compare(offValue, controlState))
                    {
                        runAddress = addressOff;
                    }
                    
                    string newValue = "";
                    if (HandlerBase.IsActionReadable(actionType))
                    {
                        if (!ignoreLvarReset && switchSettings.UseLvarReset && onValue[0] != '$')
                            newValue = onValue;
                        else
                            newValue = CalculateSwitchValue(switchState, offValue, onValue, ticks);
                    }

                    if (!HandlerBase.IsActionReadable(actionType) && actionType != ActionSwitchType.CALCULATOR)
                    {
                        int success = 0;
                        int i;
                        for (i = 0; i < ticks; i++)
                        {
                            Log.Logger.Debug($"IPCManager: Running Actions {i+1}/{ticks} '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
                            if (SimConnector.RunAction(runAddress, actionType, newValue, switchSettings, ignoreLvarReset, offValue))
                            {
                                success++;
                                if (actionType == ActionSwitchType.MACRO || actionType == ActionSwitchType.SCRIPT || actionType == ActionSwitchType.XPCMD)
                                    Thread.Sleep(AppSettings.controlDelay / 2);
                            }
                        }
                        result = i == success;
                    }
                    else
                    {
                        Log.Logger.Debug($"IPCManager: Running Action '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
                        result = SimConnector.RunAction(runAddress, actionType, newValue, switchSettings, ignoreLvarReset, offValue, ticks);
                    }
                }
            }
            else
                Log.Logger.Error($"IPCManager: not connected or Address not passed {addressOn}");

            return result;
        }
    }
}
