using System;
using System.Collections.Generic;
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
                if (currentValues.TryGetValue(FormatAddress(address), out IPCValue value))
                    return value;
                else
                    return null;
            }
        }

        public bool TryGetValue(string address, out IPCValue value)
        {
            value = this[address];
            return value != null;
        }

        public bool Contains(string address)
        {
            return currentValues.ContainsKey(FormatAddress(address));
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:Dispose", $"Exception while removing Registrations! (Exception: {ex.GetType()})");
            }

            try
            {
                SimConnector.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:Dispose", $"Exception while closing Connections! (Exception: {ex.GetType()})");
            }
        }

        public static string FormatAddress(string address, bool mobi = false)
        {
            if (string.IsNullOrWhiteSpace(address))
                return address;

            if (IPCTools.rxOffset.IsMatch(address))
            {
                string[] parts = address.Split(':');
                int idx = 0;
                if (parts[0].StartsWith("0x"))
                    idx = 2;
                string sub = parts[0].Substring(idx, 4).ToUpper();
                parts[0] = "0x" + sub;
                address = string.Join(":", parts);
            }
            if (IPCTools.rxLvar.IsMatch(address) && !mobi && address.StartsWith("L:"))
            {
                address = address[2..];
            }
            if (IPCTools.rxLvar.IsMatch(address) && mobi)
            {
                if (!address.StartsWith("L:"))
                    address = address.Insert(0, "L:");
                address = $"({address})";
                
            }
            if (IPCTools.rxAvar.IsMatch(address))
            {
                if (!address.StartsWith("(A:"))
                    address = address.Insert(1, "A:");
                address = address.Replace(", ", ",");
            }

            return address;
        }

        public IPCValue RegisterAddress(string address)
        {
            IPCValue value = null;
            try
            {
                address = FormatAddress(address);
                if (currentValues.TryGetValue(address, out value))
                {
                    currentRegistrations[address]++;
                    Logger.Log(LogLevel.Debug, "IPCManager:RegisterAddress", $"Added Registration for Address '{address}'. (Registrations: {currentRegistrations[address]})");
                    if (value == null)
                        Logger.Log(LogLevel.Error, "IPCManager:RegisterAddress", $"Registered Address '{address}' has NULL-Reference Value! (Registrations: {currentRegistrations[address]})");
                }
                else if (!string.IsNullOrWhiteSpace(address))
                {
                    value = SimulatorConnector.CreateIPCValue(address);

                    if (value != null)
                    {
                        currentValues.Add(address, value);
                        currentRegistrations.Add(address, 1);
                        SimConnector.SubscribeAddress(address);
                        Logger.Log(LogLevel.Debug, "IPCManager:RegisterAddress", $"Subscribed Value for Address '{address}'. (Registrations: {currentRegistrations[address]})");
                    }
                    else
                        Logger.Log(LogLevel.Error, "IPCManager:RegisterAddress", $"SimConnector returned NULL-Reference for Address '{address}'!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:RegisterAddress", $"Exception while registering Address '{address}'! (IsConnected: {SimConnector.IsConnected}) (IsReady: {SimConnector.IsReady}) (Exception: {ex.GetType()})");
            }

            return value;
        }

        public void DeregisterAddress(string address)
        {
            try
            { 
                address = FormatAddress(address);
                if (!string.IsNullOrWhiteSpace(address) && currentValues.ContainsKey(address))
                {
                    if (currentRegistrations[address] >= 1)
                    {
                        currentRegistrations[address]--;
                        Logger.Log(LogLevel.Debug, "IPCManager:DeregisterAddress", $"Deregistered Address '{address}'. (Registrations: {currentRegistrations[address]})");
                    }
                }
                else
                    Logger.Log(LogLevel.Error, "IPCManager:DeregisterAddress", $"Could not find Address '{address}'!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:DeregisterAddress", $"Exception while deregistering Address '{address}'! (IsConnected: {SimConnector.IsConnected}) (IsReady: {SimConnector.IsReady}) (Exception: {ex.GetType()})");
            }
        }

        public void UnsubscribeUnusedAddresses()
        {
            var unusedAddresses = currentRegistrations.Where(v => v.Value <= 0);

            if (unusedAddresses.Any() )
                Logger.Log(LogLevel.Information, "IPCManager:UnsubscribeUnusedAddresses", $"Unsubscribing {unusedAddresses.Count()} unused Addresses ...");

            foreach (var address in unusedAddresses)
            {
                currentRegistrations.Remove(address.Key);
                SimConnector.UnsubscribeAddress(address.Key);
                currentValues[address.Key].Dispose();
                currentValues[address.Key] = null;
                currentValues.Remove(address.Key);

                Logger.Log(LogLevel.Debug, "IPCManager:UnsubscribeUnusedAddresses", $"Unsubscribed '{address.Key}' from managed Addresses.");
            }
            SimConnector.UnsubscribeUnusedAddresses();
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:Process", $"Exception in Process() Call! (IsConnected: {SimConnector.IsConnected}) (IsReady: {SimConnector.IsReady}) (Exception: {ex.GetType()})");
                result = false;
            }

            return result;
        }

        public bool RunAction(string addressOn, string addressOff, ActionSwitchType actionType, string currentState, string onValue, string offValue, IModelSwitch switchSettings, bool ignoreLvarReset = false, int ticks = 1)
        {
            bool result = false;

            if (SimConnector.IsConnected && IPCTools.IsWriteAddress(addressOn, actionType))
            {
                if (actionType == ActionSwitchType.VJOY || actionType == ActionSwitchType.VJOYDRV)
                {
                    Logger.Log(LogLevel.Debug, "IPCManager:RunAction", $"Running vJoy Toggle Action for '{addressOn}'. (Type: {actionType})");
                    result = SimTools.VjoyToggle(actionType, addressOn);
                }
                else
                {
                    string runAddress = addressOn;
                    Logger.Log(LogLevel.Debug, "IPCManager:RunAction", $"offVale {offValue} State {currentState} Compare {ModelBase.Compare(offValue, currentState)}");
                    if (switchSettings.ToggleSwitch && !string.IsNullOrEmpty(addressOff) && ModelBase.Compare(offValue, currentState))
                    {
                        runAddress = addressOff;
                    }
                    
                    string newValue = "";
                    if (IPCTools.IsActionReadable(actionType))
                    {
                        if (!ignoreLvarReset && switchSettings.UseLvarReset && onValue[0] != '$')
                            newValue = onValue;
                        else
                            newValue = ValueManipulator.CalculateSwitchValue(currentState, offValue, onValue, ticks);
                    }

                    if (!IPCTools.IsActionReadable(actionType) && actionType != ActionSwitchType.CALCULATOR)
                    {
                        int success = 0;
                        int i;
                        for (i = 0; i < ticks; i++)
                        {
                            Logger.Log(LogLevel.Debug, "IPCManager:RunAction", $"Running Action {actionType} {i + 1}/{ticks} '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
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
                        Logger.Log(LogLevel.Debug, "IPCManager:RunAction", $"Running Action {actionType} '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
                        result = SimConnector.RunAction(runAddress, actionType, newValue, switchSettings, ignoreLvarReset, offValue, ticks);
                    }
                }
            }
            else
                Logger.Log(LogLevel.Error, "IPCManager:RunAction", $"IPCManager: Address '{addressOn}' is not valid! (IsConnected {SimConnector.IsConnected})");

            return result;
        }
    }
}
