using System;
using System.Threading;

namespace PilotsDeck
{
    public class HandlerSwitch(string context, ModelSwitch settings, StreamDeckType deckType) : HandlerBase(context, settings, deckType)
    {
        public virtual ModelSwitch BaseSettings { get { return Settings; } }
        public override IModelSwitch SwitchSettings { get { return Settings; } }
        public virtual ModelSwitch Settings { get; protected set; } = settings;

        public override string ActionID { get { return $"(HandlerSwitch) ({Title.Trim()}) {(BaseSettings.IsEncoder ? "(Encoder) " : "")}(Action: {(ActionSwitchType)BaseSettings.ActionType} / {Address}) (Long: {BaseSettings.HasLongPress} / {(ActionSwitchType)BaseSettings.ActionTypeLong} / {BaseSettings.AddressActionLong})"; } }
        public override string Address { get { return BaseSettings.AddressAction; } }

        public override bool HasAction { get; protected set; } = true;

        public override bool OnButtonDown(long tick)
        {
            TickDown = tick;
            return RunButtonDown(IPCManager, BaseSettings);
        }

        public static bool RunButtonDown(IPCManager ipcManager, IModelSwitch switchSettings)
        {
            if (switchSettings.IsGuarded && ModelBase.Compare(switchSettings.GuardActiveValue, ipcManager[switchSettings.AddressGuardActive].Value))
                return ipcManager.RunActionDown(switchSettings.AddressActionGuard, (ActionSwitchType)switchSettings.ActionTypeGuard, switchSettings.SwitchOnStateGuard, switchSettings);
            else
                return ipcManager.RunActionDown(switchSettings.AddressAction, (ActionSwitchType)switchSettings.ActionType, switchSettings.SwitchOnState, switchSettings);
        }

        public override bool OnButtonUp(long tick)
        {
            bool result = RunButtonUp(IPCManager, tick - TickDown, ValueManager, BaseSettings);
            TickDown = 0;

            return result;
        }

        public static bool RunButtonUp(IPCManager ipcManager, long ticks, ValueManager valueManager, IModelSwitch switchSettings, bool ignoreBaseOptions = false)
        {
            bool result = false;
            bool longPress = ticks >= AppSettings.longPressTicks;

            if (switchSettings.IsGuarded && ModelBase.Compare(switchSettings.GuardActiveValue, ipcManager[switchSettings.AddressGuardActive].Value))
            {
                result = ipcManager.RunActionUp(switchSettings.AddressActionGuard, switchSettings.AddressActionGuardOff, (ActionSwitchType)switchSettings.ActionTypeGuard, ipcManager[switchSettings.AddressGuardActive].Value, switchSettings.SwitchOnStateGuard, switchSettings.SwitchOffStateGuard, switchSettings, ignoreBaseOptions, 1, ticks);
            }
            else if (!longPress
                || (IPCTools.IsVjoyAddress(switchSettings.AddressAction, switchSettings.ActionType) && !IPCTools.IsVjoyToggle(switchSettings.AddressAction, switchSettings.ActionType))
                || (!ignoreBaseOptions && switchSettings.HoldSwitch && (IPCTools.IsHoldableValue(switchSettings.ActionType) || IPCTools.IsHoldableCommand(switchSettings.ActionType))) )
            {
                string currentState = valueManager[ID.Switch];
                if (!ignoreBaseOptions && IPCTools.IsToggleableCommand(switchSettings.ActionType) && switchSettings.ToggleSwitch)
                    currentState = valueManager[ID.Monitor];

                result = ipcManager.RunActionUp(switchSettings.AddressAction, switchSettings.AddressActionOff, (ActionSwitchType)switchSettings.ActionType, currentState, switchSettings.SwitchOnState, switchSettings.SwitchOffState, switchSettings, ignoreBaseOptions, 1, ticks);
            }
            else if (longPress && switchSettings.HasLongPress && IPCTools.IsWriteAddress(switchSettings.AddressActionLong, (ActionSwitchType)switchSettings.ActionTypeLong))
            {
                Logger.Log(LogLevel.Debug, "HandlerSwitch:RunButtonUp", $"Long Press Action! (Address: {switchSettings.AddressActionLong})");
                result = ipcManager.RunActionUp(switchSettings.AddressActionLong, "", (ActionSwitchType)switchSettings.ActionTypeLong, valueManager[ID.SwitchLong], switchSettings.SwitchOnStateLong, switchSettings.SwitchOffStateLong, switchSettings, true);
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
                result = ipcManager.RunActionUp(address, "", (ActionSwitchType)type, value, onState, offState, switchSettings, true, ticks);
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
                if (!ipcManager.RunActionUp(switchSettings.AddressActionTouch, "", (ActionSwitchType)switchSettings.ActionTypeTouch, valueManager[ID.SwitchTouch], switchSettings.SwitchOnStateTouch, switchSettings.SwitchOffStateTouch, switchSettings, true))
                    return false;
                else
                    return true;
            }
            else if (result != 1)
                return false;
            else
                return true;
        }

        public override void Refresh()
        {
            if (SwitchSettings.IsGuarded && !string.IsNullOrWhiteSpace(SwitchSettings.ImageGuard))
            {
                if (ValueManager.HasChangedValues() || NeedRefresh)
                {
                    DefaultImage64 = GetGuardedImage64(ImgManager.GetImage(BaseSettings.DefaultImage, DeckType), SwitchSettings.GuardActiveValue, ValueManager[ID.Guard]);
                    RenderImage64 = DefaultImage64;
                    NeedRedraw = true;
                }
            }
            else if (NeedRefresh)
            {
                DefaultImage64 = ImgManager.GetImage(BaseSettings.DefaultImage, DeckType).GetImageBase64();
                RenderImage64 = DefaultImage64;
                NeedRedraw = true;
            }
        }

        public override void Update(bool skipActionUpdate = false)
        {
            base.Update(skipActionUpdate);
            RenderDefaultImages();
        }

        protected override void RenderDefaultImages()
        {
            if (HasAction && SwitchSettings.IsGuarded)
            {
                DefaultImage64 = GetGuardedImage64(ImgManager.GetImage(BaseSettings.DefaultImage, DeckType), "0", "0");
                RenderImage64 = DefaultImage64;
                NeedRedraw = true;
                NeedRefresh = true;
            }
            else if (HasAction)
            {
                DefaultImage64 = ImgManager.GetImage(BaseSettings.DefaultImage, DeckType).GetImageBase64();
                RenderImage64 = DefaultImage64;
                NeedRedraw = true;
                NeedRefresh = true;
            }
        }
    }
}
