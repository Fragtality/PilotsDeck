using Microsoft.FlightSimulator.SimConnect;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.Simulator.MSFS
{
    public class MobiVariables()
    {
        public const uint DEFINE_ID_OFFSET = 2;
        public const uint DEFINE_ID_OFFSET_STR = 10001;

        protected ConcurrentDictionary<string, uint> AddressToIndex { get; } = [];
        protected ConcurrentDictionary<uint, ManagedVariable> SimVars { get; } = [];
        protected ConcurrentDictionary<uint, bool> RemoveList { get; set; } = [];
        protected ConcurrentDictionary<uint, bool> SimVarUsed { get; } = [];
        protected ConcurrentDictionary<uint, bool> ClientDataRegistered { get; } = [];

        protected uint NextVarID { get; set; } = DEFINE_ID_OFFSET;
        protected uint NextStringVarID { get; set; } = DEFINE_ID_OFFSET_STR;

        public string GetAddress(uint id)
        {
            if (SimVars.TryGetValue(id, out ManagedVariable variable) && variable != null)
            return variable.Address;
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

        protected static void SetManagedVariable(uint id, ManagedVariable variable, object[] dwData)
        {
            if (id < DEFINE_ID_OFFSET_STR)
                variable.SetValue(((ClientDataValue)dwData[0]).data);
            else
                variable.SetValue(((ClientDataStringValue)dwData[0]).data);
        }

        public void UpdateId(uint id, object[] dwData)
        {
            if (SimVars.TryGetValue(id, out ManagedVariable variable))
            {
                if (variable != null)
                {
                    SetManagedVariable(id, variable, dwData);
                }
                else
                    Logger.Error($"The Value for ID '{id}' is NULL! (Value: {GetObjectData(id, dwData)})");
            }
            else if (!RemoveList.ContainsKey(id))
                Logger.Debug($"The received ID '{id}' has no managed Variable! (Value: {GetObjectData(id, dwData)})");

            if (!RemoveList.IsEmpty)
                RemoveList.Remove(id);
        }

        protected uint GetNextID(string address)
        {
            if (TypeMatching.rxAvarMobiString.IsMatch(address))
                return NextStringVarID;
            else
                return NextVarID;
        }

        protected void IncreaseID(string address)
        {
            if (TypeMatching.rxAvarMobiString.IsMatch(address))
                NextStringVarID++;
            else
                NextVarID++;
        }

        public void SubscribeAddress(string address, ManagedVariable variable)
        {
            try
            {
                if (variable == null)
                {
                    Logger.Error($"The passed ManagedVariable is NULL!");
                    return;
                }
                if (NextVarID == 9999)
                {
                    Logger.Error($"Maximum Subscription Limit reached, can not subscribe any more SimVars!");
                    return;
                }
                
                uint next = GetNextID(address);
                if (AddressToIndex.TryAdd(address, next))
                {
                    SimVars.TryAdd(next, variable);
                    SimVarUsed.Add(next, true);
                    RegisterMobiVariable(next, variable);
                    Logger.Verbose($"Subscribed MobiVariable '{address}' to ID '{next}'");
                    IncreaseID(address);
                    variable.IsChanged = true;
                }
                else if (AddressToIndex.TryGetValue(address, out uint id) && !SimVarUsed[id])
                {
                    SimVarUsed[id] = true;
                    SimVars[id] = variable;
                    variable.IsSubscribed = true;
                    variable.IsChanged = true;
                    Logger.Verbose($"Re-Subscribed MobiVariable '{address}' to ID '{id}'");
                }
                else
                    Logger.Error($"Error subscribing Address '{address}' not added / already in Use! (contains: {AddressToIndex.ContainsKey(address)} | used: {SimVarUsed[id]})");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void UnsubscribeAddress(string address)
        {
            try
            {
                address = VariableManager.FormatAddress(address);
                bool found = AddressToIndex.TryGetValue(address, out uint id);
                bool used = SimVarUsed.TryGetValue(id, out bool result) && result;
                if (found && used)
                {
                    SimVarUsed[id] = false;
                    SimVars[id].IsSubscribed = false;
                }
                else
                    Logger.Error($"The Address '{address}' does not have an ID mapped or already unused! (found: {found}) (used: {used})");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void RegisterMobiVariable(uint id, ManagedVariable variable)
        {
            if (SimVars.ContainsKey(id) && variable != null)
            {
                Logger.Verbose($"Registering ID '{id}' for Variable '{variable.Address}' of Type '{variable.Type}'");
                string address = VariableManager.FormatAddress(variable.Address);

                if (address.StartsWith("C:"))
                    RegisterVariableNumber(id, address[2..]);
                else if (id >= DEFINE_ID_OFFSET_STR)
                    RegisterVariableString(id, address);
                else
                    RegisterVariableNumber(id, address);
                variable.IsSubscribed = true;
            }
            else
                Logger.Error($"The ID '{id}' is not used or was not found or is null! (null: {variable == null})");
        }

        protected void RegisterVariableNumber(uint ID, string address)
        {
            Logger.Verbose($"Registering ID '{ID}' on SimConnect for Address '{address}'");
            if (!ClientDataRegistered.TryGetValue(ID, out bool registered) || !registered)
            {
                Logger.Verbose($"Adding ID '{ID}' to SimConnect ClientDataDefinition");
                SimConnectManager.SimConnect.AddToClientDataDefinition(
                    (SIMCONNECT_DEFINE_ID)ID,
                    (ID - DEFINE_ID_OFFSET) * sizeof(float),
                    sizeof(float),
                    0,
                    0);

                if (!ClientDataRegistered.TryAdd(ID, true))
                    ClientDataRegistered[ID] = true;
            }

            SimConnectManager.SimConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataValue>((SIMCONNECT_DEFINE_ID)ID);
            MobiModule.RegisterClientData(SimConnectManager.SimConnect, ID, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS, SIMCONNECT_CLIENT_DATA_PERIOD.VISUAL_FRAME);

            MobiModule.SendClientWasmCmd($"MF.SimVars.Add.{address}");
        }

        protected void RegisterVariableString(uint ID, string address)
        {
            Logger.Verbose($"Registering ID '{ID}' on SimConnect for Address '{address}'");
            if (!ClientDataRegistered.TryGetValue(ID, out bool registered) || !registered)
            {
                Logger.Verbose($"Adding ID '{ID}' to SimConnect ClientDataDefinition");
                SimConnectManager.SimConnect.AddToClientDataDefinition(
                (SIMCONNECT_DEFINE_ID)ID,
                (ID - DEFINE_ID_OFFSET_STR) * MobiModule.MOBIFLIGHT_STRINGVAR_SIZE,
                MobiModule.MOBIFLIGHT_STRINGVAR_SIZE,
                0,
                0);

                if (!ClientDataRegistered.TryAdd(ID, true))
                    ClientDataRegistered[ID] = true;
            }

            SimConnectManager.SimConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ClientDataStringValue>((SIMCONNECT_DEFINE_ID)ID);
            MobiModule.RegisterClientData(SimConnectManager.SimConnect, ID, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_STRINGVARS, SIMCONNECT_CLIENT_DATA_PERIOD.VISUAL_FRAME);

            MobiModule.SendClientWasmCmd($"MF.SimVars.AddString.{address}");
        }

        public void ReorderRegistrations(bool force)
        {
            try
            {
                var unusedIndices = SimVarUsed.Where(flag => !flag.Value).ToList();
                if (unusedIndices.Count < App.Configuration.MobiReorderTreshold && !force)
                    return;

                Logger.Information($"Reordering Registrations with MobiFlight WASM Module... (Unused: {unusedIndices.Count})");

                foreach (var variable in SimVars)
                {
                    variable.Value.IsSubscribed = false;
                    if (!SimVarUsed[variable.Key])
                    {
                        if (variable.Value is VariableNumeric varNum)
                            varNum.SetValue(0);
                        else if (variable.Value is VariableString varStr)
                            varStr.SetValue("0");
                    }
                    RemoveList.Add(variable.Key);
                }

                SimVarUsed.Clear();
                SimVars.Clear();
                AddressToIndex.Clear();
                NextVarID = DEFINE_ID_OFFSET;
                NextStringVarID = DEFINE_ID_OFFSET_STR;
                MobiModule.SendClientWasmCmd("MF.SimVars.Clear");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void RemoveAll()
        {
            try
            {
                AddressToIndex.Clear();
                foreach (var variable in SimVars)
                {
                    variable.Value.IsSubscribed = false;
                    if (variable.Value is VariableNumeric varNum)
                        varNum.SetValue(0);
                    else if (variable.Value is VariableString varStr)
                        varStr.SetValue("");
                }
                SimVars.Clear();
                SimVarUsed.Clear();
                ClientDataRegistered.Clear();
                RemoveList.Clear();
                NextVarID = DEFINE_ID_OFFSET;
                NextStringVarID = DEFINE_ID_OFFSET_STR;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
