using CFIT.AppLogger;
using System;
using System.Net;
using System.Net.Sockets;

namespace PilotsDeck.Simulator.XP
{
    public class SocketXP(ConnectorXP connector) : IDisposable
    {

        protected ConnectorXP ConnectorXP {  get; set; } = connector;
        protected IPEndPoint EndPointXP { get; set; } = new IPEndPoint(App.Configuration.ParsedXPlaneIP, App.Configuration.XPlanePort);
        protected IPEndPoint EndPointLocal { get; set; } = null;
        public UdpClient SocketSend { get; protected set; } = null;
        public UdpClient SocketReceive { get; protected set; } = null;
        public bool IsConnected { get { return SocketSend?.Client?.Connected == true; } }


        public bool Connect()
        {
            if (IsConnected)
                return true;

            bool result = false;
            try
            {
                ReleaseSockets();

                Logger.Information($"Connecting to X-Plane on '{App.Configuration.XPlaneIP}:{App.Configuration.XPlanePort}' ...");
                
                SocketSend = new UdpClient();
                SocketSend.Connect(EndPointXP);
                if (!SocketSend.Client.Connected)
                {
                    Logger.Warning($"X-Plane Socket not connected - abort");
                    SocketSend?.Dispose();
                    SocketSend = null;
                    return result;
                }

                EndPointLocal = (IPEndPoint)SocketSend.Client.LocalEndPoint;
                SocketReceive = new UdpClient(EndPointLocal);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public void Close()
        {
            try { SocketReceive?.Close(); } catch { }
            try { SocketSend?.Close(); } catch { }
            ReleaseSockets();
        }

        protected bool Send(byte[] data)
        {
            try
            {
                _ = SocketSend.SendAsync(data, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException (ex);
                return false;
            }
        }

        public void SendSubscribe(string dataref, int index, int rate)
        {
            Send(DatagramXP.MessageRequest(dataref, index, rate));
        }

        public void SendSubscribe(string[] dataref, int[] index, int rate)
        {
            for (int i = 0; i < dataref.Length; i++)
                Send(DatagramXP.MessageRequest(dataref[i], index[i], rate));
        }

        public void SendUnsubscribe(string dataref, int index)
        {
            Send(DatagramXP.MessageRequest(dataref, index, 0));
        }

        public void SendUnsubscribe(string[] dataref, int[] index)
        {
            for (int i = 0; i < dataref.Length; i++)
                Send(DatagramXP.MessageRequest(dataref[i], index[i], 0));
        }

        public bool SendCommand(string dataRef)
        {
            return Send(DatagramXP.MessageCommand(dataRef));
        }

        public bool SendWriteRef(string dataRef, float value)
        {
            return Send(DatagramXP.MessageWriteRef(dataRef, value));
        }

        protected void ReleaseSockets()
        {
            if (IsConnected)
                try { SocketSend?.Close(); } catch { }
            EndPointLocal = null;
            SocketSend?.Dispose();
            SocketSend = null;
            SocketReceive?.Dispose();
            SocketReceive = null;
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ReleaseSockets();                 
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
