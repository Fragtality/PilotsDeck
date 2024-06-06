using Microsoft.FlightSimulator.SimConnect;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PilotsDeck
{
    public class SimConnectInputEvents(IPCManager ipcManager, MobiSimConnect mobiSimConnect)
    {
        protected IPCManager ipcManager { get; } = ipcManager;
        protected MobiSimConnect mobiSimConnect { get; } = mobiSimConnect;
        protected Dictionary<string, ulong> eventHashes { get; } = [];
        public const uint DEFINE_ID_OFFSET_EVENT = 1;
        protected uint nextEventID { get; set; } = DEFINE_ID_OFFSET_EVENT;
        protected Dictionary<uint, ulong> eventIndex { get; } = [];
        protected Dictionary<ulong, IPCValueInputEvent> eventSubscribedValues { get; } = [];
        public bool eventsEnumerated { get; set; } = false;

        public void SimConnect_OnRecvEnumerateInputEvents(SimConnect sender, SIMCONNECT_RECV_ENUMERATE_INPUT_EVENTS data)
        {
            if (data.dwRequestID == (uint)SIMCONNECT_REQUEST_ID.EnumEvents)
            {
                List<string> receivedEvents = [];
                foreach (SIMCONNECT_INPUT_EVENT_DESCRIPTOR evt in data.rgData.Cast<SIMCONNECT_INPUT_EVENT_DESCRIPTOR>())
                {
                    if (evt.eType == SIMCONNECT_INPUT_EVENT_TYPE.DOUBLE)
                    {
                        eventHashes.TryAdd("B:" + evt.Name, evt.Hash);
                        eventIndex.TryAdd(nextEventID, evt.Hash);
                        receivedEvents.Add($"Event '{evt.Name}' has Hash: {evt.Hash} (Type: {evt.eType}) on ID {nextEventID}");
                        nextEventID++;
                    }
                }
                eventsEnumerated = true;
                mobiSimConnect.SubscribeAllAddresses(true);

                try
                {
                    string file = "InputEvents.txt";
                    if (File.Exists(file))
                        File.Delete(file);

                    File.WriteAllLines(file, receivedEvents);
                }
                catch (IOException)
                {
                    Logger.Log(LogLevel.Warning, "SimConnectInputEvents:OnRecvEnumerateInputEvents", $"Could not write InputEvents to File!");
                    foreach (var line in receivedEvents)
                        Logger.Log(LogLevel.Debug, "SimConnectInputEvents:OnRecvEnumerateInputEvents", line);
                }
            }
        }

        public void SimConnect_OnRecvGetInputEvents(SimConnect sender, SIMCONNECT_RECV_GET_INPUT_EVENT data)
        {
            Logger.Log(LogLevel.Verbose, "SimConnectInputEvents:OnRecvGetInputEvents", $"Received InputEvent ID '{data.dwRequestID}' (Value: {(double)data.Value[0]})");
            if (eventSubscribedValues.TryGetValue(data.dwRequestID, out IPCValueInputEvent value))
            {
                value.SetValue((double)data.Value[0]);
            }
        }        

        public void EnumerateInputEvents()
        {
            Logger.Log(LogLevel.Debug, "SimConnectInputEvents:EnumerateInputEvents", $"Sending EnumerateInput Request");
            eventsEnumerated = false;
            eventHashes.Clear();
            eventIndex.Clear();
            nextEventID = DEFINE_ID_OFFSET_EVENT;
            eventSubscribedValues.Clear();
            mobiSimConnect.simConnect.EnumerateInputEvents(SIMCONNECT_REQUEST_ID.EnumEvents);
        }

        public void RequestInputEventValues()
        {
            if (!eventsEnumerated)
                return;

            foreach (var evt in eventSubscribedValues)
            {
                Logger.Log(LogLevel.Verbose, "SimConnectInputEvents:RequestInputEventValues", $"Requesting InputEvent '{evt.Value.Address}' (#{evt.Value.Hash}) on ID '{evt.Key}'");
                mobiSimConnect.simConnect.GetInputEvent((SIMCONNECT_DEFINE_ID)evt.Key, evt.Value.Hash);
            }
        }

        public void SubscribeInputEvent(string name)
        {
            if (eventHashes.TryGetValue(name, out ulong hash) && ipcManager.TryGetValue(name, out IPCValue value))
            {
                if (!eventSubscribedValues.TryAdd(eventIndex.First(kv => kv.Value == hash).Key, value as IPCValueInputEvent))
                    Logger.Log(LogLevel.Warning, "SimConnectInputEvents:SubscribeInputEvent", $"Failed to add InputEvent '{name}' to subscribed Values!");
                else
                    (value as IPCValueInputEvent).Hash = hash;
            }
            else
            {
                Logger.Log(LogLevel.Warning, "SimConnectInputEvents:SubscribeInputEvent", $"No Hash available for InputEvent '{name}'");
            }
        }

        public void UnsubscribeInputEvent(string name)
        {
            if (eventSubscribedValues.Any(kv => kv.Value.Address == name))
            {
                var hash = eventSubscribedValues.First(kv => kv.Value.Address == name);
                if (eventSubscribedValues.Remove(hash.Key))
                    Logger.Log(LogLevel.Verbose, "SimConnectInputEvents:UnsubscribeInputEvent", $"InputEvent '{name}' removed from subscribed Values");
                else
                    Logger.Log(LogLevel.Warning, "SimConnectInputEvents:UnsubscribeInputEvent", $"Could not remove InputEvent '{name}' from subscribed Values!");
            }
            else
            {
                Logger.Log(LogLevel.Warning, "SimConnectInputEvents:UnsubscribeInputEvent", $"InputEvent '{name}' not in subscribed Values!");
            }
        }

        public bool SendInputEvent(string name, double value)
        {
            if (eventsEnumerated && eventHashes.TryGetValue(name, out ulong hash))
            {
                mobiSimConnect.simConnect.SetInputEvent(hash, value);
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Warning, "SimConnectInputEvents:SendInputEvent", $"No Hash available for InputEvent '{name}'");
            }

            return false;
        }

        public void RemoveAll()
        {
            eventHashes.Clear();
            eventIndex.Clear();
            eventSubscribedValues.Clear();
            nextEventID = DEFINE_ID_OFFSET_EVENT;
            eventsEnumerated = false;
        }
    }
}
