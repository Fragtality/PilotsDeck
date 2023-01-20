﻿using System.Text.RegularExpressions;

namespace PilotsDeck
{


    public static class IPCTools
    {
        #region Regex
        //2D => -
        //2F => /
        //5F => _
        public static readonly string validName = @"[a-zA-Z0-9\x2D\x5F]+";
        public static readonly Regex rxMacro = new ($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxScript = new ($"^(Lua(Set|Clear|Toggle|Value)?:){{1}}{validName}(:[0-9]{{1,4}})*$", RegexOptions.Compiled);
        public static readonly Regex rxControlSeq = new (@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        public static readonly Regex rxControl = new (@"^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$", RegexOptions.Compiled);
        public static readonly Regex rxLvar = new ($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly string validHvar = $"((H:){{0,1}}{validName}){{1}}";
        public static readonly Regex rxHvar = new ($"^({validHvar}){{1}}(:{validHvar})*$", RegexOptions.Compiled);
        public static readonly Regex rxOffset = new (@"^((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoy = new (@"^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoyDrv = new (@"^(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxDref = new ($"^({validName}[\\x2F]){{1}}({validName}[\\x2F])*({validName}(([\\x5B][0-9]+[\\x5D])|(:s[0-9]+)){{0,1}}){{1}}$", RegexOptions.Compiled);
        public static readonly string validPathXP = $"({validName}[\\x2F]){{1}}({validName}[\\x2F])*({validName}){{1}}";
        public static readonly Regex rxCmdXP = new($"^({validPathXP}){{1}}(:{validPathXP})*$", RegexOptions.Compiled);
        #endregion

        #region Test-Functions
        public static bool IsReadAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;
            else if (rxOffset.IsMatch(address) || rxLvar.IsMatch(address) || rxDref.IsMatch(address))
                return true;
            else
                return false;
        }

        public static bool IsReadAddressForType(string address, ActionSwitchType type)
        {
            if (type == ActionSwitchType.READVALUE)
                type = GetReadType(address, type);

            if (string.IsNullOrEmpty(address))
                return false;
            else if ((rxOffset.IsMatch(address) && type == ActionSwitchType.OFFSET) || (rxLvar.IsMatch(address) && type == ActionSwitchType.LVAR) || (rxDref.IsMatch(address) && type == ActionSwitchType.XPWREF))
                return true;
            else
                return false;
        }

        public static ActionSwitchType GetReadType(string address, ActionSwitchType type)
        {
            if (rxOffset.IsMatch(address))
                return ActionSwitchType.OFFSET;
            if (rxLvar.IsMatch(address))
                return ActionSwitchType.LVAR;
            if (rxDref.IsMatch(address))
                return ActionSwitchType.XPWREF;

            return type;
        }

        public static bool IsActionReadable(ActionSwitchType type)
        {
            return type == ActionSwitchType.LVAR || type == ActionSwitchType.OFFSET || type == ActionSwitchType.XPWREF || type == ActionSwitchType.READVALUE;
        }

        public static bool IsActionReadable(int type)
        {
            return IsActionReadable((ActionSwitchType)type);
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

        public static bool IsToggleableCommand(int type)
        {
            return IsToggleableCommand((ActionSwitchType)type);
        }

        public static bool IsToggleableCommand(ActionSwitchType type)
        {
            return type == ActionSwitchType.XPCMD || type == ActionSwitchType.CONTROL;
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
        #endregion
    }
}
