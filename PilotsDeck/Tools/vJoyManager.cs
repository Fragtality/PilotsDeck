using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vJoyInterfaceWrap;

namespace PilotsDeck
{
    public class vJoyManager
    {
        protected static bool[,] stateTable = new bool[16,128];

        protected static void GetIDs(string address, out uint joyID, out uint btnID)
        {
            string[] parts = address.Split(':');
            joyID = Convert.ToUInt32(parts[0]);
            btnID = Convert.ToUInt32(parts[1]);
        }

        protected static bool WriteButton(string address, bool value, bool toggle = false)
        {
            bool result;
            GetIDs(address, out uint joyID, out uint btnID);

            vJoy joystick = new vJoy();
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

        public static bool ToggleButton(string address)
        {
            return WriteButton(address, true, true);
        }

        public static bool SetButton(string address)
        {
            return WriteButton(address, true);
        }

        public static bool ClearButton(string address)
        {
            return WriteButton(address, false);
        }
    }
}
