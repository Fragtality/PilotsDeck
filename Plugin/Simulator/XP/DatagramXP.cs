using System;
using System.Collections.Generic;
using System.Text;

namespace PilotsDeck.Simulator.XP
{
    public class DatagramXP
    {
        public static readonly int XP_MAX_NUM_RREF = 100;
        public static readonly string XP_STR_RREF = "RREF";
        public static readonly string XP_STR_DREF = "DREF";
        public static readonly string XP_STR_CMND = "CMND";

        public List<byte> Bytes { get; set; }

        public DatagramXP()
        {
            Bytes = [];
        }

        public byte[] Get()
        {
            return [.. Bytes];
        }

        protected void Add(byte value)
        {
            Bytes.Add(value);
        }

        protected void Add(int value)
        {
            var bElement = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Bytes.Add(bElement[0]);
                Bytes.Add(bElement[1]);
                Bytes.Add(bElement[2]);
                Bytes.Add(bElement[3]);
            }
            else
            {
                Bytes.Add(bElement[3]);
                Bytes.Add(bElement[2]);
                Bytes.Add(bElement[1]);
                Bytes.Add(bElement[0]);
            }
        }

        protected void Add(float value)
        {
            var bElement = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Bytes.Add(bElement[0]);
                Bytes.Add(bElement[1]);
                Bytes.Add(bElement[2]);
                Bytes.Add(bElement[3]);
            }
            else
            {
                Bytes.Add(bElement[3]);
                Bytes.Add(bElement[2]);
                Bytes.Add(bElement[1]);
                Bytes.Add(bElement[0]);
            }
        }

        protected void Add(string value)
        {
            foreach (var character in value)
                Bytes.Add((byte)character);
            Bytes.Add(0x00);
        }

        protected void FillTo(int count, byte filler = 0x00)
        {
            for (var i = Bytes.Count; i < count; i++)
                Bytes.Add(filler);
        }

        public int Len
        {
            get
            {
                return Bytes.Count;
            }
        }

        public static byte[] MessageCommand(string dataRef)
        {
            DatagramXP dgram = new();
            dgram.Add(XP_STR_CMND);
            dgram.Add(dataRef);

            return dgram.Get();
        }

        public static byte[] MessageRequest(string dataRef, int index, int rate)
        {
            DatagramXP dgram = new();
            dgram.Add(XP_STR_RREF);
            dgram.Add(rate);
            dgram.Add(index);
            dgram.Add(dataRef);
            dgram.FillTo(413);

            return dgram.Get();
        }

        public static byte[] MessageWriteRef(string dataRef, float value)
        {
            DatagramXP dgram = new();
            dgram.Add(XP_STR_DREF);
            dgram.Add(value);
            dgram.Add(dataRef);
            dgram.FillTo(509);

            return dgram.Get();
        }

        public static string GetHeader(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 4)
            {
                Logger.Error($"Could not get Header from Buffer (null: {buffer == null} | Length {buffer?.Length})");
                return null;
            }

            
            try
            {
                return Encoding.UTF8.GetString(buffer, 0, 4);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }

        public static bool ParseRREF(byte[] buffer, ref int pos, out int index, out float value)
        {
            index = -1;
            value = 0;

            if (pos + 8 > buffer.Length)
                return false;

            if (buffer == null)
            {
                Logger.Error($"Could not get Data from Buffer (NULL)");
                return false;
            }

            try
            {
                index = BitConverter.ToInt32(buffer, pos);
                pos += 4;
                value = BitConverter.ToSingle(buffer, pos);
                pos += 4;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }

            return true;
        }
    }
}


