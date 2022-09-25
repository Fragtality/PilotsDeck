using System;
using System.Collections.Generic;

namespace PilotsDeck
{
    public class XPDatagram
    {
        public List<byte> Bytes { get; set; }

        public XPDatagram()
        {
            Bytes = new List<byte>();
        }

        public byte[] Get()
        {
            return Bytes.ToArray();
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
            XPDatagram dgram = new();
            dgram.Add("CMND");
            dgram.Add(dataRef);

            return dgram.Get();
        }

        public static byte[] MessageSubscribe(string dataRef, int index)
        {
            XPDatagram dgram = new();
            dgram.Add("RREF");
            dgram.Add((int)(1000 / (AppSettings.pollInterval/2)));
            dgram.Add(index);
            dgram.Add(dataRef);
            dgram.FillTo(413);

            return dgram.Get();
        }

        public static byte[] MessageUnsubscribe(string dataRef, int index)
        {
            XPDatagram dgram = new();
            dgram.Add("RREF");
            dgram.Add(0);
            dgram.Add(index);
            dgram.Add(dataRef);
            dgram.FillTo(413);

            return dgram.Get();
        }

        public static byte[] MessageWriteRef(string dataRef, float value)
        {
            XPDatagram dgram = new();
            dgram.Add("DREF");
            dgram.Add(value);
            dgram.Add(dataRef);
            dgram.FillTo(509);

            return dgram.Get();
        }
    }
}


