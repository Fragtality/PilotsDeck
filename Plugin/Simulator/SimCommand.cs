using CFIT.AppTools;
using PilotsDeck.Resources.Variables;
using PilotsDeck.Tools;
using System;

namespace PilotsDeck.Simulator
{
    public enum SimCommandType
    {
        MACRO = 0,
        SCRIPT = 1,
        CONTROL = 2,
        LVAR = 3,
        OFFSET = 4,
        VJOY = 6, //FSUIPC vJoy
        VJOYDRV = 7, //vJoy Driver
        HVAR = 8,
        CALCULATOR = 9,
        XPCMD = 10,
        XPWREF = 11,
        AVAR = 12,
        BVAR = 13,
        LUAFUNC = 14,
        INTERNAL = 15,
        KVAR = 16
    }

    public class SimCommand
    {
        public virtual ManagedAddress Address { get; set; } = ManagedAddress.CreateEmptyCommand();
        public virtual SimCommandType Type { get; set; } = SimCommandType.MACRO;
        public virtual bool EncoderAction { get; set; } = false;
        public virtual double NumValue { get { return Conversion.ToDouble(Value); } }
        public virtual string Value { get; set; } = "";
        public virtual bool DoNotRequest { get; set; } = false;
        public virtual bool IsValueReset { get; set; } = false;
        public virtual int ResetDelay { get; set; } = 0;
        public virtual double ResetNumValue { get { return Conversion.ToDouble(ResetValue); } }
        public virtual string ResetValue { get; set; } = "";
        public virtual string Context { get; set; } = "";
        public virtual int Ticks { get; set; } = 1;
        public virtual int TickDelay { get; set; } = App.Configuration.TickDelay;
        public virtual bool IsUp { get; set; } = true;
        public virtual bool IsDown { get { return !IsUp; } }
        public virtual DateTime Time { get; set; } = DateTime.Now;
        public virtual int CommandDelay { get; set; } = 0;

        public virtual bool IsValid
        {
            get
            {
                return IsValidAddressForType(Address.Address, Type, DoNotRequest);
            }
        }

        public override string ToString()
        {
            return $"Type: {Type} | Address: {Address} | Value: {Value} | Ticks: {Ticks}";
        }

        public static bool IsValidValueCommand(string address, bool donotrequest, SimCommandType? type)
        {
            return IsValueCommand(type, donotrequest) && IsValidAddressForType(address, type, donotrequest);
        }

        public static bool IsValueCommand(SimCommandType? type, bool donotrequest)
        {
            return type == SimCommandType.LVAR || type == SimCommandType.OFFSET || type == SimCommandType.XPWREF ||
                   type == SimCommandType.AVAR || (type == SimCommandType.BVAR && !donotrequest) || type == SimCommandType.INTERNAL;
        }

        public static bool IsValidNonvalueCommand(string address, SimCommandType? type, bool donotrequest)
        {
            return IsNonvalueCommand(type, donotrequest) && IsValidAddressForType(address, type, donotrequest);
        }

        public static bool IsNonvalueCommand(SimCommandType? type, bool donotrequest)
        {
            return type == SimCommandType.MACRO || type == SimCommandType.SCRIPT || type == SimCommandType.CONTROL || type == SimCommandType.VJOY || type == SimCommandType.VJOYDRV || type == SimCommandType.HVAR
                || type == SimCommandType.CALCULATOR || type == SimCommandType.XPCMD || type == SimCommandType.LUAFUNC || type == SimCommandType.KVAR
                || (type == SimCommandType.BVAR && donotrequest);
        }

        public static bool IsToggleable(SimCommandType? type, bool donotrequest)
        {
            return IsNonvalueCommand(type, donotrequest);
        }

        public static bool IsHoldable(SimCommandType? type, bool donotrequest)
        {
            return IsHoldableCommand(type, donotrequest) || IsHoldableValue(type, donotrequest);
        }

        public static bool IsHoldableCommand(SimCommandType? type, bool donotrequest)
        {
            return IsNonvalueCommand(type, donotrequest);
        }

        public static bool IsHoldableValue(SimCommandType? type, bool donotrequest)
        {
            return IsValueCommand(type, donotrequest);
        }
        public static bool IsResetableValue(SimCommandType? type, bool donotrequest)
        {
            return IsValueCommand(type, donotrequest);
        }

        public static bool CommandTypeUsesDelay(SimCommandType type, bool donotrequest)
        {
            return type == SimCommandType.MACRO || type == SimCommandType.SCRIPT || type == SimCommandType.CONTROL || type == SimCommandType.HVAR || type == SimCommandType.XPCMD
                || (type == SimCommandType.BVAR && donotrequest) || type == SimCommandType.KVAR;
        }

        public static bool IsVjoyClearSet(ManagedAddress address)
        {
            return (address.CommandType == SimCommandType.VJOYDRV || address.CommandType == SimCommandType.VJOY) && !address.HasParameter;
        }

        //public static bool IsVjoyToggle(string address, SimCommandType? type)
        //{
        //    return (type == SimCommandType.VJOYDRV || type == SimCommandType.VJOY) && address?.Contains(":t", StringComparison.InvariantCultureIgnoreCase) == true;
        //}

        public static bool IsValidBvarAddress(string address, bool donotrequest)
        {
            return (!donotrequest && TypeMatching.rxBvarValue.IsMatch(address)) || (donotrequest && TypeMatching.rxBvarCmd.IsMatch(address));
        }

        public static bool IsValidAddressForType(string address, SimCommandType? type, bool donotrequest)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            return type switch
            {
                SimCommandType.LUAFUNC => TypeMatching.rxLuaFunc.IsMatch(address),
                SimCommandType.MACRO => TypeMatching.rxMacro.IsMatch(address),
                SimCommandType.SCRIPT => TypeMatching.rxScript.IsMatch(address),
                SimCommandType.CONTROL => TypeMatching.rxControl.IsMatch(address) || TypeMatching.rxControlSeq.IsMatch(address),
                SimCommandType.HVAR => TypeMatching.rxHvar.IsMatch(address),
                SimCommandType.OFFSET => TypeMatching.rxOffset.IsMatch(address),
                SimCommandType.VJOY => TypeMatching.rxVjoy.IsMatch(address),
                SimCommandType.VJOYDRV => TypeMatching.rxVjoyDrv.IsMatch(address),
                SimCommandType.CALCULATOR => !string.IsNullOrWhiteSpace(address) && address.Length > 1,
                SimCommandType.XPCMD => TypeMatching.rxCmdXP.IsMatch(address),
                SimCommandType.XPWREF => TypeMatching.rxDref.IsMatch(address),
                SimCommandType.AVAR => TypeMatching.rxAvar.IsMatch(address),
                SimCommandType.BVAR => IsValidBvarAddress(address, donotrequest),
                SimCommandType.KVAR => TypeMatching.rxKvarCmd.IsMatch(address),
                SimCommandType.LVAR => TypeMatching.rxLvar.IsMatch(address),
                SimCommandType.INTERNAL => TypeMatching.rxInternal.IsMatch(address),
                _ => false,
            };
        }
    }

    public class DelayCommand : SimCommand
    {
        public override bool IsValid { get { return true; } }

        public DelayCommand(int delay)
        {
            CommandDelay = delay;
        }
    }
}
