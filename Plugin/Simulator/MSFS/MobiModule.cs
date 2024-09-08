using Microsoft.FlightSimulator.SimConnect;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PilotsDeck.Simulator.MSFS
{
    public class MobiModule : IDisposable
    {
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND = "MobiFlight.Command";
        public const string MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE = "MobiFlight.Response";
        public const uint MOBIFLIGHT_MESSAGE_SIZE = 1024;
        public const int MOBIFLIGHT_STRINGVAR_SIZE = 128;
        public const int MOBIFLIGHT_STRINGVAR_MAX_AMOUNT = 64;
        public const int MOBIFLIGHT_STRINGVAR_DATAAREA_SIZE = MOBIFLIGHT_STRINGVAR_SIZE * MOBIFLIGHT_STRINGVAR_MAX_AMOUNT;

        public static readonly string CLIENT_NAME = AppConfiguration.SC_CLIENT_NAME;
        public static readonly string PILOTSDECK_CLIENT_DATA_NAME_SIMVAR = $"{CLIENT_NAME}.LVars";
        public static readonly string PILOTSDECK_CLIENT_DATA_NAME_COMMAND = $"{CLIENT_NAME}.Command";
        public static readonly string PILOTSDECK_CLIENT_DATA_NAME_RESPONSE = $"{CLIENT_NAME}.Response";
        public static readonly string PILOTSDECK_CLIENT_DATA_NAME_STRINGVAR = $"{CLIENT_NAME}.StringVars";

        protected static SimConnect SimConnect { get { return SimConnectManager.SimConnect; } }
        public bool IsMobiConnected { get; protected set; } = false;
        protected DateTime LastConnectionAttempt { get; set; } = DateTime.MinValue;
        public MobiVariables MobiVariables { get; } = new();
        public List<string> LvarList { get; } = [];
        protected bool RequestingList { get; set; } = false;

        public void CheckConnection(bool isSessionReady)
        {
            try
            {
                if (!IsMobiConnected && DateTime.Now - LastConnectionAttempt >= TimeSpan.FromMilliseconds(App.Configuration.MobiRetryDelay))
                {
                    Logger.Information($"Sending Ping to MobiFlight WASM Module.");
                    ClearLvarList();
                    LastConnectionAttempt = DateTime.Now;
                    SendMobiWasmCmd("MF.DummyCmd");
                    SendMobiWasmCmd("MF.Ping");
                    SendMobiWasmCmd("MF.DummyCmd");
                }
                else if (isSessionReady && LvarList.Count == 0)
                    GetLvarList();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static void RegisterClientData(SimConnect simConnect, uint ID, Enum DataId, SIMCONNECT_CLIENT_DATA_PERIOD period)
        {
            simConnect.RequestClientData(
                DataId,
                (SIMCONNECT_REQUEST_ID)ID,
                (SIMCONNECT_DEFINE_ID)ID,
                period,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0
            );
        }

        public static void CreateDataAreaDefaultChannel()
        {
            SimConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_COMMAND, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD);
            SimConnect.MapClientDataNameToID(MOBIFLIGHT_CLIENT_DATA_NAME_RESPONSE, MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);

            SimConnect.AddToClientDataDefinition(SIMCONNECT_DEFINE_ID.CLIENT_MOBI, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);

            SimConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>(SIMCONNECT_DEFINE_ID.CLIENT_MOBI);

            SimConnect.RequestClientData(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)SIMCONNECT_DEFINE_ID.CLIENT_MOBI,
                SIMCONNECT_DEFINE_ID.CLIENT_MOBI,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        public static void CreateDataAreaClientChannel()
        {
            SimConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_COMMAND, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD);
            SimConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_RESPONSE, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE);
            SimConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_SIMVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_LVARS);
            SimConnect.MapClientDataNameToID(PILOTSDECK_CLIENT_DATA_NAME_STRINGVAR, PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_STRINGVARS);

            SimConnect.AddToClientDataDefinition(SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK, 0, MOBIFLIGHT_MESSAGE_SIZE, 0, 0);

            SimConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, ResponseString>(SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK);

            SimConnect.RequestClientData(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_RESPONSE,
                (SIMCONNECT_REQUEST_ID)SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK,
                SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK,
                SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                0,
                0,
                0);
        }

        public void SimConnect_OnClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            try
            {
                Logger.Verbose($"dwRequestID {data.dwRequestID} => '{MobiVariables.GetAddress(data.dwRequestID)}'");
                if (data.dwRequestID == (uint)SIMCONNECT_DEFINE_ID.CLIENT_MOBI)
                {
                    var request = (ResponseString)data.dwData[0];
                    if (request.Data == "MF.Pong")
                    {
                        if (!IsMobiConnected)
                        {
                            Logger.Information($"MobiFlight WASM Ping acknowledged - opening Client Connection.");
                            SendMobiWasmCmd("MF.DummyCmd");
                            SendMobiWasmCmd($"MF.Clients.Add.{CLIENT_NAME}");
                            SendMobiWasmCmd("MF.DummyCmd");
                        }
                        else
                            Logger.Debug($"MF.Pong received although already connected.");
                    }
                    else if (request.Data == $"MF.Clients.Add.{CLIENT_NAME}.Finished")
                    {
                        CreateDataAreaClientChannel();
                        IsMobiConnected = true;
                        LastConnectionAttempt = DateTime.MinValue;
                        SendClientWasmDummyCmd();
                        SendClientWasmCmd("MF.SimVars.Clear");
                        SendClientWasmCmd($"MF.Config.MAX_VARS_PER_FRAME.Set.{App.Configuration.MobiVarsPerFrame}");
                        Logger.Information($"MobiFlight WASM Client Connection opened.");
                    }
                    else if (!request.Data.StartsWith("MF.Clients.Add."))
                        Logger.Information($"Unhandled MobiFlight Messages received: '{request.Data}'");
                }
                else if (data.dwRequestID == (uint)SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK)
                {
                    var request = (ResponseString)data.dwData[0];

                    if (request.Data == $"MF.LVars.List.Start")
                    {
                        LvarList.Clear();
                        Logger.Debug($"Receiving L-Vars from MF Module ...");
                    }
                    else if (request.Data == $"MF.LVars.List.End")
                    {
                        Logger.Debug($"Received all L-Vars from MF Module!");
                        try
                        {
                            string file = AppConfiguration.FILE_LVAR;
                            if (File.Exists(file))
                                File.Delete(file);

                            File.WriteAllLines(file, LvarList);
                        }
                        catch (IOException)
                        {
                            Logger.Warning($"Could not write L-Vars to File!");
                        }
                        RequestingList = false;
                    }
                    else if (!string.IsNullOrWhiteSpace(request.Data))
                    {
                        Logger.Verbose($"Received L-Var: {request.Data}");
                        LvarList.Add(request.Data);
                    }
                }
                else if (data?.dwData[0] is ClientDataValue || data?.dwData[0] is ClientDataStringValue)
                {
                    MobiVariables.UpdateId(data.dwRequestID, data.dwData);
                }
                else
                {
                    Logger.Warning($"Received unknown Event! (dwID {data?.dwID} | dwDefineID {data?.dwDefineID} | dwRequestID {data?.dwRequestID} | dwData {data?.dwData[0]?.GetType().Name})");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void GetLvarList()
        {
            if (!RequestingList && IsMobiConnected)
            {
                RequestingList = true;
                Logger.Debug($"Requesting L-Var List");
                SendClientWasmCmd("MF.LVars.List");
            }
        }

        public void ClearLvarList()
        {
            RequestingList = false;
            LvarList.Clear();
        }

        public void Disconnect()
        {
            try
            {
                if (IsMobiConnected)
                {
                    IsMobiConnected = false;
                    LastConnectionAttempt = DateTime.MinValue;
                    MobiVariables.RemoveAll();

                    try { SendClientWasmCmd("MF.SimVars.Clear"); } catch (Exception ex) { Logger.LogException(ex); }
                    ClearLvarList();
                    Logger.Information($"MobiModule Connection closed.");                    
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        public static void SendClientWasmCmd(string command, bool includeDummy = true)
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK, command);
            if (includeDummy)
                SendClientWasmDummyCmd();
        }

        private static void SendClientWasmDummyCmd()
        {
            SendWasmCmd(PILOTSDECK_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_DEFINE_ID.CLIENT_PILOTSDECK, "MF.DummyCmd");
        }

        public static void SendMobiWasmCmd(string command)
        {
            SendWasmCmd(MOBIFLIGHT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_DEFINE_ID.CLIENT_MOBI, command);
        }

        private static void SendWasmCmd(Enum cmdChannelId, Enum cmdId, string command)
        {
            SimConnect.SetClientData(cmdChannelId, cmdId, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new ClientDataString(command));
        }

        public void SubscribeAddress(string address, ManagedVariable variable)
        {
            MobiVariables.SubscribeAddress(address, variable);
        }

        public void UnsubscribeAddress(string address)
        {
            MobiVariables.UnsubscribeAddress(address);
        }

        public static bool SetSimVar(string address, string value)
        {
            try
            {
                SimConnectManager.SimConnectMutex.TryWaitOne();
                address = VariableManager.FormatAddress(address);
                if (!TypeMatching.rxAvar.IsMatch(address) && !TypeMatching.rxLvarMobi.IsMatch(address))
                {
                    Logger.Error($"The Address '{address}' is not valid for MobiFlight!");
                    return false;
                }

                string code = address.Insert(1, ">");
                if (!TypeMatching.rxAvarMobiString.IsMatch(address))
                    code = $"{string.Format(CultureInfo.InvariantCulture, "{0:G}", value)} {code}";
                else
                    code = $"'{value}' {code}";
                SendClientWasmCmd($"MF.SimVars.Set.{code}");
                SimConnectManager.SimConnectMutex.ReleaseMutex();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                SimConnectManager.SimConnectMutex.TryReleaseMutex();
                return false;
            }
        }

        public static bool ExecuteCode(string code)
        {
            try
            {
                SimConnectManager.SimConnectMutex.TryWaitOne();
                Logger.Debug($"Executing Calc Code: {code}");
                SendClientWasmCmd($"MF.SimVars.Set.{code}");
                SimConnectManager.SimConnectMutex.ReleaseMutex();
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                SimConnectManager.SimConnectMutex.TryReleaseMutex();
                return false;
            }
        }
    }
}
