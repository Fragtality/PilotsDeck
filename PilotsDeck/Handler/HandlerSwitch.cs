using System;
using Serilog;

namespace PilotsDeck
{
    public class HandlerSwitch : HandlerBase, IHandlerSwitch
    {
        public virtual ModelSwitch BaseSettings { get { return Settings; } }
        public virtual ModelSwitch Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerSwitch] Write: {Address} | LongWrite: {BaseSettings.HasLongPress} - {BaseSettings.AddressActionLong}"; } }
        public override string Address { get { return BaseSettings.AddressAction; } }

        public virtual long tickDown { get; protected set; }
        protected virtual string LastSwitchState { get; set; }
        protected virtual string LastSwitchStateLong { get; set; }


        public HandlerSwitch(string context, ModelSwitch settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
            LastSwitchState = settings.OffState;
            LastSwitchStateLong = settings.OffStateLong;
        }

        public override void Update(ImageManager imgManager, IPCManager ipcManager)
        {
            base.Update(imgManager, ipcManager);

            if (this.GetType().IsAssignableFrom(typeof(HandlerSwitch)) &&
                ((LastSwitchState != BaseSettings.OffState && LastSwitchState != BaseSettings.OnState) ||
                (LastSwitchStateLong != BaseSettings.OffStateLong && LastSwitchStateLong != BaseSettings.OnStateLong)))
            {
                LastSwitchState = BaseSettings.OffState;
                LastSwitchStateLong = BaseSettings.OffStateLong;
            }
        }

        public virtual bool OnButtonDown(IPCManager ipcManager, long tick)
        {
            tickDown = tick;
            return RunButtonDown(ipcManager, BaseSettings);
        }

        public static bool RunButtonDown(IPCManager ipcManager, ModelSwitch switchSettings)
        {
            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
                return VjoyClearSet(ipcManager, switchSettings.AddressAction, false);
            else if (IPCTools.IsWriteAddress(switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType))
                return true;
            else
                return false;
        }

        public virtual bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            bool result = RunButtonUp(ipcManager, (tick - tickDown) >= AppSettings.longPressTicks, LastSwitchState, LastSwitchStateLong, BaseSettings, out string[] newValues);
            LastSwitchState = newValues[0];
            LastSwitchStateLong = newValues[1];
            tickDown = 0;

            return result;
        }

        public static bool RunButtonUp(IPCManager ipcManager, bool longPress, string lastState, string lastStateLong, ModelSwitch switchSettings, out string[] newValues)
        {
            bool result = false;
            newValues = new string[2];
            newValues[0] = lastState;
            newValues[1] = lastStateLong;

            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
            {
                result = VjoyClearSet(ipcManager, switchSettings.AddressAction, true);
            }
            else if (!longPress)
            {
                string newValue = ToggleValue(lastState, switchSettings.OffState, switchSettings.OnState);
                result = RunAction(ipcManager, switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType, newValue);
                if (result)
                    newValues[0] = newValue;
            }
            else if (longPress && switchSettings.HasLongPress)
            {
                if (IPCTools.IsVjoyAddress(switchSettings.AddressActionLong, switchSettings.ActionTypeLong) && IPCTools.IsVjoyToggle(switchSettings.AddressActionLong, switchSettings.ActionTypeLong))
                {
                    result = VjoyToggle(ipcManager, switchSettings.AddressActionLong);
                }
                else if (IPCTools.IsWriteAddress(switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong) && !IPCTools.IsVjoyAddress(switchSettings.AddressActionLong, switchSettings.ActionTypeLong))
                {
                    string newValue = ToggleValue(lastStateLong, switchSettings.OffStateLong, switchSettings.OnStateLong);
                    result = RunAction(ipcManager, switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong, newValue);
                    if (result)
                        newValues[1] = newValue;
                }
            }

            return result;
        }

        public static string ToggleValue(string lastValue, string offState, string onState)
        {
            string newValue;
            if (lastValue == offState)
                newValue = onState;
            else
                newValue = offState;
            Log.Logger.Debug($"Value toggled {lastValue} -> {newValue}");
            return newValue;
        }

        public static bool RunAction(IPCManager ipcManager, string Address, ActionSwitchType actionType, string newValue)
        {
            if (ipcManager.IsConnected && IPCTools.IsWriteAddress(Address, actionType))
            {
                Log.Logger.Debug($"HandlerBase:RunAction Writing to {Address}");
                switch (actionType)
                {
                    case ActionSwitchType.MACRO:
                        return RunMacros(ipcManager, Address);
                    case ActionSwitchType.SCRIPT:
                        return RunScript(ipcManager, Address);
                    case ActionSwitchType.LVAR:
                        return WriteLvars(ipcManager, Address, newValue);
                    case ActionSwitchType.CONTROL:
                        return SendControls(ipcManager, Address);
                    case ActionSwitchType.OFFSET:
                        return WriteOffset(ipcManager, Address, newValue);
                    case ActionSwitchType.VJOY:
                        if (IPCTools.IsVjoyToggle(Address, (int)actionType))
                            return VjoyToggle(ipcManager, Address);
                        else
                            return false;
                    default:
                        return false;
                }
            }
            else
                Log.Logger.Error($"HandlerBase:RunAction not connected or Address not passed {Address}");

            return false;
        }

        public static bool VjoyToggle(IPCManager ipcManager, string address)
        {
            return ipcManager.SendVjoy(address, 0);
        }

        public static bool VjoyClearSet(IPCManager ipcManager, string address, bool clear)
        {
            if (clear)
                return ipcManager.SendVjoy(address, 2);
            else
                return ipcManager.SendVjoy(address, 1);
        }

        public static bool RunScript(IPCManager ipcManager, string address)
        {
            return ipcManager.RunScript(address);
        }

        public static bool RunMacros(IPCManager ipcManager, string address)
        {
            bool result = false;

            string[] tokens = address.Split(':');
            if (tokens.Length == 2)
                result = ipcManager.RunMacro(address);
            else
            {
                string macroFile = tokens[0];
                int fails = 0;
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (!ipcManager.RunMacro(macroFile + ":" + tokens[i]))
                        fails++;
                }
                if (fails == 0)
                    result = true;
            }

            return result;
        }

        public static bool WriteLvars(IPCManager ipcManager, string address, string newValue)
        {
            bool result = false;
            if (newValue?.Length < 1)
                return result;

            double value = Convert.ToDouble(newValue);

            string[] vars = address.Replace("L:", "").Split(':');
            if (vars.Length > 1)
            {
                int fails = 0;
                for (int i = 0; i < vars.Length; i++)
                {
                    if (!ipcManager.WriteLvar(vars[i], value))
                        fails++;
                }
                if (fails == 0)
                    result = true;
            }
            else
            {
                result = ipcManager.WriteLvar(address, value);
            }

            return result;
        }

        public static bool SendControls(IPCManager ipcManager, string address)
        {
            bool result = false;

            string[] args = address.Split(':');
            if (args.Length == 2)
                result = ipcManager.SendControl(args[0], args[1]);
            else if (args.Length == 1)
                result = ipcManager.SendControl(args[0]);
            else if (args.Length > 2)
            {
                string control = args[0];
                int fails = 0;
                for (int i = 0; i < args.Length; i++)
                {
                    if (!ipcManager.SendControl(control, args[i]))
                        fails++;
                }
                if (fails == 0)
                    result = true;
            }
            else
            {
                Log.Logger.Error($"HandlerBase: Could not resolve Control-Address: {address}");
                return false;
            }

            return result;
        }

        public static bool WriteOffset(IPCManager ipcManager, string address, string newValue)
        {
            if (newValue != "")
                return ipcManager.WriteOffset(address, newValue);
            else
                return false;
        }
    }
}
