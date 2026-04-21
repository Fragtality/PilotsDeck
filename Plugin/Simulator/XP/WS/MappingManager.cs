using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator.XP.WS.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PilotsDeck.Simulator.XP.WS
{
    public class MappingManager(WebSocketXP socketXP)
    {
        public virtual ConcurrentDictionary<string, IdMappingXP> SubscribedDataAddresses { get; } = [];
        public virtual ConcurrentDictionary<long, List<IdMappingXP>> SubscribedDataIds { get; } = [];
        protected virtual WebSocketXP WebSocket { get; } = socketXP;
        protected virtual ConcurrentDictionary<long, DataRef> KnownDataRefs => WebSocket.KnownDataRefs;
        protected virtual ConcurrentDictionary<long, CommandRef> KnownCommands => WebSocket.KnownCommands;
        public virtual bool HasDataRefMappings => !SubscribedDataIds.IsEmpty;

        public virtual bool HasDataRef(string name, out DataRef dataRef)
        {
            var query = KnownDataRefs.Where(r => r.Value.name == name);
            if (query.Any())
            {
                dataRef = query.First().Value;
                return true;
            }
            else
            {
                dataRef = null;
                return false;
            }
        }

        public virtual bool IsIdSubscribed(long id)
        {
            return SubscribedDataIds.ContainsKey(id) && KnownDataRefs.ContainsKey(id);
        }

        public virtual void AddDataRef(IdMappingXP idMapping)
        {
            idMapping.ValueRef.IsSubscribed = true;

            if (SubscribedDataIds.TryGetValue(idMapping.RefId, out var list))
                list.Add(idMapping);
            else
                SubscribedDataIds.Add(idMapping.RefId, [idMapping]);

            SubscribedDataAddresses.TryAdd(idMapping.Address, idMapping);
            Logger.Verbose($"Subscribed Mapping for '{idMapping.Address}' => '{idMapping.RefId}'");
        }

        public virtual bool GetMapping(ManagedVariable variable, out IdMappingXP idMapping)
        {
            return SubscribedDataAddresses.TryGetValue(variable.Address, out idMapping);
        }

        public virtual void RemoveDataRef(IdMappingXP idMapping)
        {
            idMapping.ValueRef.IsSubscribed = false;
            SubscribedDataIds[idMapping.RefId].Remove(idMapping);
            SubscribedDataAddresses.Remove(idMapping.Address);
        }

        public virtual void RemoveAllDataRef()
        {
            foreach (var sub in SubscribedDataIds)
                foreach (var mapping in sub.Value)
                    mapping.ValueRef.IsSubscribed = false;

            SubscribedDataIds.Clear();
            SubscribedDataAddresses.Clear();
        }

        public virtual void UpdateRef(long id, JsonElement jsonData)
        {
            try
            {
                var type = KnownDataRefs[id].ValueTypeApi;
                var mappings = SubscribedDataIds[id];

                foreach (var mapping in mappings)
                {
                    if (type == XpApiValueType.FLOAT)
                        mapping.ValueRef.SetValue(jsonData.GetSingle());
                    else if (type == XpApiValueType.DOUBLE)
                        mapping.ValueRef.SetValue(jsonData.GetDouble());
                    else if (type == XpApiValueType.INT)
                        mapping.ValueRef.SetValue(jsonData.GetInt32());
                    else if (type == XpApiValueType.DATA && mapping.IsString)
                        mapping.ValueRef.SetValue(jsonData.GetString().Base64Decode()[..mapping.StringLength]);
                    else if (type == XpApiValueType.FLOAT_ARRAY)
                        mapping.ValueRef.SetValue(jsonData.EnumerateArray().ToArray()[mapping.ArrayIndex].GetSingle());
                    else if (type == XpApiValueType.INT_ARRAY)
                        mapping.ValueRef.SetValue(jsonData.EnumerateArray().ToArray()[mapping.ArrayIndex].GetInt32());
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
