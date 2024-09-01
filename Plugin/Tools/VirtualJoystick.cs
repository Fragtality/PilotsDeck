using System;
using vJoyInterfaceWrap;

namespace PilotsDeck.Tools
{
    public class VirtualJoystick
    {
        protected readonly static bool[,] stateTable = new bool[16,128];

        protected static void GetIDs(string address, out uint joyID, out uint btnID)
        {
            address = address.ToLowerInvariant().Replace("vjoy:", "");
            string[] parts = address.Split(':');
            joyID = Convert.ToUInt32(parts[0]);
            btnID = Convert.ToUInt32(parts[1]);
        }

        protected static bool WriteButton(string address, bool value, bool toggle = false)
        {
            bool result;
            GetIDs(address, out uint joyID, out uint btnID);

            vJoy joystick = new();
            result = joystick.AcquireVJD(joyID);
            if (result)
            {
                if (toggle)
                    stateTable[joyID, btnID] = !stateTable[joyID, btnID];
                else
                    stateTable[joyID, btnID] = value;

                result = joystick.SetBtn(stateTable[joyID, btnID], joyID, btnID);
            }

            joystick.RelinquishVJD(joyID);

            return result;
        }

        public static bool ToggleDriverButton(string address)
        {
            return WriteButton(address, true, true);
        }

        public static bool ClearSetDriverButton(string address, bool down)
        {
            return WriteButton(address, down);
        }

        public static bool SetDriverButton(string address)
        {
            return WriteButton(address, true);
        }

        public static bool ClearDriverButton(string address)
        {
            return WriteButton(address, false);
        }
    }
}
