using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PilotsDeck
{
    public class MobiVariables(IPCManager manager, MobiSimConnect mobiSimConnect)
    {
        protected readonly int reorderThreshold = AppSettings.reorderThreshold;
        public const uint DEFINE_ID_OFFSET = 2;
        public const uint DEFINE_ID_OFFSET_STR = 10001;

        protected IPCManager IPCManager { get; } = manager;
        protected MobiSimConnect mobiSimConnect { get; } = mobiSimConnect;
        protected Dictionary<string, uint> addressToIndex { get; } = [];
        protected Dictionary<uint, IPCValue> simVars { get; } = [];
        protected Dictionary<uint, bool> simVarUsed { get; } = [];
        protected Dictionary<uint, bool> clientDataRegistered { get; } = [];
        protected HashSet<uint> subscribeQueue { get; } = [];

        protected uint nextVarID { get; set; } = DEFINE_ID_OFFSET;
        protected uint nextStringVarID { get; set; } = DEFINE_ID_OFFSET_STR;
        

        public bool IsIdUsed(uint id)
        {
            return simVars.TryGetValue(id, out IPCValue ipcValue) && ipcValue != null;
        }

        public string GetAddress(uint id)
        {
            if (simVars.TryGetValue(id, out IPCValue ipcValue) && ipcValue != null)
                return ipcValue.Address;
            else
                return "";
        }

        protected static string GetObjectData(uint id, object[] dwData)
        {
            if (id < DEFINE_ID_OFFSET_STR)
                return ((ClientDataValue)dwData[0]).data.ToString();
            else
                return ((ClientDataStringValue)dwData[0]).data.ToString();
        }

        protected static void SetIpcValue(uint id, IPCValue ipcValue, object[] dwData)
        {
            if (id < DEFINE_ID_OFFSET_STR)
                ipcValue.SetValue(((ClientDataValue)dwData[0]).data);
            else
                ipcValue.SetValue(((ClientDataStringValue)dwData[0]).data);
        }

        public void UpdateId(uint id, object[] dwData)
        {
            if (simVars.TryGetValue(id, out IPCValue ipcValue))
            {
                if (ipcValue != null)
                {
                    SetIpcValue(id, ipcValue, dwData);
                }
                else
                    Logger.Log(LogLevel.Error, "MobiVariables:UpdateId", $"The Value for ID '{id}' is NULL! (Value: {GetObjectData(id, dwData)})");
            }
            else
                Logger.Log(LogLevel.Warning, "MobiVariables:UpdateId", $"The received ID '{id}' has no IPCValue! (Value: {GetObjectData(id, dwData)})");
        }

        protected uint GetNextID(string address)
        {
            if (IPCTools.rxAvarMobiString.IsMatch(address))
                return nextStringVarID;
            else
                return nextVarID;
        }

        protected void IncreaseID(string address)
        {
            if (IPCTools.rxAvarMobiString.IsMatch(address))
                nextStringVarID++;
            else
                nextVarID++;
        }

        public void SubscribeAddress(string address, IPCValue ipcValue)
        {
            try
            {
                if (ipcValue == null)
                {
                    Logger.Log(LogLevel.Critical, "MobiVariables:SubscribeAddress", $"The passed IPC Value is NULL!");
                    return;
                }
                if (nextVarID == 9999)
                {
                    Logger.Log(LogLevel.Critical, "MobiVariables:SubscribeAddress", $"Maximum Subscription Limit reached, can not subscribe any more SimVars!");
                    return;
                }
                
                uint next = GetNextID(address);
                if (addressToIndex.TryAdd(address, next))
                {
                    simVars.TryAdd(next, ipcValue);
                    simVarUsed.Add(next, true);
                    subscribeQueue.Add(next);
                    IncreaseID(address);
                }
                else if (addressToIndex.TryGetValue(address, out uint id) && !simVarUsed[id])
                {
                    simVarUsed[id] = true;
                    simVars[id] = ipcValue;
                }
                else
                    Logger.Log(LogLevel.Error, "MobiVariables:SubscribeAddress", $"Error subscribing Address '{address}' not added / already in Use! (contains: {addressToIndex.ContainsKey(address)} | used: {simVarUsed[id]})");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiVariables:SubscribeAddress", $"Exception while subscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public static Enum GetClientDataId(uint id)
        {
            if (id < DEFINE_ID_OFFSET_STR)
                return PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS;
            else
                return PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_STRINGVARS;
        }

        public void UnsubscribeAddress(string address)
        {
            try
            {
                address = IPCManager.FormatAddress(address);
                bool found = addressToIndex.TryGetValue(address, out uint id);
                bool used = simVarUsed.TryGetValue(id, out bool result) && result;
                if (found && used)
                {
                    simVarUsed[id] = false;
                }
                else
                    Logger.Log(LogLevel.Error, "MobiVariables:UnsubscribeAddress", $"The Address '{address}' does not have an ID mapped or already unused! (found: {found}) (used: {used})");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiVariables:UnsubscribeAddress", $"Exception while unsubscribing SimVar '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public void SubscribeQueue()
        {
            foreach (uint id in subscribeQueue)
            {
                if (simVars.TryGetValue(id, out IPCValue value) && value != null)
                {
                    string address = IPCManager.FormatAddress(value.Address);

                    if (address.StartsWith("C:"))
                        RegisterVariableNumber(id, address[2..]);
                    else if (id >= DEFINE_ID_OFFSET_STR)
                        RegisterVariableString(id, address);
                    else
                        RegisterVariableNumber(id, address);
                }
                else
                    Logger.Log(LogLevel.Error, "MobiVariables:RegisterQueuedVariables", $"The ID '{id}' is not used or was not found or is null! (null: {value == null})");
            }

            if (subscribeQueue.Count > 0)
            {
                Logger.Log(LogLevel.Information, "MobiVariables:RegisterQueuedVariables", $"Subscribed {subscribeQueue.Count} SimVars from Queue.");
                subscribeQueue.Clear();
            }
        }

        protected void RegisterVariableNumber(uint ID, string address)
        {
            Logger.Log(LogLevel.Verbose, "MobiVariables:RegisterVariableNumber", $"Registering ID '{ID}' on SimConnect for Address '{address}'");
            if (!clientDataRegistered.TryGetValue(ID, out bool registered) || !registered)
            {
                Logger.Log(LogLevel.Verbose, "MobiVariables:RegisterVariableNumber", $"Adding ID '{ID}' to SimConnect ClientDataDefinition");
                mobiSimConnect.simConnect.AddToClientDataDefinition(
                    (SIMCONNECT_DEFINE_ID)ID,
                    (ID - DEFINE_ID_OFFSET) * sizeof(float),
                    sizeof(float),
                    0,
                    0);

                if (!clientDataRegistered.TryAdd(ID, true))
                    clientDataRegistered[ID] = true;
            }

            mobiSimConnect.simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataValue>((SIMCONNECT_DEFINE_ID)ID);
            MobiSimConnect.RegisterClientData(mobiSimConnect.simConnect, ID, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET);

            mobiSimConnect.SendClientWasmCmd($"MF.SimVars.Add.{address}");
        }

        protected void RegisterVariableString(uint ID, string address)
        {
            Logger.Log(LogLevel.Verbose, "MobiVariables:RegisterVariableString", $"Registering ID '{ID}' on SimConnect for Address '{address}'");
            if (!clientDataRegistered.TryGetValue(ID, out bool registered) || !registered)
            {
                Logger.Log(LogLevel.Verbose, "MobiVariables:RegisterVariableString", $"Adding ID '{ID}' to SimConnect ClientDataDefinition");
                mobiSimConnect.simConnect.AddToClientDataDefinition(
                (SIMCONNECT_DEFINE_ID)ID,
                (ID - DEFINE_ID_OFFSET_STR) * MobiSimConnect.MOBIFLIGHT_STRINGVAR_SIZE,
                MobiSimConnect.MOBIFLIGHT_STRINGVAR_SIZE,
                0,
                0);

                if (!clientDataRegistered.TryAdd(ID, true))
                    clientDataRegistered[ID] = true;
            }

            mobiSimConnect.simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataStringValue>((SIMCONNECT_DEFINE_ID)ID);
            MobiSimConnect.RegisterClientData(mobiSimConnect.simConnect, ID, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_STRINGVARS, SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET);

            mobiSimConnect.SendClientWasmCmd($"MF.SimVars.AddString.{address}");
        }

        public void ReorderRegistrations()
        {
            try
            {
                var unusedIndices = simVarUsed.Where(flag => !flag.Value).ToList();
                if (unusedIndices.Count == 0)
                    return;

                Logger.Log(LogLevel.Information, "MobiSimConnect:ReorderRegistrations", $"Reordering Registrations with MobiFlight WASM Module... (Unused: {unusedIndices.Count})");

                List<IPCValue> cachedVars = [];
                foreach (var index in simVarUsed)
                    if (index.Value)
                        cachedVars.Add(simVars[index.Key]);

                simVarUsed.Clear();
                simVars.Clear();
                addressToIndex.Clear();
                nextVarID = DEFINE_ID_OFFSET;
                nextStringVarID = DEFINE_ID_OFFSET_STR;
                mobiSimConnect.SendClientWasmCmd("MF.SimVars.Clear");
                Thread.Sleep(25);

                foreach (var var in cachedVars)
                {
                    SubscribeAddress(var.Address, var);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiVariables:ReorderRegistrations", $"Exception while reordering Registrations! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }

        public void RemoveAll()
        {
            try
            {
                addressToIndex.Clear();
                simVars.Clear();
                simVarUsed.Clear();
                clientDataRegistered.Clear();
                subscribeQueue.Clear();
                nextVarID = DEFINE_ID_OFFSET;
                nextStringVarID = DEFINE_ID_OFFSET_STR;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "MobiVariables:RemoveAll", $"Exception while removing all SimVars! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }
    }
}
