using System;
using System.Threading;
using System.Text.RegularExpressions;
using Serilog;
using vJoyInterfaceWrap;

namespace PilotsDeck
{


    public static class IPCTools
    {
        static string validName = @"[a-zA-Z0-9\x2D\x5F]+";
        public static Regex rxMacro = new Regex($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static Regex rxScript= new Regex($"^(Lua(Set|Clear|Toggle)?:){{1}}{validName}(:[0-9]{{1,3}})*$", RegexOptions.Compiled);
        public static Regex rxControlSeq = new Regex(@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        public static Regex rxControl = new Regex(@"^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$", RegexOptions.Compiled);
        public static Regex rxLvar = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static Regex rxOffset = new Regex(@"^((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex rxVjoy = new Regex(@"^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex rxVjoyDrv = new Regex(@"^(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


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
                case ActionSwitchType.OFFSET:
                    return rxOffset.IsMatch(address);
                case ActionSwitchType.VJOY:
                    return rxVjoy.IsMatch(address);
                case ActionSwitchType.VJOYDRV:
                    return rxVjoyDrv.IsMatch(address);
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

        public static bool RunAction(IPCManager ipcManager, string Address, ActionSwitchType actionType, string newValue, bool useControlDelay)
        {
            if (ipcManager.IsConnected && IsWriteAddress(Address, actionType))
            {
                Log.Logger.Debug($"IPCTools: Writing to {Address}");
                switch (actionType)
                {
                    case ActionSwitchType.MACRO:
                        return RunMacros(ipcManager, Address);
                    case ActionSwitchType.SCRIPT:
                        return RunScript(ipcManager, Address);
                    case ActionSwitchType.LVAR:
                        return WriteLvar(ipcManager, Address, newValue);
                    case ActionSwitchType.CONTROL:
                        return SendControls(ipcManager, Address, useControlDelay);
                    case ActionSwitchType.OFFSET:
                        return WriteOffset(ipcManager, Address, newValue);
                    case ActionSwitchType.VJOY:
                        return VjoyToggle(ipcManager, actionType, Address);
                    case ActionSwitchType.VJOYDRV:
                        return VjoyToggle(ipcManager, actionType, Address);
                    default:
                        return false;
                }
            }
            else
                Log.Logger.Error($"IPCTools: not connected or Address not passed {Address}");

            return false;
        }

        public static bool VjoyToggle(IPCManager ipcManager, ActionSwitchType type, string address)
        {
            if (!IsVjoyToggle(address, (int)type))
                return false;

            if (type == ActionSwitchType.VJOYDRV)
                return vJoyManager.ToggleButton(address);
            else
                return ipcManager.SendVjoy(address, 0);
        }

        public static bool VjoyClearSet(IPCManager ipcManager, ActionSwitchType type, string address, bool clear)
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
                    return ipcManager.SendVjoy(address, 2);
                else
                    return ipcManager.SendVjoy(address, 1);
            }
        }

        public static bool RunScript(IPCManager ipcManager, string address)
        {
            return ipcManager.RunScript(address);
        }

        public static bool RunMacros(IPCManager ipcManager, string address)
        {
            bool result = false;

            string[] tokens = address.Split(':');
            if (tokens.Length == 2)
                result = ipcManager.RunMacro(address);
            else
            {
                string macroFile = tokens[0];
                int fails = 0;
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (!ipcManager.RunMacro(macroFile + ":" + tokens[i]))
                        fails++;
                }
                if (fails == 0)
                    result = true;
            }

            return result;
        }

        public static bool WriteLvar(IPCManager ipcManager, string address, string newValue)
        {
            bool result = false;
            if (newValue?.Length < 1)
                return result;

            double value = Convert.ToDouble(newValue, new RealInvariantFormat(newValue));
            address.Replace("L:", "");
            result = ipcManager.WriteLvar(address, value);

            return result;
        }

        public static bool SendControls(IPCManager ipcManager, string address, bool useControlDelay)
        {
            if (!address.Contains("=") && address.Contains(":") && rxControlSeq.IsMatch(address))
                return SendControlsSeq(ipcManager, address, useControlDelay);
            else if (!address.Contains("=") && !address.Contains(":"))
                return ipcManager.SendControl(address);

            int fails = 0;
            
            string codeControl;
            string codeParam;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, "=");

                if (address.Length == 0)
                    if (!ipcManager.SendControl(codeControl))
                        fails++;
                
                while (address.Length > 0 && !PeekNextDelim(address, "="))
                {
                    codeParam = GetNextTokenMove(ref address, ":");
                    if (!ipcManager.SendControl(codeControl, codeParam))
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
                result = address.Substring(0, matchIndex);
                address = address.Remove(0, matchIndex + 1);
            }
            else if (address.Length > 0)
            {
                result = address;
                address = "";
            }
            
            return result;
        }

        public static bool SendControlsSeq(IPCManager ipcManager, string address, bool useControlDelay)
        {
            int fails = 0;

            string codeControl;
            while (address.Length > 0)
            {
                codeControl = GetNextTokenMove(ref address, ":");

                if (codeControl != null)
                {
                    if (!ipcManager.SendControl(codeControl))
                        fails++;
                    if (PeekNextDelim(address, ":") && useControlDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }

            return fails == 0;
        }

        public static bool WriteOffset(IPCManager ipcManager, string address, string newValue)
        {
            if (newValue != "")
                return ipcManager.WriteOffset(address, newValue);
            else
                return false;
        }
    }
}
