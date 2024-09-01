using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using System;

namespace PilotsDeck.Resources.Variables
{
    public enum SimValueType
    {
        NONE = 0,
        LVAR = 3,
        OFFSET = 4,
        CALCULATOR = 9,
        XPDREF = 11,
        AVAR = 12,
        BVAR = 13,
        LUAFUNC = 14,
        INTERNAL = 15
    }

    public abstract class ManagedVariable(string address, SimValueType type) : IManagedRessource
    {
        public virtual bool IsValueXP()
        {
            return IsValueXP(Type);
        }

        public static bool IsValueXP(ManagedVariable managedVariable)
        {
            return IsValueXP(managedVariable?.Type);
        }

        public static bool IsValueXP(SimValueType? type)
        {
            return type == SimValueType.XPDREF;
        }

        public virtual bool IsValueMSFS()
        {
            return IsValueMSFS(Type);
        }

        public static bool IsValueMSFS(ManagedVariable managedVariable)
        {
            return IsValueMSFS(managedVariable?.Type);
        }

        public static bool IsValueMSFS(SimValueType? type)
        {
            return type == SimValueType.CALCULATOR || type == SimValueType.LVAR || type == SimValueType.AVAR || type == SimValueType.BVAR;
        }

        public virtual bool IsValueFSUIPC()
        {
            return IsValueFSUIPC(Type);
        }

        public static bool IsValueFSUIPC(ManagedVariable managedVariable)
        {
            return IsValueFSUIPC(managedVariable?.Type);
        }

        public static bool IsValueFSUIPC(SimValueType? type)
        {
            return (type == SimValueType.LVAR && App.SimController.SimMainType != SimulatorType.MSFS) || type == SimValueType.OFFSET;
        }

        public virtual bool IsValueInternal()
        {
            return IsValueInternal(Type) || Address == VariableManager.ADDRESS_EMPTY;
        }

        public static bool IsValueInternal(ManagedVariable managedVariable)
        {
            return IsValueInternal(managedVariable?.Type) || managedVariable?.Address == VariableManager.ADDRESS_EMPTY;
        }

        public static bool IsValueInternal(SimValueType? type)
        {
            return type == SimValueType.NONE || type == SimValueType.INTERNAL || type == SimValueType.LUAFUNC;
        }

        public virtual string UUID { get { return Address; } }
        public virtual int Registrations { get; set; } = 1;
        public virtual string Address { get; protected set; } = address;
        public virtual SimValueType Type { get; set; } = type;
        public virtual bool IsNumericValue { get { return Conversion.IsNumber(Value); } }
        public abstract string Value { get; }
        public abstract double NumericValue { get; }
        public virtual bool IsChanged { get; set; } = false;
        public virtual bool IsSubscribed { get; set; } = false;
        public virtual bool IsConnected { get { return true; } }

        public abstract void CheckChanged();

        public virtual void Connect()
        {

        }

        public virtual void Disconnect()
        {

        }

        public abstract dynamic RawValue();

        public abstract void SetValue(string value);

        public abstract void SetValue(double value);

        public override string ToString()
        {
            return Address ?? "";
        }


        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
