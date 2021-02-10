using System.Text.RegularExpressions;

namespace PilotsDeck
{


    public static class IPCTools
    {
        static string validName = @"[a-zA-Z0-9\x2D\x5F]+";
        public static Regex rxMacro = new Regex($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static Regex rxScript= new Regex($"^(Lua:){{1}}{validName}$", RegexOptions.Compiled);
        public static Regex rxControl = new Regex(@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        //public static Regex rxLvar = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}(:(L:){{0,1}}{validName})*$", RegexOptions.Compiled);
        public static Regex rxLvarRead = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static Regex rxLvarWrite = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}(:(L:){{0,1}}{validName})*$", RegexOptions.Compiled);
        public static Regex rxOffset = new Regex(@"^((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}(:[ifs]{1}(:s)?)?){1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static bool IsReadAddress(string address)
        {
            if (address == null || address == "" || address == string.Empty)
                return false;
            else if (rxOffset.IsMatch(address) || rxLvarRead.IsMatch(address))
                return true;
            else
                return false;
        }

        public static bool IsWriteAddress(string address, ActionSwitchType type)
        {
            if (address == null || address == "" || address == string.Empty)
                return false;

            switch (type)
            {
                case ActionSwitchType.MACRO:
                    return rxMacro.IsMatch(address);
                case ActionSwitchType.SCRIPT:
                    return rxScript.IsMatch(address);
                case ActionSwitchType.CONTROL:
                    return rxControl.IsMatch(address);
                case ActionSwitchType.LVAR:
                    return rxLvarWrite.IsMatch(address);
                case ActionSwitchType.OFFSET:
                    return rxOffset.IsMatch(address);
                default:
                    return false;
            }
        }
    }
}
