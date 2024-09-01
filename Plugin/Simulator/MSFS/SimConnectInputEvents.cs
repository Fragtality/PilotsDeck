using Microsoft.FlightSimulator.SimConnect;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PilotsDeck.Simulator.MSFS
{
    public class SimConnectInputEvents(SimConnectManager simConnect)
    {
        protected SimConnectManager SimConnect { get; } = simConnect;
        protected Dictionary<string, ulong> EventHashes { get; } = [];
        public const uint DEFINE_ID_OFFSET_EVENT = 1;
        protected uint NextEventID { get; set; } = DEFINE_ID_OFFSET_EVENT;
        protected Dictionary<uint, ulong> EventIndex { get; } = [];
        protected Dictionary<ulong, VariableInputEvent> EventSubscribedValues { get; } = [];
        public bool EventsEnumerated { get; set; } = false;

        public void SimConnect_OnRecvEnumerateInputEvents(SimConnect sender, SIMCONNECT_RECV_ENUMERATE_INPUT_EVENTS data)
        {
            if (data.dwRequestID == (uint)SIMCONNECT_REQUEST_ID.EnumEvents)
            {
                List<string> receivedEvents = [];
                receivedEvents.Add($"InputEvents for Aircraft {SimConnect.AircraftString}");
                foreach (SIMCONNECT_INPUT_EVENT_DESCRIPTOR evt in data.rgData.Cast<SIMCONNECT_INPUT_EVENT_DESCRIPTOR>())
                {
                    if (evt.eType == SIMCONNECT_INPUT_EVENT_TYPE.DOUBLE)
                    {
                        EventHashes.TryAdd("B:" + evt.Name, evt.Hash);
                        EventIndex.TryAdd(NextEventID, evt.Hash);
                        Logger.Verbose($"Event '{evt.Name}' has Hash: {evt.Hash} (Type: {evt.eType}) on ID {NextEventID}");
                        receivedEvents.Add($"B:{evt.Name}");
                        NextEventID++;
                    }
                }
                EventsEnumerated = true;

                try
                {
                    string file = AppConfiguration.FILE_BVAR;
                    if (File.Exists(file))
                        File.Delete(file);

                    File.WriteAllLines(file, receivedEvents);
                }
                catch (IOException)
                {
                    Logger.Warning($"Could not write InputEvents to File!");
                }
            }
        }

        public void SimConnect_OnRecvGetInputEvents(SimConnect sender, SIMCONNECT_RECV_GET_INPUT_EVENT data)
        {
            Logger.Verbose($"Received InputEvent ID '{data.dwRequestID}' (Value: {(double)data.Value[0]})");
            if (EventSubscribedValues.TryGetValue(data.dwRequestID, out VariableInputEvent variable))
            {
                variable.SetValue((double)data.Value[0]);
            }
        }        

        public void EnumerateInputEvents()
        {
            Logger.Debug($"Sending EnumerateInput Request");
            EventsEnumerated = false;
            EventHashes.Clear();
            EventIndex.Clear();
            NextEventID = DEFINE_ID_OFFSET_EVENT;
            EventSubscribedValues.Clear();
            SimConnectManager.SimConnect.EnumerateInputEvents(SIMCONNECT_REQUEST_ID.EnumEvents);
        }

        public void RequestInputEventValues()
        {
            if (!EventsEnumerated)
                return;

            try
            {
                SimConnectManager.SimConnectMutex.TryWaitOne();
                foreach (var evt in EventSubscribedValues)
                {
                    Logger.Verbose($"Requesting InputEvent '{evt.Value.Address}' (#{evt.Value.Hash}) on ID '{evt.Key}'");
                    SimConnectManager.SimConnect.GetInputEvent((SIMCONNECT_DEFINE_ID)evt.Key, evt.Value.Hash);
                }
                SimConnectManager.SimConnectMutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                SimConnectManager.SimConnectMutex.TryReleaseMutex();
            }            
        }

        public void SubscribeInputEvent(string name, VariableInputEvent variable)
        {
            if (EventHashes.TryGetValue(name, out ulong hash) && variable != null)
            {
                if (!EventSubscribedValues.TryAdd(EventIndex.First(kv => kv.Value == hash).Key, variable))
                    Logger.Warning($"Failed to add InputEvent '{name}' to subscribed Values!");
                else
                {
                    (variable as VariableInputEvent).Hash = hash;
                    variable.IsSubscribed = true;
                }
            }
            else
            {
                Logger.Debug($"No Hash available for InputEvent '{name}'");
            }
        }

        public void UnsubscribeInputEvent(string name)
        {
            if (EventSubscribedValues.Any(kv => kv.Value.Address == name))
            {
                var hash = EventSubscribedValues.First(kv => kv.Value.Address == name);
                if (EventSubscribedValues.Remove(hash.Key))
                {
                    hash.Value.IsSubscribed = false;
                    Logger.Verbose($"InputEvent '{name}' removed from subscribed Values");
                }
                else
                    Logger.Warning($"Could not remove InputEvent '{name}' from subscribed Values!");
            }
            else
            {
                Logger.Warning($"InputEvent '{name}' not in subscribed Values!");
            }
        }

        public bool SendInputEvent(string name, double value)
        {
            try
            {
                if (EventsEnumerated && EventHashes.TryGetValue(name, out ulong hash))
                {
                    SimConnectManager.SimConnectMutex.TryWaitOne();
                    SimConnectManager.SimConnect.SetInputEvent(hash, value);
                    SimConnectManager.SimConnectMutex.ReleaseMutex();
                    return true;
                }
                else
                {
                    Logger.Warning($"No Hash available for InputEvent '{name}'");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                SimConnectManager.SimConnectMutex.TryReleaseMutex();
            }

            return false;
        }

        public void RemoveAll()
        {
            Logger.Debug($"Remove InputEvent Enumeration");
            EventHashes.Clear();
            EventIndex.Clear();
            foreach (var inputEvent in EventSubscribedValues)
                inputEvent.Value.IsSubscribed = false;
            EventSubscribedValues.Clear();
            NextEventID = DEFINE_ID_OFFSET_EVENT;
            EventsEnumerated = false;
        }
    }
}
