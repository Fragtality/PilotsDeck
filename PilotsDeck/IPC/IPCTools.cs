using FSUIPC;
using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using WASM = FSUIPC.MSFSVariableServices;

namespace PilotsDeck
{


    public static class IPCTools
    {
        //2D => -
        //2F => /
        //5F => _
        public static readonly string validName = @"[a-zA-Z0-9\x2D\x5F]+";
        public static readonly Regex rxMacro = new ($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxScript = new ($"^(Lua(Set|Clear|Toggle|Value)?:){{1}}{validName}(:[0-9]{{1,4}})*$", RegexOptions.Compiled);
        public static readonly Regex rxControlSeq = new (@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        public static readonly Regex rxControl = new (@"^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$", RegexOptions.Compiled);
        public static readonly Regex rxLvar = new ($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxHvar = new ($"^[^0-9]{{1}}((H:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxOffset = new (@"^((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoy = new (@"^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoyDrv = new (@"^(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxDref = new ($"^({validName}[\\x2F]){{1}}({validName}[\\x2F])*({validName}(([\\x5B][0-9]+[\\x5D])|(:s[0-9]+)){{0,1}}){{1}}$", RegexOptions.Compiled);
        public static readonly string validPathXP = $"({validName}[\\x2F]){{1}}({validName}[\\x2F])*({validName}){{1}}";
        public static readonly Regex rxCmdXP = new($"^({validPathXP}){{1}}(:{validPathXP})*$", RegexOptions.Compiled);

        public static bool IsReadAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;
            else if (rxOffset.IsMatch(address) || rxLvar.IsMatch(address) || rxDref.IsMatch(address))
                return true;
            else
                return false;
        }

        public static bool IsWriteAddress(string address, ActionSwitchType type)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            switch (type)
            {
                case ActionSwitchType.MACRO:
                    return rxMacro.IsMatch(address);
                case ActionSwitchType.SCRIPT:
                    return rxScript.IsMatch(address);
                case ActionSwitchType.CONTROL:
                    return rxControl.IsMatch(address) || rxControlSeq.IsMatch(address);
                case ActionSwitchType.LVAR:
                    return rxLvar.IsMatch(address);
                case ActionSwitchType.HVAR:
                    return rxHvar.IsMatch(address);
                case ActionSwitchType.OFFSET:
                    return rxOffset.IsMatch(address);
                case ActionSwitchType.VJOY:
                    return rxVjoy.IsMatch(address);
                case ActionSwitchType.VJOYDRV:
                    return rxVjoyDrv.IsMatch(address);
                case ActionSwitchType.CALCULATOR:
                    return !string.IsNullOrWhiteSpace(address);
                case ActionSwitchType.XPCMD:
                    return rxCmdXP.IsMatch(address);
                case ActionSwitchType.XPWREF:
                    return rxDref.IsMatch(address);
                default:
                    return false;
            }
        }

        public static bool IsVjoyAddress(string address, int type)
        {
            return (type == (int)ActionSwitchType.VJOY && rxVjoy.IsMatch(address))
                    || (type == (int)ActionSwitchType.VJOYDRV && rxVjoyDrv.IsMatch(address));
        }

        public static bool IsVjoyToggle(string address, int type)
        {
            return (type == (int)ActionSwitchType.VJOY && rxVjoy.IsMatch(address) && address.ToLowerInvariant().Contains(":t")) ||
                    (type == (int)ActionSwitchType.VJOYDRV && rxVjoyDrv.IsMatch(address) && address.ToLowerInvariant().Contains(":t"));
        }

        public static bool VjoyToggle(ActionSwitchType type, string address)
        {
            if (!IsVjoyToggle(address, (int)type))
                return false;

            if (type == ActionSwitchType.VJOYDRV)
                return vJoyManager.ToggleButton(address);
            else
                return SendVjoy(address, 0);
        }

        public static bool SendVjoy(string address, byte action)
        {
            try
            {
                string[] parts = address.Split(':');
                byte[] offValue = new byte[4];

                offValue[3] = 0;
                offValue[2] = action;
                offValue[1] = Convert.ToByte(parts[0]); //joy
                offValue[0] = Convert.ToByte(parts[1]); //btn

                return WriteOffset("0x29F0:4:i", BitConverter.ToUInt32(offValue, 0).ToString());
            }
            catch
            {
                Log.Logger.Error($"IPCTools: Exception while sending Virtual Joystick <{address}> to FSUIPC");
                return false;
            }
        }

        public static bool VjoyClearSet(ActionSwitchType type, string address, bool clear)
        {
            if (type == ActionSwitchType.VJOYDRV)
            {
                if (clear)
                    return vJoyManager.ClearButton(address);
                else
                    return vJoyManager.SetButton(address);
            }
            else
            {
                if (clear)
                    return SendVjoy(address, 2);
                else
                    return SendVjoy(address, 1);
            }
        }

        public static bool WriteOffset(string address, string value)
        {
            if (string.IsNullOrEmpty(address) || !FSUIPCConnection.IsOpen)
                return false;

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
                Log.Logger.Error($"IPCTools: Exception while writing Offset <{address}> (size:{offset?.Size}/float:{offset?.IsFloat}/string:{offset?.IsString}/signed:{offset?.IsSigned}) to FSUIPC! {ex.Message} - {ex.StackTrace}");
                if (offset != null)
                {
                    offset.Dispose();
                }
            }

            return result;
        }

        public static bool RunMacros(string address)
        {
            bool result = false;

            string[] tokens = address.Split(':');
            if (tokens.Length == 2)
                result = RunMacro(address);
            else
            {
                string macroFile = tokens[0];
                int fails = 0;
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (!RunMacro(macroFile + ":" + tokens[i]))
                        fails++;
                }
                if (fails == 0)
                    result = true;
            }

            return result;
        }

        public static bool RunMacro(string name)
        {
            try
            {
                Offset request = new(0x0D70, 128);
                request.SetValue(name);
                FSUIPCConnection.Process();
                request.Disconnect();
                request = null;
            }
            catch
            {
                Log.Logger.Error($"IPCTools: Exception while Executing Macro: {name}");
                return false;
            }

            return true;
        }

        public static bool WriteLvar(string address, string newValue, bool lvarReset, string offValue, bool useWASM)
        {
            bool result = false;
            if (newValue?.Length < 1)
                return result;

            double value = Convert.ToDouble(newValue, new RealInvariantFormat(newValue));
            address = address.Replace("L:", "");
            result = WriteLvar(address, value, useWASM);

            if (!result)
                return result;

            if (lvarReset && !string.IsNullOrEmpty(offValue))
            {
                Thread.Sleep(AppSettings.controlDelay * 2);
                value = Convert.ToDouble(offValue, new RealInvariantFormat(offValue));
                result = WriteLvar(address, value, useWASM);
            }

            return result;
        }

        public static bool WriteLvar(string address, double value, bool useWASM)
        {
            bool result = false;
            try
            {
                if (useWASM)
                {
                    if (WASM.LVars.Exists(address))
                    {
                        WASM.LVars[address].SetValue(value);
                        result = true;
                        //Log.Logger.Debug($"IPCTools: LVar <{name}> updated via WASM");
                    }
                    else
                        Log.Logger.Error($"IPCTools: LVar <{address}> does not exist");
                }
                else
                {
                    FSUIPCConnection.WriteLVar(address, value);
                    result = true;
                }
            }
            catch
            {
                Log.Logger.Error($"IPCTools: Exception while writing LVar <{address}:{value}> to FSUIPC/WASM");
            }

            return result;
        }

        public static bool SendControls(string address, bool useControlDelay)
        {
            if (!address.Contains('=') && address.Contains(':') && rxControlSeq.IsMatch(address))
                return SendControlsSeq(address, useControlDelay);
            else if (!address.Contains('=') && !address.Contains(':'))
                return SendControl(address);

            int fails = 0;
            
            string codeControl;
            string codeParam;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, "=");

                if (address.Length == 0)
                    if (!SendControl(codeControl))
                        fails++;
                
                while (address.Length > 0 && !PeekNextDelim(address, "="))
                {
                    codeParam = GetNextTokenMove(ref address, ":");
                    if (!SendControl(codeControl, codeParam))
                        fails++;
                    if (useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
        }

        public static bool SendControl(string control, string param = "0")
        {
            return SendControl(Convert.ToInt32(control), Convert.ToInt32(param));
        }

        public static bool SendControl(int control, int param = 0)
        {
            bool result = false;
            try
            {
                FSUIPCConnection.SendControlToFS(control, param);
                result = true;
            }
            catch
            {
                Log.Logger.Error($"IPCTools: Exception while sending Control <{control}:{param}> to FSUIPC");
            }

            return result;
        }

        public static bool RunSingleScript(string name, int flag)
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

                Offset request = new(0x0D70, 128);
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

        public static bool RunScript(string name)
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
                        Thread.Sleep(25);
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

        public static bool WriteHvar(string name)
        {
            bool result = false;

            if (!name.Contains("H:"))
                name = "H:" + name;

            try
            {
                WASM.ExecuteCalculatorCode($"(>{name})");
                result = true;
            }
            catch
            {
                Log.Logger.Error($"ConnectorMSFS: Exception while setting HVar <{name}> via WASM");
            }

            return result;
        }

        public static bool RunCalculatorCode(string code)
        {
            bool result = false;

            try
            {
                WASM.ExecuteCalculatorCode(code);
                result = true;
            }
            catch
            {
                Log.Logger.Error($"ConnectorMSFS: Exception while running Calculator Code <{code}> via WASM");
            }

            return result;
        }

        public static bool PeekNextDelim(string address, string delim)
        {
            return Regex.IsMatch(address, $"^[0-9]{{1,}}{delim}.*");
        }

        public static string GetNextTokenMove(ref string address, string delim)
        {
            string result = null;
            int matchIndex = address.IndexOf(delim);
            if (matchIndex != -1)
            {
                result = address[..matchIndex];
                address = address.Remove(0, matchIndex + 1);
            }
            else if (address.Length > 0)
            {
                result = address;
                address = "";
            }
            
            return result;
        }

        public static bool SendControlsSeq(string address, bool useControlDelay)
        {
            int fails = 0;

            string codeControl;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, ":");

                if (codeControl != null)
                {
                    if (!SendControl(codeControl))
                        fails++;
                    if (PeekNextDelim(address, ":") && useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
        }
    }
}
