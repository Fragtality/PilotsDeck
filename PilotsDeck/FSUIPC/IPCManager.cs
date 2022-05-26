using System;
using System.Linq;
using System.Collections.Generic;
using FSUIPC;
using Serilog;
//using WASM = FSUIPC.MSFSVariableServices;

namespace PilotsDeck
{
    public class IPCManager : IDisposable
    {
        private Dictionary<string, IPCValue> currentValues = new Dictionary<string, IPCValue>();
        private Dictionary<string, int> currentRegistrations = new Dictionary<string, int>();
        private List<string> persistentValues = new List<string>();
        private static readonly string inMenuAddr = "3365:1";
        private static readonly string isPausedAddr = "0262:2";
        private Simulator currentSim = Simulator.UNKNOWN;
        //private MSFSVariableServices WASM = new MSFSVariableServices();
        

        private IPCValueOffset inMenuValue;
        private IPCValueOffset isPausedValue;

        public bool IsConnected
        {
            get
            {
                //if (currentSim == Simulator.MSFS)
                //    return FSUIPCConnection.IsOpen && WASM.IsRunning;
                //else
                    return FSUIPCConnection.IsOpen;
            }
        }

        public bool IsReady
        {
            get
            {
                return inMenuValue.Value == "0" && isPausedValue.Value == "0" && IsConnected; 
            }
        }

        public int Length => currentValues.Count;

        public IPCValue this[string address]
        {
            get
            {
                if (currentValues.ContainsKey(address))
                    return currentValues[address];
                else
                    return null;
            }
        }

        public IPCManager(string group)
        {
            inMenuValue = RegisterAddress(inMenuAddr, group, true) as IPCValueOffset;

            isPausedValue = RegisterAddress(isPausedAddr, group, true) as IPCValueOffset;
        }

        public void SetSimulator(Simulator sim)
        {
            currentSim = sim;
        }

        public bool Connect()
        {
            try
            {
                FSUIPCConnection.Open();

                if (FSUIPCConnection.IsOpen)
                {
                    Log.Logger.Information("IPCManager: FSUIPC Connected");
                }

                //if (currentSim == Simulator.MSFS)
                //{
                //    WASM.OnLogEntryReceived += OnVariableServiceLogEvent;
                //    WASM.Init(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle);
                //    WASM.Start();
                //    if (WASM.IsRunning)
                //        Log.Logger.Information("IPCManager: FSUIPC WASM Started");
                //}
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while opening FSUIPC");
            }

            return IsConnected;
        }

        private void OnVariableServiceLogEvent(object sender, LogEventArgs e)
        {
            Log.Logger.Information($"IPCManager- WASM: {e.LogEntry}");
        }

        public void Dispose()
        {
            Close();
        }

        public bool Close()
        {
            try
            {
                var delAddresses = currentValues.Keys.Where(adr => !persistentValues.Contains(adr)).ToList();
                foreach (var address in delAddresses)
                {
                    currentValues.Remove(address);
                    currentRegistrations.Remove(address);
                }
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while removing Registrations!");
            }

            try
            {
                FSUIPCConnection.Close();

                if (!FSUIPCConnection.IsOpen)
                {
                    Log.Logger.Information("IPCManager: FSUIPC Closed");
                }

                if (currentSim == Simulator.MSFS)
                {
                    //WASM.Stop();
                    //WASM.OnLogEntryReceived -= OnVariableServiceLogEvent;
                    //if (!WASM.IsRunning)
                    //    Log.Logger.Information("IPCManager: FSUIPC WASM Stopped");
                }
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while closing FSUIPC");
            }

            return !IsConnected;
        }

        public IPCValue RegisterAddress(string address, string group, bool persistent = false)
        {
            IPCValue value = null;
            try
            {
                if (!IPCTools.IsReadAddress(address))
                {
                    Log.Logger.Error($"RegisterValue: Not an Read-Address! [{address}]");
                    return value;
                }

                if (currentValues.ContainsKey(address))
                {
                    value = currentValues[address];
                    currentRegistrations[address]++;
                    Log.Logger.Debug($"RegisterValue: Added Registration for Address {address}, Registrations: {currentRegistrations[address]}");
                }
                else
                {
                    if (IPCTools.rxOffset.IsMatch(address))
                        value = new IPCValueOffset(address, group);
                    else
                    {
                        //if (currentSim == Simulator.MSFS)
                        //    value = new IPCValueWASM(address);
                        //else
                            value = new IPCValueLvar(address);
                    }

                    currentValues.Add(address, value);
                    currentRegistrations.Add(address, 1);
                    if (persistent)
                        persistentValues.Add(address);

                    Log.Logger.Debug($"RegisterValue: Added Address {address}, Persistent: {persistent}");
                }                
            }
            catch
            {
                Log.Logger.Error($"RegisterValue: Exception while registering Address {address}");
            }

            if (value == null)
                Log.Logger.Error($"RegisterValue: Null Reference for Address {address}");

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

                        currentValues[address].Dispose();
                        currentValues.Remove(address);

                        Log.Logger.Debug($"DeregisterAddress: Removed Address {address}");
                    }
                    else
                    {
                        currentRegistrations[address]--;
                        Log.Logger.Debug($"DeregisterAddress: Deregistered Address {address}, Registrations open: {currentRegistrations[address]}");
                    }
                }
                else if (IsConnected)
                    Log.Logger.Error($"DeregisterAddress: Could not find Address {address}");
            }
            catch
            {
                Log.Logger.Error($"DeregisterAddress: Exception while deregistering Address {address}");
            }
        }

        public bool Process(string group)
        {
            try
            {
                foreach (var address in persistentValues)
                    currentValues[address].Connect();

                FSUIPCConnection.Process(group); //should update all offsets in currentOffsets (type OFFSETx and SCRIPT)
                if (!IsReady)
                    return false;

                foreach (var value in currentValues.Values) //read Lvars
                {
                    //value.Process(WASM);
                    value.Process();
                }
                    

                return true;
            }
            catch
            {
                Log.Logger.Error("IPCManager: Exception while process call to FSUIPC");
                return false;
            }
        }

        public bool ProcessWithConnect(string group)
        {
            if (!IsConnected)
            {
                if (Connect())
                    return Process(group);
                else
                    return false;
            }
            else
                return Process(group);
        }

        public bool SendVjoy(string address, byte action)
        {
            try
            {
                string[] parts = address.Split(':');
                byte[] offValue = new byte[4];

                offValue[3] = 0;
                offValue[2] = action;
                offValue[1] = Convert.ToByte(parts[0]); //joy
                offValue[0] = Convert.ToByte(parts[1]); //btn

                return WriteOffset("0x29F0:4:i", BitConverter.ToUInt32(offValue,0).ToString());
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while sending Virtual Joystick <{address}> to FSUIPC");
                return false;
            }
        }

        public bool WriteOffset(string address, string value)
        {
            bool result = false;
            IPCValueOffset offset = null;
            try
            {
                offset = new IPCValueOffset(address, AppSettings.groupStringWrite, OffsetAction.Write);
                offset.Write(value, AppSettings.groupStringWrite);
                offset.Dispose();
                offset = null;
                result = true;

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"IPCManager: Exception while writing Offset <{address}> (size:{offset?.Size}/float:{offset?.IsFloat}/string:{offset?.IsString}/signed:{offset?.IsSigned}) to FSUIPC! {ex.Message} - {ex.StackTrace}");
                if (offset != null)
                {
                    offset.Dispose();
                    offset = null;
                }
            }

            return result;
        }

        public bool WriteLvar(string name, double value)
        {
            bool result = false;
            try
            {
                //if (currentSim == Simulator.MSFS)
                //{
                //    if (WASM.LVars.Exists(name))
                //    {
                //        WASM.LVars[name].SetValue(value);
                //        result = true;
                //    }
                //    else
                //        Log.Logger.Error($"IPCManager: LVar <{name}> does not exist");
                //}
                //else
                //{
                    FSUIPCConnection.WriteLVar(name, value);
                    result = true;
                //}
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while writing LVar <{name}:{value}> to FSUIPC/WASM");
            }

            return result;
        }

        //public bool WriteHvar(string name)
        //{
        //    bool result = false;

        //    try
        //    {
        //        if (currentSim == Simulator.MSFS)
        //        {
        //            if (WASM.HVars.Exists(name))
        //            {
        //                WASM.HVars[name].Set();
        //                result = true;
        //            }
        //            else
        //                Log.Logger.Error($"IPCManager: HVar <{name}> does not exist");
        //        }
        //    }
        //    catch
        //    {
        //        Log.Logger.Error($"IPCManager: Exception while setting HVar <{name}> via WASM");
        //    }

        //    return result;
        //}

        public bool RunMacro(string name)
        {
            try
            {
                Offset request = new Offset(0x0D70, 128);
                request.SetValue(name);
                FSUIPCConnection.Process();
                request.Disconnect();
                request = null;
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while Executing Macro: {name}");
                return false;
            }

            return true;
        }

        protected bool RunSingleScript(string name, int flag)
        {
            try
            {
                Offset param = null;
                if (flag != -1)
                {
                    param = new Offset(0x0D6C, 4);
                    param.SetValue(flag);
                    FSUIPCConnection.Process();
                }

                Offset request = new Offset(0x0D70, 128);
                request.SetValue(name);

                FSUIPCConnection.Process();
                request.Disconnect();
                request = null;
                if (flag != -1)
                {
                    param.Disconnect();
                    param = null;
                }
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while Executing Script: {name}");
                return false;
            }

            return true;
        }

        public bool RunScript(string name)
        {
            try
            {
                string[] parts = name.Split(':');
                if (parts.Length == 3 && int.TryParse(parts[2], out int result))    //Script with flag
                    return RunSingleScript(parts[0] + ":" + parts[1], result);
                if (parts.Length == 2)                                              //Script
                    return RunSingleScript(parts[0] + ":" + parts[1], -1);          //Script with mulitple flags
                if (parts.Length >= 3)                          
                {
                    for (int i = 2; i < parts.Length; i++)
                    {
                        if (int.TryParse(parts[i], out result))
                        {
                            if (!RunSingleScript(parts[0] + ":" + parts[1], result))
                                return false;
                        }
                        else
                        {
                            Log.Logger.Error($"IPCManager: Exception while Executing Script: {name} - flag could not be parsed");
                            return false;
                        }
                        System.Threading.Thread.Sleep(25);
                    }
                }
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while Executing Script: {name}");
                return false;
            }

            return true;
        }

        public bool SendControl(string control, string param = "0")
        {
            return SendControl(Convert.ToInt32(control), Convert.ToInt32(param));
        }

        public bool SendControl(int control, int param = 0)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.SendControlToFS(control, param);
                result = true;
            }
            catch
            {
                Log.Logger.Error($"IPCManager: Exception while sending Control <{control}:{param}> to FSUIPC");
            }

            return result;
        }
    }
}
