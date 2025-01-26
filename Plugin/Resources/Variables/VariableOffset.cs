using FSUIPC;
using PilotsDeck.Tools;
using System;
using System.Globalization;
using System.Linq;

namespace PilotsDeck.Resources.Variables
{
    public class VariableOffset : ManagedVariable
    {
        protected Offset IpcOffset { get; set; } = null;
        public OffsetParam OffsetParam { get; protected set; } = null;
        public int Size { get { return OffsetParam.Length; } }
        public bool IsSigned { get { return OffsetParam.Signed; } }
        public bool IsString { get { return OffsetParam.Type == OffsetType.STRING; } }
        public bool IsFloat { get { return OffsetParam.Type == OffsetType.FLOAT; } }
        public bool IsBit { get { return OffsetParam.Type == OffsetType.BIT; } }
        public bool IsByteArray { get { return OffsetParam.Type == OffsetType.BYTEARRAY; } }

        public override string Value { get { return Read(); } }
        protected virtual string ValueLast { get; set; } = "";

        public override double NumericValue { get { return (double)ReadNumber(); } }

        public override bool IsConnected { get { return IpcOffset?.IsConnected == true; } }

        public VariableOffset(string address, string group = null, OffsetAction action = OffsetAction.Read) : base(address, SimValueType.OFFSET)
        {
            if (string.IsNullOrWhiteSpace(group))
                group = AppConfiguration.IpcGroupRead;

            OffsetParam = new OffsetParam(address);
            IpcOffset = new Offset(group, OffsetParam.Address, OffsetParam.Length)
            {
                ActionAtNextProcess = action
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                    IpcOffset = null;
                }
                _disposed = true;
            }
        }

        public override void CheckChanged()
        {
            IsChanged = Value != ValueLast;
            ValueLast = Value;
        }

        public override void Connect()
        {
            Logger.Verbose($"Connecting '{Address}'");
            if (IpcOffset != null && !IpcOffset.IsConnected)
                IpcOffset.Reconnect();

            IsSubscribed = true;
        }

        public override void Disconnect()
        {
            Logger.Verbose($"Disconnecting '{Address}'");
            if (IpcOffset != null && IpcOffset.IsConnected)
                IpcOffset.Disconnect();

            IsSubscribed = false;
        }

        public virtual void Write(string newValue, string group)
        {
            if (IsBit && int.TryParse(newValue, out int result))
            {
                IpcOffset.ActionAtNextProcess = OffsetAction.Read;
                FSUIPCConnection.Process(group);
                FsBitArray bitArray = IpcOffset.GetValue<FsBitArray>();
                bitArray[OffsetParam.BitNum] = Convert.ToBoolean(result);
                IpcOffset.SetValue(bitArray);
                IpcOffset.ActionAtNextProcess = OffsetAction.Write;
                FSUIPCConnection.Process(group);
            }
            else
            {
                IpcOffset.ActionAtNextProcess = OffsetAction.Write;
                IpcOffset.SetValue(CastValue(newValue));
                FSUIPCConnection.Process(group);
                IpcOffset.ActionAtNextProcess = OffsetAction.Read;
            }
        }

        protected string Read()
        {
            if (IsString)
                return ReadOffsetString();
            if (IsByteArray)
                return $"[{string.Join(",", ReadByteArray().Select(b => Convert.ToString(b)))}]";
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
                    return "0";
            }
        }

        public override dynamic RawValue()
        {
            if (IsString)
                return ReadOffsetString();
            if (IsByteArray)
                return ReadByteArray();
            else //should be float, int or bit
                return ReadNumber();
        }

        protected byte[] ReadByteArray()
        {
            try
            {
                return IpcOffset.GetValue<byte[]>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return Array.Empty<byte>();
            }
        }

        protected string ReadOffsetString()
        {
            try
            {
                return IpcOffset.GetValue<string>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return "0";
            }
        }

        protected dynamic ReadNumber()
        {
            if (IsFloat)
                return ReadOffsetFloat();
            else if (IsBit)
                return Convert.ToInt32(IpcOffset.GetValue<FsBitArray>()[OffsetParam.BitNum]);
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
                            return IpcOffset.GetValue<sbyte>();
                        else
                            return IpcOffset.GetValue<byte>();
                    case 2:
                        if (IsSigned)
                            return IpcOffset.GetValue<short>();
                        else
                            return IpcOffset.GetValue<ushort>();
                    case 4:
                        if (IsSigned)
                            return IpcOffset.GetValue<int>();
                        else
                            return IpcOffset.GetValue<uint>();
                    case 8:
                        if (IsSigned)
                            return IpcOffset.GetValue<long>();
                        else
                            return IpcOffset.GetValue<ulong>();
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
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
                        return IpcOffset.GetValue<float>();
                    case 8:
                        return IpcOffset.GetValue<double>();
                    default:
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }

        public override void SetValue(string newValue)
        {
            if (IsBit && int.TryParse(newValue, out int result))
            {
                IpcOffset.ActionAtNextProcess = OffsetAction.Read;
                FSUIPCConnection.Process(AppConfiguration.IpcGroupWrite);
                FsBitArray bitArray = IpcOffset.GetValue<FsBitArray>();
                bitArray[OffsetParam.BitNum] = Convert.ToBoolean(result);
                IpcOffset.SetValue(bitArray);
                IpcOffset.ActionAtNextProcess = OffsetAction.Write;
                FSUIPCConnection.Process(AppConfiguration.IpcGroupWrite);
            }
            else
            {
                IpcOffset.ActionAtNextProcess = OffsetAction.Write;
                IpcOffset.SetValue(CastValue(newValue));
                FSUIPCConnection.Process(AppConfiguration.IpcGroupWrite);
                IpcOffset.ActionAtNextProcess = OffsetAction.Read;
            }
        }

        public override void SetValue(double newValue)
        {
            if (IsBit)
            {
                IpcOffset.ActionAtNextProcess = OffsetAction.Read;
                FSUIPCConnection.Process(AppConfiguration.IpcGroupWrite);
                FsBitArray bitArray = IpcOffset.GetValue<FsBitArray>();
                bitArray[OffsetParam.BitNum] = Convert.ToBoolean(newValue);
                IpcOffset.SetValue(bitArray);
                IpcOffset.ActionAtNextProcess = OffsetAction.Write;
                FSUIPCConnection.Process(AppConfiguration.IpcGroupWrite);
            }
            else
            {
                IpcOffset.ActionAtNextProcess = OffsetAction.Write;
                IpcOffset.SetValue(CastValue(newValue));
                FSUIPCConnection.Process(AppConfiguration.IpcGroupWrite);
                IpcOffset.ActionAtNextProcess = OffsetAction.Read;
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

        protected dynamic CastValue(double newValue)
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
                            return newValue;
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
