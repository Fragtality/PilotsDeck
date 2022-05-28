﻿using Serilog;

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

        public override bool OnButtonDown(long tick)
        {
            TickDown = tick;
            return RunButtonDown(BaseSettings);
        }

        public static bool RunButtonDown(IModelSwitch switchSettings)
        {
            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
                return IPCTools.VjoyClearSet((ActionSwitchType)switchSettings.ActionType, switchSettings.AddressAction, false);
            else if (IPCTools.IsWriteAddress(switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType))
                return true;
            else
                return false;
        }

        public override bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            bool result = RunButtonUp(ipcManager, (tick - TickDown) >= AppSettings.longPressTicks, ValueManager[ID.SwitchState], ValueManager[ID.SwitchStateLong], BaseSettings);
            TickDown = 0;

            return result;
        }

        public static bool RunButtonUp(IPCManager ipcManager, bool longPress, string lastState, string lastStateLong, IModelSwitch switchSettings)
        {
            bool result = false;

            if (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
            {
                result = IPCTools.VjoyClearSet((ActionSwitchType)switchSettings.ActionType, switchSettings.AddressAction, true);
            }
            else if (!longPress)
            {
                string newValue = "";
                if (IsActionReadable(switchSettings.ActionType) && !switchSettings.UseLvarReset)
                    newValue = ToggleValue(lastState, switchSettings.SwitchOffState, switchSettings.SwitchOnState);
                else if (IsActionReadable(switchSettings.ActionType))
                    newValue = switchSettings.SwitchOnState;

                result = IPCTools.RunAction(ipcManager, switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType, newValue, switchSettings, switchSettings.SwitchOffState);
            }
            else if (longPress && switchSettings.HasLongPress)
            {
                if (IPCTools.IsVjoyAddress(switchSettings.AddressActionLong, switchSettings.ActionTypeLong) && IPCTools.IsVjoyToggle(switchSettings.AddressActionLong, switchSettings.ActionTypeLong))
                {
                    result = IPCTools.VjoyToggle((ActionSwitchType)switchSettings.ActionTypeLong, switchSettings.AddressActionLong);
                }
                else if (IPCTools.IsWriteAddress(switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong) && !IPCTools.IsVjoyAddress(switchSettings.AddressActionLong, switchSettings.ActionTypeLong))
                {
                    string newValue = "";
                    if (IsActionReadable(switchSettings.ActionTypeLong) && !switchSettings.UseLvarReset)
                        newValue = ToggleValue(lastStateLong, switchSettings.SwitchOffStateLong, switchSettings.SwitchOnStateLong);
                    else if (IsActionReadable(switchSettings.ActionTypeLong))
                        newValue = switchSettings.SwitchOnStateLong;

                    result = IPCTools.RunAction(ipcManager, switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong, newValue, switchSettings, switchSettings.SwitchOffStateLong);
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
