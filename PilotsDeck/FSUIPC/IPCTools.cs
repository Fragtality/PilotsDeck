using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace PilotsDeck
{


    public static class IPCTools
    {
        public static readonly string validName = @"[a-zA-Z0-9\x2D\x5F]+";
        public static readonly Regex rxMacro = new ($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxScript = new ($"^(Lua(Set|Clear|Toggle)?:){{1}}{validName}(:[0-9]{{1,3}})*$", RegexOptions.Compiled);
        public static readonly Regex rxControlSeq = new (@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        public static readonly Regex rxControl = new (@"^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$", RegexOptions.Compiled);
        public static readonly Regex rxLvar = new ($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxHvar = new ($"^[^0-9]{{1}}((H:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxOffset = new (@"^((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoy = new (@"^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoyDrv = new (@"^(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsReadAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;
            else if (rxOffset.IsMatch(address) || rxLvar.IsMatch(address))
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

        public static bool RunAction(IPCManager ipcManager, string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, string offValue = null)
        {
            if (ipcManager.IsConnected && IsWriteAddress(Address, actionType))
            {
                Log.Logger.Debug($"IPCTools: Writing to {Address}");
                switch (actionType)
                {
                    case ActionSwitchType.MACRO:
                        return RunMacros(Address);
                    case ActionSwitchType.SCRIPT:
                        return RunScript(Address);
                    case ActionSwitchType.LVAR:
                        return WriteLvar(ipcManager, Address, newValue, switchSettings.UseLvarReset, offValue);
                    case ActionSwitchType.HVAR:
                        return WriteHvar(ipcManager, Address);
                    case ActionSwitchType.CONTROL:
                        return SendControls(Address, switchSettings.UseControlDelay);
                    case ActionSwitchType.OFFSET:
                        return WriteOffset(Address, newValue);
                    case ActionSwitchType.VJOY:
                        return VjoyToggle(actionType, Address);
                    case ActionSwitchType.VJOYDRV:
                        return VjoyToggle(actionType, Address);
                    case ActionSwitchType.CALCULATOR:
                        return ipcManager.RunCalculatorCode(Address);
                    default:
                        return false;
                }
            }
            else
                Log.Logger.Error($"IPCTools: not connected or Address not passed {Address}");

            return false;
        }

        public static bool VjoyToggle(ActionSwitchType type, string address)
        {
            if (!IsVjoyToggle(address, (int)type))
                return false;

            if (type == ActionSwitchType.VJOYDRV)
                return vJoyManager.ToggleButton(address);
            else
                return IPCManager.SendVjoy(address, 0);
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
                    return IPCManager.SendVjoy(address, 2);
                else
                    return IPCManager.SendVjoy(address, 1);
            }
        }

        public static bool RunScript(string address)
        {
            return IPCManager.RunScript(address);
        }

        public static bool RunMacros(string address)
        {
            bool result = false;

            string[] tokens = address.Split(':');
            if (tokens.Length == 2)
                result = IPCManager.RunMacro(address);
            else
            {
                string macroFile = tokens[0];
                int fails = 0;
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (!IPCManager.RunMacro(macroFile + ":" + tokens[i]))
                        fails++;
                }
                if (fails == 0)
                    result = true;
            }

            return result;
        }

        public static bool WriteLvar(IPCManager ipcManager, string address, string newValue, bool lvarReset, string offValue)
        {
            bool result = false;
            if (newValue?.Length < 1)
                return result;

            double value = Convert.ToDouble(newValue, new RealInvariantFormat(newValue));
            address = address.Replace("L:", "");
            result = ipcManager.WriteLvar(address, value);

            if (!result)
                return result;

            if (lvarReset && !string.IsNullOrEmpty(offValue))
            {
                Thread.Sleep(AppSettings.controlDelay * 2);
                value = Convert.ToDouble(offValue, new RealInvariantFormat(offValue));
                result = ipcManager.WriteLvar(address, value);
            }

            return result;
        }

        public static bool WriteHvar(IPCManager ipcManager, string address)
        {
            if (!address.Contains("H:"))
                address = "H:" + address;

            return ipcManager.WriteHvar(address);
        }

        public static bool SendControls(string address, bool useControlDelay)
        {
            if (!address.Contains('=') && address.Contains(':') && rxControlSeq.IsMatch(address))
                return SendControlsSeq(address, useControlDelay);
            else if (!address.Contains('=') && !address.Contains(':'))
                return IPCManager.SendControl(address);

            int fails = 0;
            
            string codeControl;
            string codeParam;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, "=");

                if (address.Length == 0)
                    if (!IPCManager.SendControl(codeControl))
                        fails++;
                
                while (address.Length > 0 && !PeekNextDelim(address, "="))
                {
                    codeParam = GetNextTokenMove(ref address, ":");
                    if (!IPCManager.SendControl(codeControl, codeParam))
                        fails++;
                    if (useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
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
                    if (!IPCManager.SendControl(codeControl))
                        fails++;
                    if (PeekNextDelim(address, ":") && useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
        }

        public static bool WriteOffset(string address, string newValue)
        {
            if (newValue != "")
                return IPCManager.WriteOffset(address, newValue);
            else
                return false;
        }
    }
}
