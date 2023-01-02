using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PilotsDeck
{
    public class ConnectorXP : SimulatorConnector, IDisposable
    {
        protected IPEndPoint epXplane;
        protected UdpClient senderSocket = null;

        protected IPEndPoint epLocal;
        protected UdpClient receiverSocket = null;
        protected Task receiverTask;

        protected CancellationTokenSource tokenSource = null;

        protected Dictionary<string, int> AddresstoIndex = new();
        protected Dictionary<int, IPCValue> dataRefs = new();
        protected List<int> subscribeQueue = new();
        protected int nextIndex = 10;
        protected bool receiverNotReady = true;
        protected int simPausedReceived = 0;
        protected bool socketError = false;
        protected static readonly float waitDivisor = 7.5f;

        public bool CloseRequested { get; set; } = false;

        public override bool IsConnected { get { return senderSocket != null && senderSocket.Client.Connected; } protected set { } }
        public override bool IsReady { get { return IsConnected && !receiverNotReady && !socketError && simPausedReceived >= (AppSettings.waitTicks / waitDivisor); } }

        public override bool IsRunning { get { return GetProcessRunning("X-Plane"); } }
        public override bool IsPaused { get; protected set; }

        protected static readonly string AircraftRefString = "sim/aircraft/view/acf_relative_path:s32";
        protected IPCValueDataRefString AircraftValue = null;
        public override string AicraftString { get { return AircraftValue == null ? "" : AircraftValue.Value; } protected set { } }

        public ConnectorXP()
        {
            epXplane = new IPEndPoint(IPAddress.Parse(AppSettings.xpIP), AppSettings.xpPort);
        }

        public override void Close()
        {
            try
            { 
                if (AircraftValue != null)
                {
                    UnsubscribeAddress(AircraftRefString);
                    AircraftValue.Dispose();
                    AircraftValue = null;
                }

                foreach (var dataRef in AddresstoIndex.Keys)
                    UnsubscribeAddress(dataRef);
            }
            catch
            {
                Log.Logger.Error($"ConnectorXP: Exception while unsubscribing DataRefs");
            }

            if (senderSocket != null)
            {
                if (tokenSource != null)
                {
                    tokenSource.Cancel();
                    try
                    {
                        if (!receiverTask.IsCompleted)
                            Task.WaitAll(new[] { receiverTask }, 50);
                    }
                    catch
                    {
                        Log.Logger.Error($"ConnectorXP: Exception while waiting for receiverTask to end");
                    }

                    tokenSource.Dispose();
                    tokenSource = null;
                }

                senderSocket.Close();
                senderSocket.Dispose();
                senderSocket = null;

                if (receiverSocket != null)
                {
                    receiverSocket.Close();
                    receiverSocket.Dispose();
                    receiverSocket = null;
                }
                receiverNotReady = true;
                socketError = false;
            }
        }

        public override bool Connect()
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
                                Log.Logger.Error($"ConnectorXP: Exception while receiving Data from Socket! {ex.Message}");
                            socketError = true;
                            break;
                        }
                        socketError = false;
                    }
                    if (CloseRequested)
                        receiverSocket.Close();
                }, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                result = XPDatagram.SendSubscribe(senderSocket, "sim/time/paused", 0);
            }
            catch
            {
                Log.Logger.Error("ConnectorXP: Exception while establishing Sockets!");
                if (senderSocket != null)
                {
                    if ((bool)senderSocket?.Client?.Connected)
                        senderSocket.Close();
                    senderSocket.Dispose();
                    senderSocket = null;
                }
                if (receiverSocket != null)
                {
                    if ((bool)receiverSocket?.Client?.Connected)
                        receiverSocket.Close();
                    receiverSocket.Dispose();
                    receiverSocket = null;
                }
                if (receiverTask != null && tokenSource != null)
                    tokenSource.Cancel();
            }

            return result;
        }

        public override void UnsubscribeAddress(string address)
        {
            try
            {
                if (AddresstoIndex.TryGetValue(address, out int index) && dataRefs.TryGetValue(index, out IPCValue value))
                {
                    if (value is IPCValueDataRef)
                    {
                        XPDatagram.SendUnsubscribe(senderSocket, address, index);
                        AddresstoIndex.Remove(address);
                        dataRefs.Remove(index);
                    }
                    else
                    {
                        UnsubscribeMultiple((value as IPCValueDataRefString).BaseAddress, (value as IPCValueDataRefString).Length, index);
                        AddresstoIndex.Remove(address);
                        dataRefs.Remove(index);
                    }
                }
                else
                {
                    Log.Logger.Error($"ConnectorXP: Error while deregistering DataRef '{address}': not in dictionary!");
                }
            }
            catch
            {
                Log.Logger.Error($"ConnectorXP: Exception while deregistering DataRef '{address}'!");
            }
        }

        protected bool UnsubscribeMultiple(string baseAddress, int Length, int startIndex)
        {
            bool result = true;

            string tmp;
            for (int i = 0; i < Length; i++)
            {
                tmp = $"{baseAddress}[{i}]";
                if (XPDatagram.SendUnsubscribe(senderSocket, tmp, startIndex))
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

        public override void Init(long tickCounter, IPCManager manager)
        {
            TickCounter = tickCounter;
            ipcManager = manager;

            SimType = SimulatorType.XP;
        }

        public override bool Process()
        {
            resultProcess = false;
            try
            {
                if (IsReady)
                {
                    if (AircraftValue == null)
                    {
                        AircraftValue = (IPCValueDataRefString)ipcManager.RegisterAddress(AircraftRefString);
                    }

                    SubscribeQueue();

                    resultProcess = true;
                }
                else
                {
                    Log.Logger.Warning($"ConnectorXP: Process() Call while not ready!");
                }
            }
            catch
            {
                Log.Logger.Error($"ConnectorXP: Exception while working subscription Queue!");
            }

            return resultProcess;
        }

        protected virtual void SubscribeQueue()
        {
            List<int> success = new();
            foreach (int index in subscribeQueue)
            {
                if (dataRefs[index] is IPCValueDataRefString value)
                {
                    if (SubscribeMultiple(value.BaseAddress, value.Length, index + 1))
                        success.Add(index);
                }
                else
                {
                    if (XPDatagram.SendSubscribe(senderSocket, dataRefs[index].Address, index))
                        success.Add(index);
                }
            }

            if (success.Count > 0)
            {
                Log.Logger.Information($"ConnectorXP: Subscribed {success.Count} DataRefs during Process()");
                foreach (int remIndex in success)
                    subscribeQueue.Remove(remIndex);
            }
        }

        public override void SubscribeAllAddresses()
        {
            foreach (var address in ipcManager.AddressList)
            { 
                if (!AddresstoIndex.ContainsKey(address))
                    SubscribeAddress(address);
            }
            Log.Logger.Debug("ConnectorXP: Subscribed all IPCValues");
        }

        public override void SubscribeAddress(string address)
        {
            try
            {
                if (IPCTools.rxDref.IsMatch(address))
                {
                    dataRefs.Add(nextIndex, ipcManager[address]);
                    AddresstoIndex.Add(address, nextIndex);

                    if (IPCValueDataRefString.IsStringDataRef(address))
                        RegisterString(address);
                    else
                        RegisterFloat(address);
                }
            }
            catch
            {
                Log.Logger.Error($"ConnectorXP: Exception while subscribing DataRef '{address}'!");
            }
        }

        protected void RegisterFloat(string address)
        {
            if (IsReady && XPDatagram.SendSubscribe(senderSocket, address, nextIndex))
            {
                nextIndex++;
            }
            else
            {
                subscribeQueue.Add(nextIndex);
                nextIndex++;
                Log.Logger.Warning($"ConnectorXP: Error while subscribing DataRef '{address}'! Added to Queue");
            }
        }

        protected bool SubscribeMultiple(string baseAddress, int Length, int startIndex)
        {
            bool result = true;

            string tmp;
            for (int i = 0; i < Length; i++)
            {
                tmp = $"{baseAddress}[{i}]";
                if (XPDatagram.SendSubscribe(senderSocket, tmp, startIndex))
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

        protected void RegisterString(string address)
        {
            int homeIndex = nextIndex;
            string baseAddress = address.Split(':')[0];
            _ = int.TryParse(address.Split(':')[1][1..], out int length);

            nextIndex += length + 1;

            if (IsReady)
            {
                if (!SubscribeMultiple(baseAddress, length, homeIndex))
                {
                    subscribeQueue.Add(homeIndex);
                    Log.Logger.Warning($"ConnectorXP: Error while subscribing DataRefString '{address}'! Added to Queue");
                }
            }
            else
            {
                subscribeQueue.Add(homeIndex);
                Log.Logger.Warning($"ConnectorXP: Registered DataRefString '{address}' while disconnected");
            }
        }

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, string offValue = null)
        {
            switch (actionType)
            {
                case ActionSwitchType.XPCMD:
                    return RunCommands(Address);
                case ActionSwitchType.XPWREF:
                    return XPDatagram.SetDataRef(senderSocket, Address, newValue);
                default:
                    Log.Logger.Error($"ConnectorXP: ActionType {actionType} not valid for Address {Address}");
                    return false;
            }
        }

        protected bool RunCommands(string Address)
        {
            bool result = false;
            string[] commands = Address.Split(':');

            if (commands.Length > 1)
            {
                foreach (string command in commands)
                {
                    result = XPDatagram.SendCommand(senderSocket, command);
                    Thread.Sleep(AppSettings.controlDelay);
                }
            }
            else if (commands.Length == 1)
            {
                result = XPDatagram.SendCommand(senderSocket, Address);
            }
            else
            {
                Log.Logger.Error($"ConnectorXP: Command-Array has zero members! Address: {Address}");
            }

            return result;
        }

        protected virtual void Dispose(bool a)
        {
            if (senderSocket != null)
            {
                foreach (var ipcValue in dataRefs)
                    XPDatagram.SendUnsubscribe(senderSocket, ipcValue.Value.Address, ipcValue.Key);
                AddresstoIndex.Clear();
                dataRefs.Clear();
                subscribeQueue.Clear();

                tokenSource.Cancel();
                try
                {
                    if (!receiverTask.IsCompleted)
                        Task.WaitAll(new[] { receiverTask }, 50);
                }
                catch
                {
                    Log.Logger.Error($"ConnectorXP: Exception while waiting for receiverTask to end");
                }

                tokenSource.Dispose();
                tokenSource = null;

                try
                {
                    senderSocket.Close();
                }
                catch
                {
                    Log.Logger.Error($"ConnectorXP: Exception while closing senderSocket");
                }
                try
                {
                    receiverSocket.Close();
                }
                catch
                {
                    Log.Logger.Error($"ConnectorXP: Exception while closing receiverSocket");
                }

                senderSocket.Dispose();
                senderSocket = null;
                receiverSocket.Dispose();
                receiverSocket = null;
            }
        }

        public override void Dispose()
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
                                Log.Logger.Debug($"ConnectorXP: simPaused received! (Ready {!receiverNotReady})");
                            }
                            if (simPausedReceived <= (AppSettings.waitTicks / waitDivisor))
                                simPausedReceived++;
                            else
                                IsPaused = value != 0.0f;
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
                                    var ipcValue = (IPCValueDataRef)dataRefs[index];
                                    if (ipcValue != null)
                                        ipcValue.FloatValue = value;
                                    else
                                        Log.Logger.Error($"ConnectorXP: IPCValue at index '{index}' is Null! Address {AddresstoIndex.Where((k, v) => v == index).FirstOrDefault()}");
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
                            Log.Logger.Error($"ConnectorXP: Received Index '{index}' is not in subscribed DataRefs!");
                        }
                    }
                }
            }
            catch
            {
                Log.Logger.Error($"ConnectorXP: Exception while parsing response Buffer!");
            }
        }
    }
}
