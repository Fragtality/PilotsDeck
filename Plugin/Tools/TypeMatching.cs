using PilotsDeck.Resources.Variables;
using PilotsDeck.Simulator;
using System.Text.RegularExpressions;

namespace PilotsDeck.Tools
{
    public static class TypeMatching
    {
        //2B => +
        //2D => -
        //2F => /
        //5F => _
        //2E => .
        //2C => ,
        //3A => :
        public static readonly string validName = @"[^:\s][a-zA-Z0-9\x2D\x5F]+";
        public static readonly string validNameXP = @"[^:\s][a-zA-Z0-9\x2D\x5F\x2B]+";
        public static readonly string validNameKvar = @"[^:\s][a-zA-Z0-9\x2D\x5F\x2E]+";
        public static readonly string validNameMultiple = @"[a-zA-Z0-9\x2D\x5F]+";
        public static readonly string validNameMultipleXP = @"[a-zA-Z0-9\x2D\x5F\x2B]+";
        public static readonly string validLVarName = @"[^:\s][a-zA-Z0-9\x2D\x5F\x2E\x20]+([\x3A][0-9]+){0,1}";
        public static readonly Regex rxMacro = new($"^([^:0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static readonly Regex rxScript = new($"^(Lua(Set|Clear|Toggle|Value)?:){{1}}{validName}(:[0-9]{{1,4}})*$", RegexOptions.Compiled);
        public static readonly Regex rxControlSeq = new(@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        public static readonly Regex rxControl = new(@"^([0-9]+)$|^(([0-9]+\=[0-9]+(:[0-9]+)*){1}(:([0-9]+\=[0-9]+(:[0-9]+)*){1})*)$", RegexOptions.Compiled);
        public static readonly Regex rxLvar = new($"^((L:|[^:0-9]){{1}}{validLVarName}){{1}}$", RegexOptions.Compiled);
        public static readonly string validHvar = $"((?!K:)(?!B:)(H:|[^:0-9]){{1}}{validName}(:[0-9]+){{0,1}}){{1}}";
        public static readonly Regex rxHvar = new($"^({validHvar}){{1}}(:{validHvar})*$", RegexOptions.Compiled);
        public static readonly Regex rxOffset = new(@"^((0x){0,1}[0-9A-Fa-f]{4}:[0-9]{1,3}((:[ifs]{1}(:s)?)|(:b:[0-9]{1,2}))?){1}$", RegexOptions.Compiled);
        public static readonly Regex rxVjoy = new(@"^(vjoy:|vJoy:|VJOY:){0,1}(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxVjoyDrv = new(@"^(vjoy:|vJoy:|VJOY:){0,1}(1[0-6]|[0-9]){1}:([0-9]|[0-9]{2}|1[0-1][0-9]|12[0-8]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxDref = new($"^({validNameXP}[\\x2F]){{1}}({validNameMultipleXP}[\\x2F])*({validNameMultipleXP}(([\\x5B][0-9]+[\\x5D])|(:s[0-9]+)){{0,1}}){{1}}$", RegexOptions.Compiled);
        public static readonly string validPathXP = $"({validNameXP}[\\x2F]){{1}}({validNameMultipleXP}[\\x2F])*({validNameMultipleXP}){{1}}";
        public static readonly Regex rxCmdXP = new($"^({validPathXP}){{1}}(:{validPathXP})*$", RegexOptions.Compiled);
        public static readonly Regex rxAvar = new(@"^\((A:){0,1}[\w][\w ]+(:\d+){0,1},\s{0,1}([\w][\w/ ]+)\)$", RegexOptions.Compiled);
        public static readonly Regex rxAvarMobiString = new(@"^\(A:[\w][\w ]+(:\d+){0,1},\s{0,1}string\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxLvarMobi = new($"^\\(L:({validLVarName}){{1}}\\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxLvarMobiMatch = new(@"([^:\s][a-zA-Z0-9\x2D\x5F\x2E\x20]{2,}([\x3A][0-9]+){0,1}){1}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex rxBvarValue = new($"^(B:{validName}){{1}}$", RegexOptions.Compiled);
        public static readonly string validBvarCmd = $"((B:){{0,1}}{validName}(:[-+]?[0-9]+([,.]{{1}}[0-9]+)?){{0,1}}){{1}}";
        public static readonly Regex rxBvarCmd = new($"^({validBvarCmd}){{1}}(:{validBvarCmd})*$", RegexOptions.Compiled);
        public static readonly Regex rxLuaFunc = new($"^(Lua|lua|LUA){{1}}:{validName}(\\.lua){{0,1}}(:{validName}){{1}}(\\({{1}}[^\\)]+\\){{1}}){{0,1}}$", RegexOptions.Compiled);
        public static readonly Regex rxLuaFile = new($"^(Lua|lua|LUA){{1}}:{validName}(\\.lua){{0,1}}", RegexOptions.Compiled);
        public static readonly Regex rxInternal = new($"^X:{validName}$", RegexOptions.Compiled);
        public static readonly Regex rxCalcRead = new(@"^C:[^\s].+$", RegexOptions.Compiled);
        public static readonly string validKvar = $"((?!H:)(?!B:)(K:|[^:0-9]){{1}}{validNameKvar}(:[0-9.]+(:[0-9.]+){{0,1}}){{0,1}}){{1}}";
        public static readonly Regex rxKvar = new($"^({validKvar}){{1}}(:{validKvar})*$", RegexOptions.Compiled);

        public static bool IsStringDataRef(string address)
        {
            if (rxDref.IsMatch(address))
            {
                var parts = address.Split(':');
                return parts.Length == 2 && parts[1].Length >= 2;
            }
            else
                return false;
        }

        public static SimValueType GetReadType(string address)
        {
            if (rxLuaFunc.IsMatch(address))
                return SimValueType.LUAFUNC;
            if (rxBvarValue.IsMatch(address))
                return SimValueType.BVAR;
            if (rxOffset.IsMatch(address))
                return SimValueType.OFFSET;
            if (rxDref.IsMatch(address))
                return SimValueType.XPDREF;
            if (rxAvar.IsMatch(address))
                return SimValueType.AVAR;
            if (rxInternal.IsMatch(address))
                return SimValueType.INTERNAL;
            if (rxCalcRead.IsMatch(address))
                return SimValueType.CALCULATOR;
            if (rxLvar.IsMatch(address))
                return SimValueType.LVAR;

            return 0;
        }

        public static SimCommandType? GetCommandOnlyType(string address)
        {
            if (rxLuaFunc.IsMatch(address))
                return SimCommandType.LUAFUNC;
            if (rxCmdXP.IsMatch(address))
                return SimCommandType.XPCMD;
            if (rxScript.IsMatch(address))
                return SimCommandType.SCRIPT;
            if (rxMacro.IsMatch(address))
                return SimCommandType.MACRO;
            if (rxControl.IsMatch(address) || rxControlSeq.IsMatch(address))
                return SimCommandType.CONTROL;
            if (rxVjoy.IsMatch(address))
                return SimCommandType.VJOY;
            if (rxVjoyDrv.IsMatch(address))
                return SimCommandType.VJOYDRV;
            if (rxBvarCmd.IsMatch(address))
                return SimCommandType.BVAR;
            if (rxHvar.IsMatch(address))
                return SimCommandType.HVAR;
            if (rxKvar.IsMatch(address))
                return SimCommandType.KVAR;

            return null;
        }

        public static SimCommandType? GetCommandValueType(string address)
        {

            if (rxDref.IsMatch(address))
                return SimCommandType.XPWREF;
            if (rxOffset.IsMatch(address))
                return SimCommandType.OFFSET;
            if (rxAvar.IsMatch(address))
                return SimCommandType.AVAR;
            if (rxBvarValue.IsMatch(address))
                return SimCommandType.BVAR;
            if (rxInternal.IsMatch(address))
                return SimCommandType.INTERNAL;
            if (rxLvar.IsMatch(address))
                return SimCommandType.LVAR;

            return null;
        }
    }
}
