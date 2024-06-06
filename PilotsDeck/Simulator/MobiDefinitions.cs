using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PilotsDeck
{
    public enum MOBIFLIGHT_CLIENT_DATA_ID
    {
        MOBIFLIGHT_LVARS,
        MOBIFLIGHT_CMD,
        MOBIFLIGHT_RESPONSE,
        MOBIFLIGHT_STRINGVARS
    }

    public enum PILOTSDECK_CLIENT_DATA_ID
    {
        MOBIFLIGHT_LVARS = 1984,
        MOBIFLIGHT_CMD,
        MOBIFLIGHT_RESPONSE,
        MOBIFLIGHT_STRINGVARS
    }

    public enum SIMCONNECT_REQUEST_ID
    {
        Dummy = 0,
        EnumEvents = 101,
        GetEvent = 102
    }

    public enum SIMCONNECT_DEFINE_ID
    {
        CLIENT_MOBI = 0,
        CLIENT_PILOTSDECK = 1
    }

    public enum SIMCONNECT_NOTIFICATION_GROUP_ID
    {
        SIMCONNECT_GROUP_PRIORITY_DEFAULT,
        SIMCONNECT_GROUP_PRIORITY_HIGHEST
    }
    public enum SIM_EVENTS
    {
        Dummy = 0
    };

    public enum NOTFIY_GROUP
    {
        INTERNAL = 1,
        DYNAMIC = 2
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDataValue
    {
        public float data;
    }

    public struct ClientDataStringValue
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public String data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClientDataString
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
        public byte[] data;

        public ClientDataString(string strData)
        {
            byte[] txtBytes = Encoding.ASCII.GetBytes(strData);
            var ret = new byte[1024];
            Array.Copy(txtBytes, ret, txtBytes.Length);
            data = ret;
        }
    }

    public struct ResponseString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MobiSimConnect.MOBIFLIGHT_MESSAGE_SIZE)]
        public String Data;
    }
}
