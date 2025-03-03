using CFIT.AppTools;
using CFIT.SimConnectLib.SimVars;
using PilotsDeck.Simulator;
using PilotsDeck.Tools;
using System;

namespace PilotsDeck.Resources.Variables
{
    public class ManagedAddress : IComparable<ManagedAddress>
    {
        public const string EMPTY_PREFIX = "Z:";
        public const string EMPTY_NAME = "NULL";
        public static readonly string ADDRESS_EMPTY = $"{EMPTY_PREFIX}{EMPTY_NAME}";

        public virtual string Address { get { return BuildUniformAddress(); } }
        public virtual string Prefix { get; }
        public virtual bool HasPrefix { get { return !string.IsNullOrWhiteSpace(Prefix); } }
        public virtual string NamePrefixed { get { return $"{Prefix}{Name}"; } }
        public virtual string Name { get; }
        public virtual string Separator { get; }
        public virtual string Parameter { get; }
        public virtual bool HasParameter { get { return !string.IsNullOrWhiteSpace(Parameter); } }
        public virtual SimValueType ReadType { get; }
        public virtual SimCommandType CommandType { get; }
        public virtual bool IsCommand { get; }

        public virtual bool IsEmpty { get { return string.IsNullOrWhiteSpace(Address) || Address == ADDRESS_EMPTY; } }
        public virtual bool IsRead { get { return ReadType != SimValueType.NONE && !IsCommand; } }
        public virtual bool IsStringType { get { return EvaluateStringType(); } }

        public static ManagedAddress CreateEmpty()
        {
            return new ManagedAddress();
        }

        public static ManagedAddress CreateEmptyCommand()
        {
            return new ManagedAddress("", SimCommandType.MACRO, true);
        }

        public ManagedAddress()
        {
            Prefix = EMPTY_PREFIX;
            Name = EMPTY_NAME;
            Separator = "";
            Parameter = "";
            ReadType = SimValueType.INTERNAL;
            IsCommand = false;
        }

        public ManagedAddress(string address, SimCommandType commandType, bool donotrequest)
        {
            CommandType = commandType;
            IsCommand = true;
            if (SimCommand.IsValueCommand(commandType, donotrequest))
                ReadType = MatchCommandType(commandType);
            else
                ReadType = SimValueType.NONE;

            ParseCommandAddress(address, commandType, donotrequest, out string prefix, out string name, out string separator, out string param);
            Prefix = prefix;
            Name = name;
            Separator = separator;
            Parameter = param;
        }

        public ManagedAddress(string address)
        {
            if (ParseReadAddress(address, out string prefix, out string name, out string separator, out string param))
            {
                Prefix = prefix;
                Name = name;
                Separator = separator;
                Parameter = param;
                ReadType = TypeMatching.GetReadType(address);
            }
            else
            {
                Prefix = EMPTY_PREFIX;
                Name = EMPTY_NAME;
                Separator = "";
                Parameter = "";
                ReadType = SimValueType.INTERNAL;
            }
            IsCommand = false;
        }

        public ManagedAddress(ManagedAddress other)
        {
            Prefix = other.Prefix;
            Name = other.Name;
            Separator = other.Separator;
            Parameter = other.Parameter;
            ReadType = other.ReadType;
            CommandType = other.CommandType;
            IsCommand = other.IsCommand;
        }

        public virtual ManagedAddress Copy()
        {
            return new ManagedAddress(this);
        }

        protected virtual bool EvaluateStringType()
        {
            if (ReadType == SimValueType.OFFSET && Parameter.Contains(":s"))
                return true;
            if (ReadType == SimValueType.XPDREF && Parameter.Contains(":s"))
                return true;
            if (ReadType == SimValueType.AVAR && Parameter == SimUnitType.String)
                return true;
            if (ReadType == SimValueType.INTERNAL || ReadType == SimValueType.CALCULATOR)
                return true;

            return false;
        }

        public static bool ParseReadAddress(string address, out string prefix, out string name, out string separator, out string param)
        {
            prefix = EMPTY_PREFIX;
            name = EMPTY_NAME;
            separator = "";
            param = "";

            if (string.IsNullOrWhiteSpace(address))
                return false;

            if (TypeMatching.rxOffset.IsMatch(address))
            {
                prefix = "0x";

                string[] parts = address.Split(':');
                int idx = 0;
                if (parts[0].StartsWith("0x"))
                    idx = 2;
                name = parts[0].Substring(idx, 4).ToUpper();

                if (parts.Length > 1)
                {
                    separator = ":";
                    param = string.Join(':', parts[1..]).ToLowerInvariant();
                }
            }
            else if (TypeMatching.rxAvar.IsMatch(address) && TypeMatching.rxAvar.GroupsMatching(address, [3,5], out var rpnGroups))
            {
                if (TypeMatching.rxAvar.GroupsMatching(address, [1], out var prefixGroup))
                    prefix = prefixGroup[0];
                else
                    prefix = "A:";

                name = rpnGroups[0];

                separator = ",";
                param = rpnGroups[1].ToLowerInvariant();
            }
            else if (TypeMatching.rxKvarVariable.IsMatch(address))
            {
                prefix = "K:";

                name = address.Split(':')[1];                
            }
            else if (TypeMatching.rxBvarValue.IsMatch(address))
            {
                prefix = "B:";

                name = address[2..];
            }
            else if (TypeMatching.rxLuaFunc.IsMatch(address))
            {
                prefix = "lua:";

                string[] parts = address.Split(':');
                name = parts[1].ToLowerInvariant().Replace(".lua", "");

                separator = ":";

                param = parts[2];
            }
            else if (TypeMatching.rxInternal.IsMatch(address))
            {
                prefix = "X:";

                name = address[2..];
            }
            else if (TypeMatching.rxCalcRead.IsMatch(address))
            {
                prefix = "C:";

                name = address[2..];
            }
            else if (TypeMatching.rxDref.IsMatch(address) && TypeMatching.rxDref.GroupsMatching(address, [1], out var drefNameGroup))
            {
                prefix = "";

                name = drefNameGroup[0];

                if (TypeMatching.rxDref.GroupsMatching(address, [4], out var drefParamGroup))
                    param = drefParamGroup[0].ToLowerInvariant();
            }
            else if (TypeMatching.rxLvar.IsMatch(address) && TypeMatching.rxLvar.GroupsMatching(address, [1], out var lvarGroups))
            {
                prefix = "L:";

                name = lvarGroups[0];
                if (name.StartsWith("L:"))
                    name = name[2..];
            }

            return $"{prefix}{name}" != ADDRESS_EMPTY;
        }

        public static SimValueType MatchCommandType(SimCommandType commandType)
        {
            switch (commandType)
            {
                case SimCommandType.LVAR:
                    return SimValueType.LVAR;
                case SimCommandType.OFFSET:
                    return SimValueType.OFFSET;
                case SimCommandType.XPWREF:
                    return SimValueType.XPDREF;
                case SimCommandType.AVAR:
                    return SimValueType.AVAR;
                case SimCommandType.BVAR:
                    return SimValueType.BVAR;
                case SimCommandType.INTERNAL:
                    return SimValueType.INTERNAL;
                default:
                    return SimValueType.NONE;
            }
        }

        public static bool ParseCommandAddress(string address, SimCommandType commandType, bool donotrequest, out string prefix, out string name, out string separator,  out string param)
        {
            prefix = "";
            name = "";
            separator = "";
            param = "";

            if (string.IsNullOrWhiteSpace(address) || !SimCommand.IsValidAddressForType(address, commandType, donotrequest))
                return false;

            if (commandType == SimCommandType.MACRO)
            {
                string[] parts = address.Split(':');
                if (parts.Length >= 2)
                {
                    prefix = $"{parts[0]}:";

                    name = parts[1];

                    if (parts.Length > 2)
                    {
                        separator = ":";
                        param = string.Join(':', parts[2..]);
                    }
                }
            }
            else if (commandType == SimCommandType.SCRIPT)
            {
                string[] parts = address.Split(':');
                if (parts.Length >= 2)
                {
                    prefix = $"{parts[0]}:";

                    name = parts[1];

                    if (parts.Length > 2)
                    {
                        separator = ":";
                        param = string.Join(':', parts[2..]);
                    }
                }
            }
            else if (commandType == SimCommandType.CONTROL)
            {
                if (address.Contains('='))
                {
                    string[] parts = address.Split('=');
                    name = parts[0];

                    if (parts.Length > 1)
                    {
                        separator = "=";
                        param = string.Join('=', parts[1..]);
                    }
                }
                else if (address.Contains(':'))
                {
                    string[] parts = address.Split(':');
                    name = parts[0];

                    if (parts.Length > 1)
                    {
                        separator = ":";
                        param = string.Join(':', parts[1..]);
                    }
                }
                else
                    name = address;
            }
            else if (commandType == SimCommandType.LVAR && TypeMatching.rxLvar.GroupsMatching(address, [1], out var lvarGroups))
            {
                prefix = "L:";

                name = lvarGroups[0];
                if (name.StartsWith("L:"))
                    name = name[2..];
            }
            else if (commandType == SimCommandType.OFFSET)
            {
                prefix = "0x";

                string[] parts = address.Split(':');
                int idx = 0;
                if (parts[0].StartsWith("0x"))
                    idx = 2;
                name = parts[0].Substring(idx, 4).ToUpper();

                if (parts.Length > 1)
                {
                    separator = ":";
                    param = string.Join(':', parts[1..]).ToLowerInvariant();
                }
            }
            else if (commandType == SimCommandType.VJOY || commandType == SimCommandType.VJOYDRV)
            {
                string[] parts = address.ToLowerInvariant().Replace("vjoy:","").Split(':');

                prefix = "vjoy:";

                name = parts[0] + ":" + parts[1];

                if (parts.Length > 2)
                {
                    separator = ":";
                    param = string.Join(':', parts[2..]).ToLowerInvariant();
                }
            }
            else if (commandType == SimCommandType.HVAR)
            {
                prefix = "H:";
                
                if (address.StartsWith("H:"))
                    address = address[2..];
                string[] parts = address.Split(':');
                name = parts[0];

                if (parts.Length > 1)
                {
                    separator = ":";
                    param = string.Join(':', parts[1..]);
                }
            }
            else if (commandType == SimCommandType.CALCULATOR)
            {
                name = address;
            }
            else if (commandType == SimCommandType.XPCMD)
            {
                string[] parts = address.Split(':');
                name = parts[0];

                if (parts.Length > 1)
                {
                    separator = ":";
                    param = string.Join(':', parts[1..]);
                }
            }
            else if (commandType == SimCommandType.XPWREF && TypeMatching.rxDref.GroupsMatching(address, [1], out var drefNameGroup))
            {
                name = drefNameGroup[0];

                if (TypeMatching.rxDref.GroupsMatching(address, [4], out var drefParamGroup))
                    param = drefParamGroup[0].ToLowerInvariant();
            }
            else if (commandType == SimCommandType.AVAR && TypeMatching.rxAvar.GroupsMatching(address, [3, 5], out var rpnGroups))
            {
                if (TypeMatching.rxAvar.GroupsMatching(address, [1], out var prefixGroup))
                    prefix = prefixGroup[0];
                else
                    prefix = "A:";

                name = rpnGroups[0];

                separator = ",";
                param = rpnGroups[1].ToLowerInvariant();
            }
            else if (commandType == SimCommandType.BVAR)
            {
                prefix = "B:";

                if (address.StartsWith("B:"))
                    address = address[2..];
                string[] parts = address.Split(':');
                name = parts[0];

                if (parts.Length > 1)
                {
                    separator = ":";
                    param = string.Join(':', parts[1..]);
                }
            }
            else if (commandType == SimCommandType.LUAFUNC)
            {
                prefix = "lua:";

                string[] parts = address.Split(':');
                name = parts[1].ToLowerInvariant().Replace(".lua", "");

                separator = ":";

                param = parts[2];
            }
            else if (commandType == SimCommandType.INTERNAL)
            {
                prefix = "X:";

                name = address[2..];
            }
            else if (commandType == SimCommandType.KVAR)
            {
                prefix = "K:";

                if (address.StartsWith("K:"))
                    address = address[2..];

                string[] parts = address.Split(':');
                name = parts[0];

                if (parts.Length > 1)
                {
                    separator = ":";
                    param = string.Join(':', parts[1..]);
                }
            }

            return !string.IsNullOrWhiteSpace(name);
        }

        public virtual string BuildUniformAddress()
        {
            string addr = $"{Prefix}{Name}{Separator}{Parameter}";
            if (ReadType == SimValueType.AVAR)
                return $"({addr})";
            else
                return addr;
        }

        public virtual string FormatFsuipcLvar()
        {
            if (IsEmpty)
                return "";

            return $"L:{Name}";
        }

        public virtual string FormatLuaFile()
        {
            if (IsEmpty)
                return "";

            return $"{Name}.lua";
        }

        public override string ToString()
        {
            return BuildUniformAddress();
        }

        public static implicit operator string(ManagedAddress address)
        {
            return address.ToString();
        }

        public static bool operator ==(ManagedAddress left, ManagedAddress right)
        {
            return left?.Equals(right) == true;
        }

        public static bool operator !=(ManagedAddress left, ManagedAddress right)
        {
            return left?.Equals(right) == false;
        }

        public static bool operator ==(ManagedAddress left, string right)
        {
            return left?.ToString() == right;
        }

        public static bool operator !=(ManagedAddress left, string right)
        {
            return left?.ToString() != right;
        }

        public static bool operator ==(ManagedAddress left, SimValueType right)
        {
            return left?.ReadType == right;
        }

        public static bool operator !=(ManagedAddress left, SimValueType right)
        {
            return left?.ReadType != right;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if (obj is ManagedAddress managedAddress && managedAddress.BuildUniformAddress() == this.BuildUniformAddress())
                return true;
            if (obj is string stringAddress && stringAddress == this.BuildUniformAddress())
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return Prefix?.GetHashCode() ?? 0 ^ Name?.GetHashCode() ?? 0 ^ Parameter?.GetHashCode() ?? 0;
        }

        public int CompareTo(ManagedAddress? other)
        {
            if (other == null)
                return 1;

            if (Prefix == other.Prefix)
            {
                if (Name != other.Name)
                    return Name.CompareTo(other.Name);
                else
                    return Parameter.CompareTo(other.Parameter);
            }
            else
                return Prefix.CompareTo(other.Prefix);
        }
    }
}
