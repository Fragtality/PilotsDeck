using System;
using System.Collections.Generic;
using System.Net.Sockets;

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

        private static byte[] MessageCommand(string dataRef)
        {
            XPDatagram dgram = new();
            dgram.Add("CMND");
            dgram.Add(dataRef);

            return dgram.Get();
        }

        public static bool SendCommand(UdpClient senderSocket, string address)
        {
            bool result = false;
            try
            {
                var buffer = XPDatagram.MessageCommand(address);
                if (senderSocket.Send(buffer, buffer.Length) > 0)
                {
                    result = true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "XPDatagram:SendCommand", $"Command '{address}' had zero Bytes sent!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "XPDatagram:SendCommand", $"Exception while sending Command '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
            return result;
        }

        private static byte[] MessageSubscribe(string dataRef, int index)
        {
            XPDatagram dgram = new();
            dgram.Add("RREF");
            dgram.Add((int)(1000 / (AppSettings.pollInterval/2)));
            dgram.Add(index);
            dgram.Add(dataRef);
            dgram.FillTo(413);

            return dgram.Get();
        }

        public static bool SendSubscribe(UdpClient senderSocket, string address, int index)
        {
            bool result = false;
            try
            {
                var buffer = MessageSubscribe(address, index);
                if (senderSocket.Send(buffer, buffer.Length) > 0)
                {
                    result = true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "XPDatagram:SendSubscribe", $"Subscription for '{address}' had zero Bytes sent!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "XPDatagram:SendSubscribe", $"Exception while sending Subscription for DataRef '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
            return result;
        }

        private static byte[] MessageUnsubscribe(string dataRef, int index)
        {
            XPDatagram dgram = new();
            dgram.Add("RREF");
            dgram.Add(0);
            dgram.Add(index);
            dgram.Add(dataRef);
            dgram.FillTo(413);

            return dgram.Get();
        }

        public static bool SendUnsubscribe(UdpClient senderSocket, string address, int index)
        {
            bool result = false;
            try
            {
                var buffer = XPDatagram.MessageUnsubscribe(address, index);
                if (senderSocket.Send(buffer, buffer.Length) > 0)
                {
                    result = true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "XPDatagram:SendUnsubscribe", $"Unsubscription for '{address}' had zero Bytes sent!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "XPDatagram:SendUnsubscribe", $"Exception while unsubscribing DataRef '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
            return result;
        }

        private static byte[] MessageWriteRef(string dataRef, float value)
        {
            XPDatagram dgram = new();
            dgram.Add("DREF");
            dgram.Add(value);
            dgram.Add(dataRef);
            dgram.FillTo(509);

            return dgram.Get();
        }

        public static bool SetDataRef(UdpClient senderSocket, string address, string value)
        {
            bool result = false;
            try
            {
                float fvalue = ModelDisplayText.GetNumValue(value, 0.0f);
                var buffer = XPDatagram.MessageWriteRef(address, fvalue);
                if (senderSocket.Send(buffer, buffer.Length) > 0)
                {
                    result = true;
                }
                else
                {
                    Logger.Log(LogLevel.Error, "XPDatagram:SetDataRef", $"Error while writing DataRef '{address}': conversion failed / zero Bytes sent!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "XPDatagram:SetDataRef", $"Exception while writing DataRef '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
            return result;
        }
    }
}


