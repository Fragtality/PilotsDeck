using System;
using System.Threading.Tasks;
using vJoyInterfaceWrap;

namespace PilotsDeck.Tools
{
    public class VirtualJoystick
    {
        protected readonly static bool[,] stateTable = new bool[16,128];
        protected readonly static DateTime[,] timeTable = new DateTime[16,128];

        protected static void GetIDs(string address, out uint joyID, out uint btnID)
        {
            address = address.ToLowerInvariant().Replace("vjoy:", "");
            string[] parts = address.Split(':');
            joyID = Convert.ToUInt32(parts[0]);
            btnID = Convert.ToUInt32(parts[1]);
        }

        protected static async Task<bool> WriteButton(string address, bool value, bool toggle = false)
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
                {
                    if (stateTable[joyID, btnID] && !value)
                    {
                        var diff = DateTime.Now - timeTable[joyID, btnID];
                        var min = TimeSpan.FromMilliseconds(App.Configuration.VJoyMinimumPressed);
                        if (diff < min)
                        {
                            if ((min - diff) > TimeSpan.Zero)
                                await Task.Delay(min - diff, App.CancellationToken);
                            else
                                await Task.Delay(min, App.CancellationToken);
                        }
                    }
                    else if (!stateTable[joyID, btnID] && value)
                        timeTable[joyID, btnID] = DateTime.Now;

                    stateTable[joyID, btnID] = value;
                }

                result = joystick.SetBtn(stateTable[joyID, btnID], joyID, btnID);
            }
            else
                Logger.Warning($"Could not acquire VJD '{joyID}'");

            joystick.RelinquishVJD(joyID);

            return result;
        }

        public static async Task<bool> ToggleDriverButton(string address)
        {
            return await WriteButton(address, true, true);
        }

        public static async Task<bool> ClearSetDriverButton(string address, bool down)
        {
            return await WriteButton(address, down);
        }
    }
}
