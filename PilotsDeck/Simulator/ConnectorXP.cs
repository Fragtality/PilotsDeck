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

        public override bool IsConnected { get { return senderSocket != null && senderSocket.Client.Connected && !socketError; } protected set { } }
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
                    ipcManager.DeregisterAddress(AircraftRefString);
                    AircraftValue.Dispose();
                    AircraftValue = null;
                }

                foreach (var dataRef in AddresstoIndex.Keys)
                    UnsubscribeAddress(dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorXP:Close", $"Exception while unsubscribing DataRefs! (Exception: {ex.GetType()}) (Message: {ex.Message})");
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
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Critical, "ConnectorXP:Close", $"Exception while waiting for receiverTask to end! (Exception: {ex.GetType()}) (Message: {ex.Message})");
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
                                Logger.Log(LogLevel.Critical, "ConnectorXP:Connect", $"Exception while receiving Data from Socket! (Exception: {ex.GetType()}) (Message: {ex.Message})");

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
            catch (Exception exOuter)
            {
                Logger.Log(LogLevel.Critical, "ConnectorXP:Connect", $"Exception while establishing Sockets! (Exception: {exOuter.GetType()}) (Message: {exOuter.Message})");
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
                    Logger.Log(LogLevel.Error, "ConnectorXP:UnsubscribeAddress", $"DataRef '{address}' is not in dictionary!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorXP:UnsubscribeAddress", $"Exception while subscribing DataRef '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
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
                    Logger.Log(LogLevel.Warning, "ConnectorXP:Process", $"Call while not ready!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorXP:Process", $"Exception while working subscription Queue! (Count: {subscribeQueue?.Count}) (Exception: {ex.GetType()}) (Message: {ex.Message})");
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
                Logger.Log(LogLevel.Information, "ConnectorXP:SubscribeQueue", $"Subscribed {success.Count} DataRefs during Process.");
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
            Logger.Log(LogLevel.Information, "ConnectorXP:SubscribeAllAddresses", $"Subscribed all IPCValues.");
        }

        public override void SubscribeAddress(string address)
        {
            try
            {
                if (IPCTools.rxDref.IsMatch(address) && !AddresstoIndex.ContainsKey(address))
                {
                    dataRefs.Add(nextIndex, ipcManager[address]);
                    AddresstoIndex.Add(address, nextIndex);

                    if (IPCValueDataRefString.IsStringDataRef(address))
                        RegisterString(address);
                    else
                        RegisterFloat(address);
                }
                else if (!IPCTools.rxDref.IsMatch(address))
                    Logger.Log(LogLevel.Error, "ConnectorXP:SubscribeAddress", $"The Address '{address}' is not valid!");
                else
                    Logger.Log(LogLevel.Error, "ConnectorXP:SubscribeAddress", $"The Address '{address}' is already subscribed!");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorXP:SubscribeAddress", $"Exception while subscribing DataRef '{address}'! (Exception: {ex.GetType()}) (Message: {ex.Message})");
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
                Logger.Log(LogLevel.Warning, "ConnectorXP:RegisterFloat", $"Not Ready / Error while subscribing DataRef '{address}'! Added to Queue.");
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
                    Logger.Log(LogLevel.Warning, "ConnectorXP:RegisterString", $"Error while subscribing DataRef '{address}'! Added to Queue.");
                }
            }
            else
            {
                subscribeQueue.Add(homeIndex);
                Logger.Log(LogLevel.Warning, "ConnectorXP:RegisterString", $"Not ready to subscribe DataRef '{address}'! Added to Queue.");
            }
        }

        public override bool RunAction(string Address, ActionSwitchType actionType, string newValue, IModelSwitch switchSettings, int ticks = 1)
        {
            switch (actionType)
            {
                case ActionSwitchType.XPCMD:
                    return RunCommands(Address, switchSettings.UseControlDelay);
                case ActionSwitchType.XPWREF:
                    return XPDatagram.SetDataRef(senderSocket, Address, newValue);
                default:
                    Logger.Log(LogLevel.Error, "ConnectorXP:RunAction", $"Action-Type '{actionType}' not valid for Address '{Address}'!");
                    return false;
            }
        }

        protected bool RunCommands(string Address, bool useDelay)
        {
            bool result = false;
            string[] commands = Address.Split(':');

            if (commands.Length > 1)
            {
                foreach (string command in commands)
                {
                    result = XPDatagram.SendCommand(senderSocket, command);
                    if (useDelay)
                        Thread.Sleep(AppSettings.controlDelay);
                }
            }
            else if (commands.Length == 1)
            {
                result = XPDatagram.SendCommand(senderSocket, Address);
            }
            else
            {
                Logger.Log(LogLevel.Error, "ConnectorXP:RunCommands", $"Command-Array has zero members! (Address: {Address})");
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
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Critical, "ConnectorXP:Dispose", $"Exception while waiting for receiverTask to end! (Exception: {ex.GetType()}) (Message: {ex.Message})");
                }

                tokenSource.Dispose();
                tokenSource = null;

                try
                {
                    senderSocket.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Critical, "ConnectorXP:Dispose", $"Exception while closing senderSocket! (Exception: {ex.GetType()}) (Message: {ex.Message})");
                }
                try
                {
                    receiverSocket.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Critical, "ConnectorXP:Dispose", $"Exception while closing receiverSocket! (Exception: {ex.GetType()}) (Message: {ex.Message})");
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
                                Logger.Log(LogLevel.Information, "ConnectorXP:ParseResponse", $"DataRef for 'simPaused' received! (Ready {!receiverNotReady})");
                            }
                            if (simPausedReceived <= (AppSettings.waitTicks / waitDivisor))
                                simPausedReceived++;
                            else
                                IsPaused = value != 0.0f;
                        }
                        else if (index >= 10)
                        {
                            if (dataRefs.TryGetValue(index, out IPCValue ipcValue))
                            {
                                if (ipcValue is IPCValueDataRefString)
                                {
                                    (ipcValue as IPCValueDataRefString).SetChar(0, Convert.ToChar((int)value));
                                }
                                else
                                {
                                    if (ipcValue != null && ipcValue is IPCValueDataRef)
                                        (ipcValue as IPCValueDataRef).FloatValue = value;
                                    else
                                        Logger.Log(LogLevel.Error, "ConnectorXP:ParseResponse", $"IPCValue at Index '{index}' is Null! (Address {AddresstoIndex.Where((k, v) => v == index).FirstOrDefault()})");
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
                            Logger.Log(LogLevel.Error, "ConnectorXP:ParseResponse", $"Received Index '{index}' is not in subscribed DataRefs!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ConnectorXP:ParseResponse", $"Exception while parsing response Buffer! (Exception: {ex.GetType()}) (Message: {ex.Message})");
            }
        }
    }
}
