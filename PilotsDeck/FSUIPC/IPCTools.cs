using System.Text.RegularExpressions;

namespace PilotsDeck
{


    public static class IPCTools
    {
        static string validName = @"[a-zA-Z0-9\x2D\x5F]+";
        public static Regex rxMacro = new Regex($"^([^0-9]{{1}}{validName}:({validName}){{0,1}}(:{validName}){{0,}}){{1}}$", RegexOptions.Compiled);
        public static Regex rxScript= new Regex($"^(Lua(Set|Clear|Toggle)?:){{1}}{validName}(:[0-9]+)?$", RegexOptions.Compiled);
        public static Regex rxControl = new Regex(@"^[0-9]+(:[0-9]+)*$", RegexOptions.Compiled);
        //public static Regex rxLvar = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}(:(L:){{0,1}}{validName})*$", RegexOptions.Compiled);
        public static Regex rxLvarRead = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}$", RegexOptions.Compiled);
        public static Regex rxLvarWrite = new Regex($"^[^0-9]{{1}}((L:){{0,1}}{validName}){{1}}(:(L:){{0,1}}{validName})*$", RegexOptions.Compiled);
        public static Regex rxOffset = new Regex(@"^((0x){0,1}[0-9A-F]{4}:[0-9]{1,3}(:[ifs]{1}(:s)?)?){1}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex rxVjoy = new Regex(@"^(6[4-9]|7[0-2]){1}:(0?[0-9]|1[0-9]|2[0-9]|3[0-1]){1}(:t)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static bool IsReadAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;
            else if (rxOffset.IsMatch(address) || rxLvarRead.IsMatch(address))
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
                    return rxControl.IsMatch(address);
                case ActionSwitchType.LVAR:
                    return rxLvarWrite.IsMatch(address);
                case ActionSwitchType.OFFSET:
                    return rxOffset.IsMatch(address);
                case ActionSwitchType.VJOY:
                    return rxVjoy.IsMatch(address);
                default:
                    return false;
            }
        }

        public static bool IsVjoyAddress(string address, int type)
        {
            return type == (int)ActionSwitchType.VJOY && rxVjoy.IsMatch(address);
        }

        public static bool IsVjoyToggle(string address, int type)
        {
            return type == (int)ActionSwitchType.VJOY && rxVjoy.IsMatch(address) && address.ToLowerInvariant().Contains(":t");
        }

        //public static bool GetVjoyValues(string address, out byte joy, out byte btn)
        //{
        //    string[] parts = address.Split(':');

        //    if (byte.TryParse(parts[0], out joy) && byte.TryParse(parts[1], out btn))
        //        return true;
        //    else
        //    {
        //        joy = 64;
        //        btn = 0;
        //        return false;
        //    }
        //}
    }
}
