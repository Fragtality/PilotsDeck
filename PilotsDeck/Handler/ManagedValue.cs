
namespace PilotsDeck
{
    public static class ID
    {
        //Values
        public static readonly int Control = 0;
        public static readonly int Monitor = 1;

        public static readonly int Switch = 2;
        public static readonly int SwitchLong = 3;
        public static readonly int SwitchLeft = 4;
        public static readonly int SwitchRight = 5;
        public static readonly int SwitchTouch = 6;

        public static readonly int Active = 0;
        public static readonly int Standby = 7;

        public static readonly int Top = 0;
        public static readonly int Bottom = 7;

        public static readonly int Gauge = 0;
        public static readonly int GaugeColor = 8;
        public static readonly int GaugeFirst = 0;
        public static readonly int GaugeSecond = 7;

        public static readonly int Guard = 18;
        public static readonly int GuardCmd = 19;

        //Images
        public static readonly int Wait = 9;
        public static readonly int Default = 10;
        public static readonly int Error = 11;
        public static readonly int On = 12;
        public static readonly int Off = 13;
        public static readonly int Indication = 14;
        public static readonly int ImgTop = 15;
        public static readonly int ImgBot = 16;
        public static readonly int Map = 17;

        public static readonly int ImgGuard = 20;
        public static readonly int MapGuard = 21;



        public static readonly string[] names = ["Control", "Monitor", "Switch", "SwitchLong", "SwitchLeft", "SwitchRight", "SwitchTouch", "SecondControl", "GaugeColor"];
        public static string str(int id)
        {
            if (id >= 0 && id < names.Length)
                return names[id];
            else
                return id.ToString();
        }
    }

    public enum ActionSwitchType
    {
        MACRO = 0,
        SCRIPT = 1,
        CONTROL = 2,
        LVAR = 3,
        OFFSET = 4,
        READVALUE = 5, //Offset or LVar or DRef
        VJOY = 6, //FSUIPC vJoy
        VJOYDRV = 7, //vJoy Driver
        HVAR = 8,
        CALCULATOR = 9,
        XPCMD = 10,
        XPWREF = 11,
        AVAR = 12,
        BVAR = 13,
        LUAFUNC = 14
    }

    public class ManagedValue(int id, string address, ActionSwitchType type = ActionSwitchType.READVALUE, IPCValue value = null)
    {
        public int ID { get; set; } = id;
        public string Address { get; set; } = address;
        public ActionSwitchType Type { get; set; } = type;
        public IPCValue Value { get; set; } = value;
    }
}
