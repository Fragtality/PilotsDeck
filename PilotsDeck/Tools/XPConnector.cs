using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public class XPConnector : IDisposable
    {
        private IPEndPoint epXplane;
        private UdpClient senderSocket = null;

        private IPEndPoint epLocal;
        private UdpClient receiverSocket = null;
        private Task receiverTask;

        private CancellationTokenSource tokenSource = null;

        private Dictionary<string, int> AddresstoIndex = new();
        private Dictionary<int, IPCValue> dataRefs = new();
        private List<int> subscribeQueue = new();
        private int nextIndex = 10;
        private bool receiverNotReady = true;
        private int simPausedReceived = 0;
        private bool socketError = false;
        private static readonly float waitDivisor = 7.5f;

        public bool CloseRequested { get; set; } = false;

        public XPConnector()
        {
            epXplane = new IPEndPoint(IPAddress.Parse(AppSettings.xpIP), AppSettings.xpPort);
        }

        public bool IsConnected { get { return senderSocket != null && senderSocket.Client.Connected; } }
        public bool IsReady { get { return IsConnected && !receiverNotReady && !socketError && simPausedReceived >= (AppSettings.waitTicks / waitDivisor); } }

        public bool Connect()
        {
            bool result = false;
            CloseRequested = false;
            try
            {
                senderSocket = new UdpClient();
                senderSocket.Connect(epXplane);
                receiverNotReady = true;
                socketError = false;

                epLocal = (IPEndPoint)senderSocket.Client.LocalEndPoint;
                receiverSocket = new UdpClient(epLocal);

                tokenSource = new CancellationTokenSource();

                receiverTask = Task.Factory.StartNew(async () =>
                {
                    while (tokenSource != null && !tokenSource.Token.IsCancellationRequested && !CloseRequested)
                    {
                        try
                        {
                            var response = await receiverSocket.ReceiveAsync().ConfigureAwait(false);
                            if (response.Buffer != null && response.Buffer.Length > 5)
                                ParseResponse(response.Buffer);
                        }
                        catch (Exception ex)
                        {
                            if (!CloseRequested)
                                Log.Logger.Error($"XPConnector: Exception while receiving Data from Socket! {ex.Message}");
                            socketError = true;
                            break;
                        }
                        socketError = false;
                    }
                    if (CloseRequested)
                        receiverSocket.Close();
                }, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                result = Subscribe("sim/time/paused", 0);
            }
            catch
            {
                Log.Logger.Error("XPConnector: Exception while establishing Sockets!");
            }

            return result;
        }

        public void Reconnect()
        {
            if (senderSocket == null && dataRefs.Count == 0)
                Connect();
            else if (senderSocket != null)
            {
                Disconnect(true);
                Connect();
                foreach (var dataRef in dataRefs)
                    Subscribe(dataRef.Value.Address, dataRef.Key);
            }
        }

        public void Disconnect(bool keepDataRefs = false)
        {
            if (senderSocket != null)
            {
                if (!keepDataRefs)
                {
                    foreach (var ipcValue in dataRefs)
                        Unsubscribe(ipcValue.Value.Address, ipcValue.Key);
                    AddresstoIndex.Clear();
                    dataRefs.Clear();
                    subscribeQueue.Clear();
                    nextIndex = 10;
                }

                tokenSource.Cancel();
                try
                {
                    if (!receiverTask.IsCompleted)
                        Task.WaitAll(new[] { receiverTask }, 50);
                }
                catch
                {
                    Log.Logger.Error($"XPConnector: Exception while waiting for receiverTask to end");
                }
                
                tokenSource.Dispose();
                tokenSource = null;

                senderSocket.Close();
                senderSocket.Dispose();
                senderSocket = null;
                receiverSocket.Close();
                receiverSocket.Dispose();
                receiverSocket = null;
                receiverNotReady = true;
                socketError = false;
            }
        }

        protected virtual void Dispose(bool a)
        {
            Disconnect();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ParseResponse(byte[] buffer)
        {
            try
            {
                string header = Encoding.UTF8.GetString(buffer, 0, 4);
                int pos = 5;

                if (header == "RREF")
                {
                    int index;
                    float value;
                    while (pos < buffer.Length)
                    {
                        index = BitConverter.ToInt32(buffer, pos);
                        pos += 4;
                        value = BitConverter.ToSingle(buffer, pos);
                        pos += 4;

                        if (index == 0)
                        {
                            if (receiverNotReady)
                            {
                                receiverNotReady = value != 0.0f;
                                Log.Logger.Debug($"XPConnector: simPaused received! (Ready {!receiverNotReady})");
                            }
                            if (simPausedReceived <= (AppSettings.waitTicks / waitDivisor))
                                simPausedReceived++;
                        }
                        else if (index >= 10)
                        {
                            if (dataRefs.ContainsKey(index))
                            {
                                if (dataRefs[index] is IPCValueDataRefString)
                                {
                                    (dataRefs[index] as IPCValueDataRefString).SetChar(0, Convert.ToChar((int)value));
                                }
                                else
                                {
                                    (dataRefs[index] as IPCValueDataRef).FloatValue = value;
                                }
                            }
                            else if (value != 0.0f)
                            {
                                foreach (var dataRef in dataRefs)
                                {
                                    if (dataRef.Value is IPCValueDataRefString)
                                    {
                                        if (index > dataRef.Key && index <= dataRef.Key + (dataRef.Value as IPCValueDataRefString).Length)
                                        {
                                            (dataRef.Value as IPCValueDataRefString).SetChar(index - dataRef.Key, Convert.ToChar((int)value));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log.Logger.Error($"XPConnector: Received Index '{index}' is not in subscribed DataRefs!");
                        }
                    }
                }
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while parsing response Buffer!");
            }
        }

        public void Process()
        {
            try
            {
                if (IsReady)
                {
                    List<int> success = new();
                    foreach (int index in subscribeQueue)
                    {
                        if (dataRefs[index] is IPCValueDataRefString)
                        {
                            var value = (IPCValueDataRefString)dataRefs[index];
                            if (SubscribeMultiple(value.BaseAddress, value.Length, index + 1))
                                success.Add(index);
                        }
                        else
                        {
                            if (Subscribe(dataRefs[index].Address, index))
                                success.Add(index);
                        }
                    }

                    if (success.Count > 0)
                        Log.Logger.Information($"XPConnector: Subscribed {success.Count} DataRefs during Process()");

                    foreach (int remIndex in success)
                        subscribeQueue.Remove(remIndex);
                }
                else
                {
                    Log.Logger.Warning($"XPConnector: Process() Call while not ready!");
                }
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while working subscription Queue!");
            }
        }

        private bool Subscribe(string address, int index)
        {
            bool result = false;
            try
            {
                var buffer = XPDatagram.MessageSubscribe(address, index);
                if (senderSocket.Send(buffer, buffer.Length) > 0)
                {
                    result = true;
                }
                else
                {
                    Log.Logger.Error($"XPConnector: Error while sending Subscription for DataRef '{address}': zero Bytes sent!");
                }
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while sending Subscription for DataRef '{address}'!");
            }
            return result;
        }

        public IPCValue Register(string address)
        {
            IPCValue ipcValue = null;

            try
            {

                if (IPCValueDataRefString.IsStringDataRef(address))
                {
                    ipcValue = new IPCValueDataRefString(address);
                    dataRefs.Add(nextIndex, ipcValue);
                    AddresstoIndex.Add(ipcValue.Address, nextIndex);
                    if (!RegisterString((IPCValueDataRefString)ipcValue))
                    {
                        ipcValue.Dispose();
                        ipcValue = null;
                    }
                }
                else
                {
                    ipcValue = new IPCValueDataRef(address);
                    dataRefs.Add(nextIndex, ipcValue);
                    AddresstoIndex.Add(ipcValue.Address, nextIndex);
                    if (!RegisterFloat((IPCValueDataRef)ipcValue))
                    {
                        ipcValue.Dispose();
                        ipcValue = null;
                    }
                }

                if (nextIndex <= 0)
                    nextIndex = 10;
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while subscribing DataRef '{address}'!");
            }

            return ipcValue;
        }

        protected bool RegisterFloat(IPCValueDataRef dataRef)
        {
            bool result = false;

            if (IsReady)
            {
                if (Subscribe(dataRef.Address, nextIndex))
                {
                    nextIndex++;
                    result = true;
                }
                else
                {
                    dataRefs.Remove(nextIndex);
                    AddresstoIndex.Remove(dataRef.Address);
                    Log.Logger.Error($"XPConnector: Error while subscribing DataRef '{dataRef.Address}': zero Bytes sent!");
                }
            }
            else
            {
                subscribeQueue.Add(nextIndex);
                nextIndex++;
                result = true;
                Log.Logger.Warning($"XPConnector: Registered DataRef '{dataRef.Address}' while disconnected");
            }

            return result;
        }

        protected bool SubscribeMultiple(string baseAddress, int Length, int startIndex)
        {
            bool result = true;

            string tmp;
            for (int i = 0; i < Length; i++)
            {
                tmp = $"{baseAddress}[{i}]";
                if (Subscribe(tmp, startIndex))
                {
                    startIndex++;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        protected bool RegisterString(IPCValueDataRefString dataRef)
        {
            bool result = true;
            int homeIndex = nextIndex;
            nextIndex += dataRef.Length + 1;

            if (IsReady)
            {
                if (!SubscribeMultiple(dataRef.BaseAddress, dataRef.Length, homeIndex))
                {
                    dataRefs.Remove(homeIndex);
                    AddresstoIndex.Remove(dataRef.Address);
                    Log.Logger.Error($"XPConnector: Error while subscribing DataRefString '{dataRef.Address}'!");
                }
            }
            else
            {
                subscribeQueue.Add(homeIndex);
                Log.Logger.Warning($"XPConnector: Registered DataRefString '{dataRef.Address}' while disconnected");
            }

            return result;
        }

        protected bool Unsubscribe(string address, int index)
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
                    Log.Logger.Error($"XPConnector: Error while unsubscribing DataRef '{address}': zero Bytes sent!");
                }
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while unsubscribing DataRef '{address}'!");
            }
            return result;
        }

        protected bool UnsubscribeMultiple(string baseAddress, int Length, int startIndex)
        {
            bool result = true;

            string tmp;
            for (int i = 0; i < Length; i++)
            {
                tmp = $"{baseAddress}[{i}]";
                if (Unsubscribe(tmp, startIndex))
                {
                    startIndex++;
                }
                else
                {
                    result = false;
                }
            }

            return result;
        }

        public bool Deregister(string address)
        {
            bool result = false;
            try
            {
                if (AddresstoIndex.ContainsKey(address) && dataRefs.ContainsKey(AddresstoIndex[address]))
                {
                    int index = AddresstoIndex[address];
                    if (dataRefs[index] is IPCValueDataRef)
                    {
                        result = Unsubscribe(address, index);
                        AddresstoIndex.Remove(address);
                        dataRefs.Remove(index);
                    }
                    else
                    {
                        var value = (IPCValueDataRefString)dataRefs[index];
                        result = UnsubscribeMultiple(value.BaseAddress, value.Length, index);
                        AddresstoIndex.Remove(address);
                        dataRefs.Remove(index);
                    }
                }
                else
                {
                    Log.Logger.Error($"XPConnector: Error while deregistering DataRef '{address}': not in dictionary!");
                }

                result = true;
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while deregistering DataRef '{address}'!");
            }
            return result;
        }

        public bool SendCommand(string address)
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
                    Log.Logger.Error($"XPConnector: Error while sending Command '{address}': zero Bytes sent!");
                }
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while sending Command '{address}'!");
            }
            return result;
        }

        public bool SetDataRef(string address, string value)
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
                    Log.Logger.Error($"XPConnector: Error while writing DataRef '{address}': conversion failed / zero Bytes sent!");
                }
            }
            catch
            {
                Log.Logger.Error($"XPConnector: Exception while writing DataRef '{address}'!");
            }
            return result;
        }
    }
}
