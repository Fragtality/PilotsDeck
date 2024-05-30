using FSUIPC;
using System;
using System.Globalization;

namespace PilotsDeck
{
    public enum OffsetType
    {
        INTEGER,
        FLOAT,
        STRING,
        BIT
    }

    public class OffsetParam
    {
        public int Address { get; }
        public int Length { get; }
        public OffsetType Type { get; }
        public bool Signed { get; }
        public int BitNum { get; }

        public OffsetParam(int _address, int _length, OffsetType _type = OffsetType.INTEGER, bool _signed = false, int _bit = 0)
        {
            Address = _address;
            Length = _length;
            Type = _type;
            Signed = _signed;
            BitNum = _bit;
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
                    case "b":
                        Type = OffsetType.BIT;
                        BitNum = 0;
                        break;
                    default:
                        Type = OffsetType.INTEGER;
                        Signed = false;
                        break;
                }

                if (parameters.Length > 3)
                {
                    if (Type == OffsetType.INTEGER && parameters[3].ToLower() == "s")
                        Signed = true;

                    if (Type == OffsetType.BIT && int.TryParse(parameters[3], out int bitnum))
                        BitNum = bitnum;
                }

            }
            else
            {
                Type = OffsetType.INTEGER;
                Signed = false;
            }
        }
    }
    public class IPCValueOffset : IPCValue
    {
        private Offset offset = null;
        private bool disposed = false;

        public OffsetParam Param { get; } = null;
        public int Size { get { return Param.Length;  } }
        public bool IsSigned { get { return Param.Signed; } }
        public bool IsString { get { return Param.Type == OffsetType.STRING; } }
        public bool IsFloat { get { return Param.Type == OffsetType.FLOAT; } }
        public bool IsBit { get { return Param.Type == OffsetType.BIT; } }

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (offset != null && disposing)
            {
                if (offset.IsConnected)
                    offset.Disconnect();
                offset = null;
            }
            disposed = true;
        }

        public override void Connect()
        {
            if (!disposed && !offset.IsConnected)
                offset.Reconnect();
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
            else //should be float, int or bit
            {
                var result = ReadNumber();
                if (result != null)
                {
                    string num = Convert.ToString(result, CultureInfo.InvariantCulture.NumberFormat);

                    int idxE = num.IndexOf('E');
                    if (idxE < 0)
                        return num;
                    else
                        return string.Format("{0:F1}", result);
                }
                else
                    return null;
            }
        }

        public override dynamic RawValue()
        {
            if (IsString)
                return ReadOffsetString();
            else //should be float, int or bit
                return ReadNumber();
        }

        protected string ReadOffsetString()
        {
            try
            {
                return offset.GetValue<string>();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCValueOffset:ReadOffsetString", $"Exception while Reading Offset! (Address: {Address}) (Size: {offset?.DataLength}) (Exception: {ex.GetType()})");
                return null;
            }
        }

        protected dynamic ReadNumber()
        {
            if (IsFloat)
                return ReadOffsetFloat();
            else if (IsBit)
                return Convert.ToInt32(offset.GetValue<FsBitArray>()[Param.BitNum]);
            else //integer (string should be checked before)
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCValueOffset:ReadOffsetInt", $"Exception while Reading Offset! (Address: {Address}) (Size: {offset?.DataLength}) (Signed: {IsSigned}) (Exception: {ex.GetType()})");
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
            catch(Exception ex)
            {
                Logger.Log(LogLevel.Critical, "IPCValueOffset:ReadOffsetFloat", $"Exception while Reading Offset! (Address: {Address}) (Size: {offset?.DataLength}) (Exception: {ex.GetType()})");
                return null;
            }
        }

        public void Write(string newValue, string group)
        {
            if (IsBit && int.TryParse(newValue, out int result))
            {
                offset.ActionAtNextProcess = OffsetAction.Read;
                FSUIPCConnection.Process(group);
                FsBitArray bitArray = offset.GetValue<FsBitArray>();
                bitArray[Param.BitNum] = Convert.ToBoolean(result);
                offset.SetValue(bitArray);
                offset.ActionAtNextProcess = OffsetAction.Write;
                FSUIPCConnection.Process(group);
            }
            else
            {
                offset.ActionAtNextProcess = OffsetAction.Write;
                offset.SetValue(CastValue(newValue));
                FSUIPCConnection.Process(group);
                offset.ActionAtNextProcess = OffsetAction.Read;
            }
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
                            return Convert.ToSingle(newValue, new RealInvariantFormat(newValue));
                        case 8:
                            return Convert.ToDouble(newValue, new RealInvariantFormat(newValue));
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
