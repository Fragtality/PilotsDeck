using System;
using Serilog;

namespace PilotsDeck
{
    public class HandlerSwitch : HandlerBase
    {
        public virtual ModelSwitch BaseSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public virtual ModelSwitch Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerSwitch] Write: {Address} | LongWrite: {BaseSettings.HasLongPress} - {BaseSettings.AddressActionLong}"; } }
        public override string Address { get { return BaseSettings.AddressAction; } }

        public override bool HasAction { get; protected set; } = true;


        public HandlerSwitch(string context, ModelSwitch settings, StreamDeckType deckType) : base(context, settings, deckType)
        {
            Settings = settings;
        }

        public override bool OnButtonDown(IPCManager ipcManager, long tick)
        {
            tickDown = tick;
            return RunButtonDown(ipcManager, BaseSettings);
        }

        public static bool RunButtonDown(IPCManager ipcManager, IModelSwitch switchSettings)
        {
            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
                return IPCTools.VjoyClearSet(ipcManager, switchSettings.AddressAction, false);
            else if (IPCTools.IsWriteAddress(switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType))
                return true;
            else
                return false;
        }

        public override bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            bool result = RunButtonUp(ipcManager, (tick - tickDown) >= AppSettings.longPressTicks, ValueManager[ID.SwitchState], ValueManager[ID.SwitchStateLong], BaseSettings);
            tickDown = 0;

            return result;
        }

        public static bool RunButtonUp(IPCManager ipcManager, bool longPress, string lastState, string lastStateLong, IModelSwitch switchSettings)
        {
            bool result = false;

            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
            {
                result = IPCTools.VjoyClearSet(ipcManager, switchSettings.AddressAction, true);
            }
            else if (!longPress)
            {
                string newValue = "";
                if (IsActionReadable(switchSettings.ActionType))
                    newValue = ToggleValue(lastState, switchSettings.SwitchOffState, switchSettings.SwitchOnState);
                result = IPCTools.RunAction(ipcManager, switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType, newValue, switchSettings.UseControlDelay);
            }
            else if (longPress && switchSettings.HasLongPress)
            {
                if (IPCTools.IsVjoyAddress(switchSettings.AddressActionLong, switchSettings.ActionTypeLong) && IPCTools.IsVjoyToggle(switchSettings.AddressActionLong, switchSettings.ActionTypeLong))
                {
                    result = IPCTools.VjoyToggle(ipcManager, switchSettings.AddressActionLong);
                }
                else if (IPCTools.IsWriteAddress(switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong) && !IPCTools.IsVjoyAddress(switchSettings.AddressActionLong, switchSettings.ActionTypeLong))
                {
                    string newValue = "";
                    if (IsActionReadable(switchSettings.ActionTypeLong))
                        newValue = ToggleValue(lastStateLong, switchSettings.SwitchOffStateLong, switchSettings.SwitchOnStateLong);
                    result = IPCTools.RunAction(ipcManager, switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong, newValue, switchSettings.UseControlDelay);
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
            Log.Logger.Debug($"HandlerSwitch: Value toggled {lastValue} -> {newValue}");
            return newValue;
        }

        
    }
}
