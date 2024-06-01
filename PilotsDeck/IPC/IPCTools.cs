using System.Text.RegularExpressions;

namespace PilotsDeck
{


    public static class IPCTools
    {
        #region Regex
        //2B => +
        //2D => -
        //2F => /
        //5F => _
        //2E => .
        //3A => :
        public static readonly string validName = @"[^:\s][a-zA-Z0-9\x2D\x5F]+";
        public static readonly string validNameXP = @"[^:\s][a-zA-Z0-9\x2D\x5F\x2B]+";
        public static readonly string validNameMultiple = @"[a-zA-Z0-9\x2D\x5F]+";
        public static readonly string validNameMultipleXP = @"[a-zA-Z0-9\x2D\x5F\x2B]+";
        public static readonly string validLVarName = @"[^:\s][a-zA-Z0-9\x2D\x5F\x2E\x20]+([\x3A][0-9]+){0,1}";
        public static readonly Regex rxMacro = new($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxScript = new($"^(Lua(Set|Clear|Toggle|Value)?:){{1}}{validName}(:[0-9]{{1,4}})*$", RegexOptions.Compiled);
        public static readonly Regex rxControlSeq = new(@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        public static readonly Regex rxControl = new(@"^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$", RegexOptions.Compiled);
        public static readonly Regex rxLvar = new($"^((L:){{0,1}}{validLVarName}){{1}}$", RegexOptions.Compiled);
        public static readonly string validHvar = $"((H:){{0,1}}{validName}){{1}}";
        public static readonly Regex rxHvar = new($"^({validHvar}){{1}}(:{validHvar})*$", RegexOptions.Compiled);
        public static readonly Regex rxOffset = new(@"^((0x){0,1}[0-9A-Fa-f]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$", RegexOptions.Compiled);
        public static readonly Regex rxVjoy = new(@"^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoyDrv = new(@"^(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxDref = new($"^({validNameXP}[\\x2F]){{1}}({validNameMultipleXP}[\\x2F])*({validNameMultipleXP}(([\\x5B][0-9]+[\\x5D])|(:s[0-9]+)){{0,1}}){{1}}$", RegexOptions.Compiled);
        public static readonly string validPathXP = $"({validNameXP}[\\x2F]){{1}}({validNameMultipleXP}[\\x2F])*({validNameMultipleXP}){{1}}";
        public static readonly Regex rxCmdXP = new($"^({validPathXP}){{1}}(:{validPathXP})*$", RegexOptions.Compiled);
        public static readonly Regex rxAvar = new(@"^\((A:){0,1}[\w][\w ]+(:\d+){0,1},\s{0,1}[\w][\w ]+\)$", RegexOptions.Compiled);
        public static readonly Regex rxLvarMobi = new($"^\\(L:({validLVarName}){{1}}\\)$", RegexOptions.Compiled);
        public static readonly Regex rxBvar = new($"^(B:{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxLuaFunc = new($"^(Lua|lua){{1}}:{validName}(\\.lua){{0,1}}:{validName}", RegexOptions.Compiled);
        #endregion

        #region Test-Functions
        public static bool IsReadAddressForType(string address, ActionSwitchType type)
        {
            if (type == ActionSwitchType.READVALUE)
                type = GetReadType(address, type);

            if (string.IsNullOrEmpty(address))
                return false;
            else if ((rxOffset.IsMatch(address) && type == ActionSwitchType.OFFSET)
                    || (rxLvar.IsMatch(address) && type == ActionSwitchType.LVAR)
                    || (rxDref.IsMatch(address) && type == ActionSwitchType.XPWREF)
                    || (rxAvar.IsMatch(address) && type == ActionSwitchType.AVAR)
                    || (rxBvar.IsMatch(address) && type == ActionSwitchType.BVAR)
                    || (rxLuaFunc.IsMatch(address) && type == ActionSwitchType.LUAFUNC))
                return true;
            else
                return false;
        }

        public static ActionSwitchType GetReadType(string address, ActionSwitchType type)
        {
            if (rxLuaFunc.IsMatch(address))
                return ActionSwitchType.LUAFUNC;
            if (rxBvar.IsMatch(address))
                return ActionSwitchType.BVAR;
            if (rxOffset.IsMatch(address))
                return ActionSwitchType.OFFSET;
            if (rxDref.IsMatch(address))
                return ActionSwitchType.XPWREF;
            if (rxAvar.IsMatch(address))
                return ActionSwitchType.AVAR;
            if (rxLvar.IsMatch(address))
                return ActionSwitchType.LVAR;

            return type;
        }

        public static ActionSwitchType GetCommandType(string address, ActionSwitchType type)
        {
            if (rxLuaFunc.IsMatch(address))
                return ActionSwitchType.LUAFUNC;
            if (rxCmdXP.IsMatch(address))
                return ActionSwitchType.XPCMD;
            if (rxScript.IsMatch(address))
                return ActionSwitchType.SCRIPT;
            if (rxMacro.IsMatch(address))
                return ActionSwitchType.MACRO;
            if (rxControl.IsMatch(address) || rxControlSeq.IsMatch(address))
                return ActionSwitchType.CONTROL;
            if (rxVjoy.IsMatch(address))
                return ActionSwitchType.VJOY;
            if (rxVjoyDrv.IsMatch(address))
                return ActionSwitchType.VJOYDRV;
            if (rxHvar.IsMatch(address))
                return ActionSwitchType.HVAR;

            return type;
        }

        public static bool IsActionReadable(ActionSwitchType type)
        {
            return type == ActionSwitchType.LVAR || type == ActionSwitchType.OFFSET || type == ActionSwitchType.XPWREF
                || type == ActionSwitchType.AVAR || type == ActionSwitchType.READVALUE || type == ActionSwitchType.BVAR || type == ActionSwitchType.LUAFUNC;
        }

        public static bool IsReadableScript(string address, ActionSwitchType type)
        {
            return type == ActionSwitchType.READVALUE && rxLuaFunc.IsMatch(address);
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
                case ActionSwitchType.LUAFUNC:
                    return rxLuaFunc.IsMatch(address);
                case ActionSwitchType.MACRO:
                    return rxMacro.IsMatch(address);
                case ActionSwitchType.SCRIPT:
                    return rxScript.IsMatch(address);
                case ActionSwitchType.CONTROL:
                    return rxControl.IsMatch(address) || rxControlSeq.IsMatch(address);
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
                case ActionSwitchType.AVAR:
                    return rxAvar.IsMatch(address);
                case ActionSwitchType.BVAR:
                    return rxBvar.IsMatch(address);
                case ActionSwitchType.LVAR:
                    return rxLvar.IsMatch(address);
                default:
                    return false;
            }
        }

        public static bool IsTickAction(int type, string address, string onValue)
        {
            return IsTickAction((ActionSwitchType)type, address, onValue);
        }

        public static bool IsTickAction(ActionSwitchType type, string address, string onValue)
        {
            return (IsActionReadable(type) && !string.IsNullOrWhiteSpace(onValue) && onValue[0] == '$')
                   || (type == ActionSwitchType.CALCULATOR && !string.IsNullOrWhiteSpace(address) && address[0] == '$');
        }

        public static bool IsToggleableCommand(int type)
        {
            return IsToggleableCommand((ActionSwitchType)type);
        }

        public static bool IsToggleableCommand(ActionSwitchType type)
        {
            return type == ActionSwitchType.XPCMD || type == ActionSwitchType.CONTROL || type == ActionSwitchType.CALCULATOR || type == ActionSwitchType.LUAFUNC
                   || type == ActionSwitchType.SCRIPT || type == ActionSwitchType.HVAR || type == ActionSwitchType.MACRO || type == ActionSwitchType.BVAR;
        }

        public static bool IsHoldableCommand(int type)
        {
            return IsHoldableCommand((ActionSwitchType)type);
        }

        public static bool IsHoldableCommand(ActionSwitchType type)
        {
            return type == ActionSwitchType.CONTROL || type == ActionSwitchType.XPCMD || type == ActionSwitchType.MACRO || type == ActionSwitchType.CALCULATOR
                   || type == ActionSwitchType.SCRIPT || type == ActionSwitchType.HVAR || type == ActionSwitchType.LUAFUNC;
        }

        public static bool IsHoldableValue(int type)
        {
            return IsHoldableValue((ActionSwitchType)type);
        }

        public static bool IsHoldableValue(ActionSwitchType type)
        {
            return type == ActionSwitchType.AVAR || type == ActionSwitchType.LVAR || type == ActionSwitchType.OFFSET || type == ActionSwitchType.XPWREF || type == ActionSwitchType.BVAR;
        }

        public static bool IsResetableValue(int type)
        {
            return IsResetableValue((ActionSwitchType)type);
        }

        public static bool IsResetableValue(ActionSwitchType type)
        {
            return type == ActionSwitchType.AVAR || type == ActionSwitchType.LVAR || type == ActionSwitchType.XPWREF || type == ActionSwitchType.OFFSET || type == ActionSwitchType.BVAR;
        }
        public static bool IsVjoyAddress(string address, int type)
        {
            return IsVjoyAddress(address, (ActionSwitchType)type);
        }

        public static bool IsVjoyAddress(string address, ActionSwitchType type)
        {
            return (type == ActionSwitchType.VJOY && rxVjoy.IsMatch(address))
                    || (type == ActionSwitchType.VJOYDRV && rxVjoyDrv.IsMatch(address));
        }

        public static bool IsVjoyToggle(string address, int type)
        {
            return IsVjoyToggle(address, (ActionSwitchType)type);
        }

        public static bool IsVjoyToggle(string address, ActionSwitchType type)
        {
            return (type == ActionSwitchType.VJOY && rxVjoy.IsMatch(address) && address.ToLowerInvariant().Contains(":t")) ||
                    (type == ActionSwitchType.VJOYDRV && rxVjoyDrv.IsMatch(address) && address.ToLowerInvariant().Contains(":t"));
        }
        #endregion
    }
}
