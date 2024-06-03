using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PilotsDeck
{
    public class IPCManager : IDisposable
    {
        private Dictionary<string, IPCValue> currentValues = [];
        private Dictionary<string, int> currentRegistrations = [];
        public SimulatorConnector SimConnector { get; set; }

        public ScriptManager ScriptManager { get; protected set; } = null;
        private string lastAircraft = "";
 
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

        public List<string> AddressList { get { return [.. currentValues.Keys]; } }

        public IPCManager()
        {
            ScriptManager = new(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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
            if (IPCTools.rxAvar.IsMatch(address))
            {
                if (!address.StartsWith("(A:"))
                    address = address.Insert(1, "A:");
                address = address.Replace(", ", ",");
            }
            if (IPCTools.rxLuaFunc.IsMatch(address))
            {
                string[] parts = address.Split(':');
                address = $"lua:{parts[1].ToLower().Replace(".lua","")}:{parts[2]}";
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

            return address;
        }

        public IPCValue RegisterAddress(string address, ActionSwitchType type = ActionSwitchType.READVALUE)
        {
            IPCValue value = null;
            try
            {
                address = FormatAddress(address);

                if (IPCTools.rxLuaFunc.IsMatch(address))
                {
                    ScriptManager.RegisterScript(address);
                }

                if (type != ActionSwitchType.LUAFUNC && currentValues.TryGetValue(address, out value))
                {
                    currentRegistrations[address]++;
                    Logger.Log(LogLevel.Debug, "IPCManager:RegisterAddress", $"Added Registration for Address '{address}'. (Registrations: {currentRegistrations[address]})");
                    if (value == null)
                        Logger.Log(LogLevel.Error, "IPCManager:RegisterAddress", $"Registered Address '{address}' has NULL-Reference Value! (Registrations: {currentRegistrations[address]})");
                }
                else if (!string.IsNullOrWhiteSpace(address) && type != ActionSwitchType.LUAFUNC)
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

        public void DeregisterAddress(string address, ActionSwitchType type = ActionSwitchType.READVALUE)
        {
            try
            { 
                address = FormatAddress(address);
                
                if (IPCTools.rxLuaFunc.IsMatch(address))
                {
                    ScriptManager.DeregisterScript(address);
                }

                if (!string.IsNullOrWhiteSpace(address) && currentValues.ContainsKey(address))
                {
                    if (currentRegistrations[address] >= 1)
                    {
                        currentRegistrations[address]--;
                        Logger.Log(LogLevel.Debug, "IPCManager:DeregisterAddress", $"Deregistered Address '{address}'. (Registrations: {currentRegistrations[address]})");
                    }
                }
                else if (type != ActionSwitchType.LUAFUNC)
                    Logger.Log(LogLevel.Error, "IPCManager:DeregisterAddress", $"Could not find Address '{address}'!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:DeregisterAddress", $"Exception while deregistering Address '{address}'! (IsConnected: {SimConnector.IsConnected}) (IsReady: {SimConnector.IsReady}) (Exception: {ex.GetType()})");
            }
        }

        public int UnsubscribeUnusedAddresses()
        {
            int result = 0;
            ScriptManager.RemoveUnusedScripts();

            var unusedAddresses = currentRegistrations.Where(v => v.Value <= 0);

            if (unusedAddresses.Any())
                Logger.Log(LogLevel.Information, "IPCManager:UnsubscribeUnusedAddresses", $"Unsubscribing {unusedAddresses.Count()} unused Addresses ...");

            foreach (var address in unusedAddresses)
            {
                currentRegistrations.Remove(address.Key);
                SimConnector.UnsubscribeAddress(address.Key);
                currentValues[address.Key].Dispose();
                currentValues[address.Key] = null;
                currentValues.Remove(address.Key);
                result++;
                Logger.Log(LogLevel.Debug, "IPCManager:UnsubscribeUnusedAddresses", $"Unsubscribed '{address.Key}' from managed Addresses.");
            }
            SimConnector.UnsubscribeUnusedAddresses();

            return result;
        }

        public void UnloadScripts()
        {
            ScriptManager.StopGlobalScripts();
        }

        public bool Process()
        {
            bool result;

            try
            {
                if (SimConnector.FirstProcessSuccessfull())
                {
                    if (ScriptManager.GlobalScriptsStopped)
                        ScriptManager.StartGlobalScripts();
                    ScriptManager.RegisterAllVariables();
                    SimConnector.SubscribeAllAddresses();
                }
                if (SimConnector.IsReady && ScriptManager.GlobalScriptsStopped)
                    ScriptManager.StartGlobalScripts();

                result = SimConnector.Process();
                foreach (var value in currentValues.Values) //read Lvars
                {
                    value.Process(SimConnector.SimType);
                }

                if (SimConnector.IsReady && !ScriptManager.GlobalScriptsStopped)
                {
                    if (lastAircraft != SimConnector.AicraftString)
                        ScriptManager.StartGlobalScripts();
                    ScriptManager.RunGlobalScripts();
                }

                if (!SimConnector.IsReady && !ScriptManager.GlobalScriptsStopped)
                {
                    if (!SimConnector.IsPaused || lastAircraft != SimConnector.AicraftString)
                        ScriptManager.StopGlobalScripts();
                }

                if (SimConnector.IsReady)
                    lastAircraft = SimConnector.AicraftString;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCManager:Process", $"Exception in Process() Call! (IsConnected: {SimConnector.IsConnected}) (IsReady: {SimConnector.IsReady}) (Exception: {ex.GetType()})");
                result = false;
            }

            return result;
        }

        public bool RunActionDown(string addressOn, ActionSwitchType actionType, string onState, IModelSwitch switchSettings, bool ignoreBaseOptions = false)
        {
            bool result = false;

            if (IPCTools.IsWriteAddress(addressOn, actionType))
            {
                //VJOYs
                if (IPCTools.IsVjoyAddress(addressOn, actionType))
                {
                    //Set
                    if (!IPCTools.IsVjoyToggle(addressOn, actionType) && !ignoreBaseOptions)
                    {
                        Logger.Log(LogLevel.Debug, "IPCManager:RunActionDown", $"Running vJoy Set Action for '{addressOn}'. (Type: {actionType})");
                        result = SimTools.VjoyClearSet(actionType, addressOn, false);
                    }
                    else
                        result = true;
                }
                else if (SimConnector.IsConnected)
                {
                    //HOLD
                    if (!ignoreBaseOptions && switchSettings.HoldSwitch && (IPCTools.IsHoldableValue(actionType) || IPCTools.IsHoldableCommand(actionType)))
                    {
                        Logger.Log(LogLevel.Debug, "IPCManager:RunActionDown", $"HoldSwitch down - Running Action {actionType} '{addressOn}' on Connector '{SimConnector.GetType().Name}' (Value: {onState})");
                        result = SimConnector.RunAction(addressOn, actionType, onState, switchSettings);
                    }
                    else
                        result = true;
                }
                else
                    Logger.Log(LogLevel.Error, "IPCManager:RunActionDown", $"Simulator not connected!");
            }
            else
                Logger.Log(LogLevel.Error, "IPCManager:RunActionDown", $"Address '{addressOn}' is not valid! (IsConnected {SimConnector.IsConnected})");

            return result;
        }

        public bool RunActionUp(string addressOn, string addressOff, ActionSwitchType actionType, string currentState, string onState, string offState, IModelSwitch switchSettings, bool ignoreBaseOptions = false, int encTicks = 1, long downTicks = 1)
        {
            bool result = false;

            if (IPCTools.IsWriteAddress(addressOn, actionType))
            {
                //VJOYs
                if (IPCTools.IsVjoyAddress(addressOn, actionType))
                {
                    //Toggle
                    if (IPCTools.IsVjoyToggle(addressOn, actionType))
                    {
                        int success = 0;
                        int i;
                        for (i = 0; i < encTicks; i++)
                        {
                            Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"Running vJoy Toggle Action {i + 1}/{encTicks} for '{addressOn}'. (Type: {actionType})");
                            if (SimTools.VjoyToggle(actionType, addressOn))
                            {
                                success++;
                                if (encTicks > 1)
                                    Thread.Sleep(AppSettings.controlDelay);
                            }
                        }
                        result = i == success;
                    }
                    //Clear
                    else
                    {
                        Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"Running vJoy Clear Action for '{addressOn}'. (Type: {actionType})");
                        if (downTicks < 1)
                            Thread.Sleep(AppSettings.controlDelay);
                        result = SimTools.VjoyClearSet(actionType, addressOn, true);
                    }
                }
                else if (SimConnector.IsConnected)
                {
                    string runAddress = addressOn;
                    string newValue = onState;
                    
                    bool resetResult = true;

                    //HOLD
                    if (!ignoreBaseOptions && switchSettings.HoldSwitch && (IPCTools.IsHoldableValue(actionType) || IPCTools.IsHoldableCommand(actionType)))
                    {
                        if (IPCTools.IsHoldableCommand(actionType))
                            runAddress = addressOff;
                        newValue = offState;
                    }
                    //TOGGLE
                    else if (!ignoreBaseOptions && switchSettings.ToggleSwitch && IPCTools.IsToggleableCommand(actionType) && !string.IsNullOrEmpty(addressOff))
                    {
                        if (ModelBase.Compare(offState, currentState))
                        {
                            Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"ToggleSwitch OffState matched - using Alternate Action. (offState: {offState}) (State: {currentState})");
                            runAddress = addressOff;
                            if (actionType != ActionSwitchType.BVAR)
                                newValue = offState;
                            else
                                newValue = "1";
                        }
                        else
                            Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"ToggleSwitch OffState not matched - using Normal Action. (offState: {offState}) (State: {currentState})");
                    }
                    //RESET                    
                    else if (switchSettings.UseLvarReset && IPCTools.IsResetableValue(actionType)
                        && !string.IsNullOrWhiteSpace(offState) && onState[0] != '$')
                    {
                        Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"ResetSwitch - Running Action {actionType} '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
                        resetResult = SimConnector.RunAction(runAddress, actionType, newValue, switchSettings);
                        Thread.Sleep(AppSettings.controlDelay * 2);
                        newValue = offState;
                    }
                    //CALCULATE VALUE
                    else if (IPCTools.IsActionReadable(actionType))
                    {
                        if (actionType == ActionSwitchType.BVAR && string.IsNullOrWhiteSpace(offState))
                        {
                            newValue = onState;
                        }
                        else
                        {
                            newValue = ValueManipulator.CalculateSwitchValue(currentState, offState, onState, encTicks);
                            if (!string.IsNullOrEmpty(newValue) && (newValue.StartsWith('>') || newValue.StartsWith('<')))
                                newValue = newValue.Replace("=", "").Replace("<", "").Replace(">", "");
                        }
                    }


                    if (!IPCTools.IsTickAction(actionType, runAddress, newValue))
                    {
                        int success = 0;
                        int i;
                        for (i = 0; i < encTicks; i++)
                        {
                            Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"Running Action {i + 1}/{encTicks} {actionType} '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
                            if (SimConnector.RunAction(runAddress, actionType, newValue, switchSettings))
                            {
                                success++;
                                if (encTicks > 1 && IPCTools.IsHoldableCommand(actionType))
                                    Thread.Sleep(AppSettings.controlDelay / 2);
                            }
                        }
                        result = i == success && resetResult;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, "IPCManager:RunActionUp", $"Running Action {actionType} '{runAddress}' on Connector '{SimConnector.GetType().Name}' (Value: {newValue})");
                        result = SimConnector.RunAction(runAddress, actionType, newValue, switchSettings, encTicks) && resetResult;
                    }
                }
                else
                    Logger.Log(LogLevel.Error, "IPCManager:RunActionUp", $"Simulator not connected!");
            }
            else
                Logger.Log(LogLevel.Error, "IPCManager:RunActionUp", $"Address '{addressOn}' is not valid! (IsConnected {SimConnector.IsConnected})");

            return result;
        }
    }
}
