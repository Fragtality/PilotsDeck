using System;
using Serilog;
using FSUIPC;

namespace PilotsDeck
{
    public enum OffsetType
    {
        INTEGER,
        FLOAT,
        STRING
    }

    public class OffsetParam
    {
        public int Address { get; }
        public int Length { get; }
        public OffsetType Type { get; }
        public bool Signed { get; }

        public OffsetParam(int _address, int _length, OffsetType _type = OffsetType.INTEGER, bool _signed = false)
        {
            Address = _address;
            Length = _length;
            Type = _type;
            Signed = _signed;
        }

        public OffsetParam(string paramString)
        {
            string[] parameters = paramString.Split(':');

            Address = Convert.ToInt32(parameters[0], 16);
            Length = Convert.ToInt32(parameters[1]);
            if (parameters.Length > 2)
            {
                switch (parameters[2].ToLower())
                {
                    case "s":
                        Type = OffsetType.STRING;
                        break;
                    case "f":
                        Type = OffsetType.FLOAT;
                        break;
                    default:
                        Type = OffsetType.INTEGER;
                        break;
                }

                if (parameters.Length > 3 && parameters[3].ToLower() == "s")
                    Signed = true;
                else
                    Signed = false;
            }
            else
            {
                Type = OffsetType.INTEGER;
                Signed = false;
            }
        }
    }
    public class IPCValueOffset : IPCValue, IDisposable
    {
        private Offset offset = null;
        public OffsetParam Param { get; } = null;
        public int Size { get { return Param.Length;  } }
        public bool IsSigned { get { return Param.Signed; } }
        public bool IsString { get { return Param.Type == OffsetType.STRING; } }
        public bool IsFloat { get { return Param.Type == OffsetType.FLOAT; } }

        public IPCValueOffset(string address, string group, OffsetAction action = OffsetAction.Read) : base(address)
        {
            Param = new OffsetParam(address);
            offset = new Offset(group, Param.Address, Param.Length)
            {
                ActionAtNextProcess = action
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            if (offset != null)
            {
                if (offset.IsConnected)
                    offset.Disconnect();
                offset = null;
            }
        }

        public override bool IsChanged
        {
            get
            {
                if (offset == null)
                    return false;
                else
                    return offset.ValueChanged;
            }
        }

        protected override string Read()
        {
            if (IsString)
                return ReadOffsetString();
            else //should be float or int
            {
                var result = ReadNumber();
                if (result != null)
                    return Convert.ToString(result);
                else
                    return null;
            }
        }

        public override dynamic RawValue()
        {
            if (IsString)
                return ReadOffsetString();
            else //should be float or int
                return ReadNumber();
        }

        public override string ScaledValue(double scalar)
        {
            if (!IsString)
            {
                var result = ReadNumber();
                if (result != null)
                    return Convert.ToString(result * scalar);
                else
                    return null;
            }
            else
            {
                var str = ReadOffsetString();
                if (double.TryParse(str, out double num))
                    return Convert.ToString(num * scalar);
                else
                    return str;
            }
        }

        protected string ReadOffsetString()
        {
            try
            {
                return offset.GetValue<string>();
            }
            catch
            {
                Log.Logger.Error($"Exception while Reading String Offset {Address}, Size: {offset?.DataLength}");
                return null;
            }
        }

        protected dynamic ReadNumber()
        {
            if (IsFloat)
                return ReadOffsetFloat();
            else //integer (should be checked before)
                return ReadOffsetInt();
        }

        protected dynamic ReadOffsetInt()
        {
            try
            {
                switch (Size)
                {
                    case 1:
                        if (IsSigned)
                            return offset.GetValue<sbyte>();
                        else
                            return offset.GetValue<byte>();
                    case 2:
                        if (IsSigned)
                            return offset.GetValue<short>();
                        else
                            return offset.GetValue<ushort>();
                    case 4:
                        if (IsSigned)
                            return offset.GetValue<int>();
                        else
                            return offset.GetValue<uint>();
                    case 8:
                        if (IsSigned)
                            return offset.GetValue<long>();
                        else
                            return offset.GetValue<ulong>();
                    default:
                        return null;
                }
            }
            catch
            {
                Log.Logger.Error($"Exception while Reading Integer Offset {Address}, Size: {offset?.DataLength}, Signed: {IsSigned}");
                return null;
            }
        }

        protected dynamic ReadOffsetFloat()
        {
            try
            {
                switch (Size)
                {
                    case 4:
                        return offset.GetValue<float>();
                    case 8:
                        return offset.GetValue<double>();
                    default:
                        return null;
                }
            }
            catch
            {
                Log.Logger.Error($"Exception while Reading Float Offset {Address}, Size: {offset?.DataLength}");
                return null;
            }
        }

        public void Write(string newValue, string group)
        {
            offset.SetValue(CastValue(newValue));
            FSUIPCConnection.Process(group);
        }

        protected dynamic CastValue(string newValue)
        {
            if (!IsString)
            {
                if (IsFloat)
                {
                    switch (Size)
                    {
                        case 4:
                            return Convert.ToSingle(newValue);
                        case 8:
                            return Convert.ToDouble(newValue);
                        default:
                            return newValue;
                    }
                }
                else
                {
                    switch (Size)
                    {
                        case 1:
                            if (IsSigned)
                                return Convert.ToSByte(newValue);
                            else
                                return Convert.ToByte(newValue);
                        case 2:
                            if (IsSigned)
                                return Convert.ToInt16(newValue);
                            else
                                return Convert.ToUInt16(newValue);
                        case 4:
                            if (IsSigned)
                                return Convert.ToInt32(newValue);
                            else
                                return Convert.ToUInt32(newValue);
                        case 8:
                            if (IsSigned)
                                return Convert.ToInt64(newValue);
                            else
                                return Convert.ToUInt64(newValue);
                        default:
                            return newValue;
                    }
                }
            }
            else
            {
                return newValue;
            }
        }
    }
}
