using System;
using System.Threading;

namespace PilotsDeck
{
    public class HandlerSwitch : HandlerBase
    {
        public virtual ModelSwitch BaseSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public virtual ModelSwitch Settings { get; protected set; }

        public override string ActionID { get { return $"(HandlerSwitch) ({Title.Trim()}) {(BaseSettings.IsEncoder ? "(Encoder) " : "")}(Action: {(ActionSwitchType)BaseSettings.ActionType} / {Address}) (Long: {BaseSettings.HasLongPress} / {(ActionSwitchType)BaseSettings.ActionTypeLong} / {BaseSettings.AddressActionLong})"; } }
        public override string Address { get { return BaseSettings.AddressAction; } }

        public override bool HasAction { get; protected set; } = true;


        public HandlerSwitch(string context, ModelSwitch settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        public override bool OnButtonDown(long tick)
        {
            TickDown = tick;
            return RunButtonDown(BaseSettings);
        }

        public static bool RunButtonDown(IModelSwitch switchSettings)
        {
            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
                return SimTools.VjoyClearSet((ActionSwitchType)switchSettings.ActionType, switchSettings.AddressAction, false);
            else if (IPCTools.IsWriteAddress(switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType))
                return true;
            else
                return false;
        }

        public override bool OnButtonUp(long tick)
        {
            bool result = RunButtonUp(IPCManager, tick - TickDown, ValueManager, BaseSettings);
            TickDown = 0;

            return result;
        }

        public static bool RunButtonUp(IPCManager ipcManager, long ticks, ValueManager valueManager, IModelSwitch switchSettings)
        {
            bool result = false;
            bool longPress = ticks >= AppSettings.longPressTicks;

            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
            {
                if (ticks < 1)
                    Thread.Sleep(AppSettings.controlDelay);

                result = SimTools.VjoyClearSet((ActionSwitchType)switchSettings.ActionType, switchSettings.AddressAction, true);
            }
            else if (!longPress)
            {
                string currentState = valueManager[ID.Switch];
                if (((ActionSwitchType)switchSettings.ActionType == ActionSwitchType.XPCMD || (ActionSwitchType)switchSettings.ActionType == ActionSwitchType.CONTROL) && switchSettings.ToggleSwitch)
                    currentState = valueManager[ID.Monitor];

                result = ipcManager.RunAction(switchSettings.AddressAction, switchSettings.AddressActionOff, (ActionSwitchType)switchSettings.ActionType, currentState, switchSettings.SwitchOnState, switchSettings.SwitchOffState, switchSettings);
            }
            else if (longPress && switchSettings.HasLongPress && IPCTools.IsWriteAddress(switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong))
            {
                result = ipcManager.RunAction(switchSettings.AddressActionLong, null, (ActionSwitchType)switchSettings.ActionTypeLong, valueManager[ID.SwitchLong], switchSettings.SwitchOnStateLong, switchSettings.SwitchOffStateLong, switchSettings);
            }

            return result;
        }

        public static int RunVjoy(string address, int type)
        {
            int result = 0;
            if (IPCTools.IsVjoyAddress(address, type) && !IPCTools.IsVjoyToggle(address, type))
            {
                if (!SimTools.VjoyClearSet((ActionSwitchType)type, address, false))

                    Thread.Sleep(AppSettings.controlDelay);

                if (!SimTools.VjoyClearSet((ActionSwitchType)type, address, true))
                    result = -1;
                else
                    result = 1;
            }

            return result;
        }

        public override bool OnDialRotate(int ticks)
        {
            return RunDialRotate(IPCManager, ticks, ValueManager, BaseSettings);
        }

        public static bool RunDialRotate(IPCManager ipcManager, int ticks, ValueManager valueManager, IModelSwitch switchSettings)
        {          
            string address;
            int type;
            string value;
            string onState;
            string offState;
            bool result;

            if (ticks < 0)
            {
                address = switchSettings.AddressActionLeft;
                type = switchSettings.ActionTypeLeft;
                value = valueManager[ID.SwitchLeft];
                onState = switchSettings.SwitchOnStateLeft;
                offState = switchSettings.SwitchOffStateLeft;
            }
            else
            {
                address = switchSettings.AddressActionRight;
                type = switchSettings.ActionTypeRight;
                value = valueManager[ID.SwitchRight];
                onState = switchSettings.SwitchOnStateRight;
                offState = switchSettings.SwitchOffStateRight;
            }

            ticks = Math.Abs(ticks);

            if (IPCTools.IsVjoyAddress(address, type) && !IPCTools.IsVjoyToggle(address, type))
            {
                int i = 0;
                while (i < ticks)
                {
                    if (RunVjoy(address, type) == 1)
                        i++;
                    else
                        break;
                }

                result = i == ticks;
            }
            else
            {
                result = ipcManager.RunAction(address, "", (ActionSwitchType)type, value, onState, offState, switchSettings, true, ticks);
            }

            return result;
        }

        public override bool OnTouchTap()
        {
            return RunTouchTap(IPCManager, ValueManager, BaseSettings);
        }

        public static bool RunTouchTap(IPCManager ipcManager, ValueManager valueManager, IModelSwitch switchSettings)
        {
            int result = RunVjoy(switchSettings.AddressActionTouch, switchSettings.ActionTypeTouch);
            if (result == 0)
            {
                if (!ipcManager.RunAction(switchSettings.AddressActionTouch, "", (ActionSwitchType)switchSettings.ActionTypeTouch, valueManager[ID.SwitchTouch], switchSettings.SwitchOnStateTouch, switchSettings.SwitchOffStateTouch, switchSettings, true))
                    return false;
                else
                    return true;
            }
            else if (result != 1)
                return false;
            else
                return true;
        }
    }
}
