using System;
using System.Collections.Generic;
using System.Threading;
using StreamDeckLib;
using StreamDeckLib.Messages;
using Serilog;
using System.Diagnostics;

namespace PilotsDeck
{
       
    public class ActionController : IActionController
    {
        private Dictionary<string, IHandler> currentActions = null;
        private IPCManager ipcManager = null;
        private ImageManager imgManager = null;

        public ConnectionManager DeckManager { get; set; }
        public int Timing { get { return AppSettings.waitTicks; } }
        public bool IsApplicationOpen { get; set; }
        public string Application { get; } = AppSettings.applicationName;

        private long tickCounter = 0;
        private bool lastAppState = true;
        private bool lastConnectState = false;
        private bool lastProcessState = false;
        private bool redrawRequested = false;
        private readonly int waitTicks = AppSettings.waitTicks;
        private Stopwatch stopWatch = new Stopwatch();
        private double averageTime = 0;
       

        public ActionController()
        {
            currentActions = new Dictionary<string, IHandler>();
            ipcManager = new IPCManager(AppSettings.groupStringRead);
            imgManager = new ImageManager();
            Log.Logger.Information("ActionController and IPCManager created");
        }

        public void Dispose()
        {
            ipcManager.Dispose();
            imgManager.Dispose();
            
            Log.Logger.Information("ActionController and IPCManager Disposed");
        }

        public int Length => currentActions.Count;

        public IHandler this[string context]
        {
            get
            {
                if (currentActions.ContainsKey(context))
                    return currentActions[context];
                else
                    return null;
            }
        }

        public void Run(CancellationToken token)
        {
            stopWatch.Restart();

            tickCounter++;
            if (tickCounter < waitTicks / 7.5) //wait till streamdeck<>plugin init is done ( <150> / 7.5 = 5 Ticks => 5 * <200> = 1s )
                return;

            if (!IsApplicationOpen || tickCounter == waitTicks / 7.5)     //P3D closed or the first tick
            {
                if (lastAppState)       //P3D changed to closed
                {
                    lastAppState = false;
                    CallOnAll(handler => handler.SetDefault());
                    redrawRequested = true;
                }
            }
            else                        //P3D open
            {
                if (!lastAppState)      //P3D changed to opened
                {
                    lastAppState = true;
                    lastConnectState = false;
                    lastProcessState = false;
                    CallOnAll(handler => handler.SetWait());
                    redrawRequested = true;
                    ipcManager.Connect();
                }
                
                if (!ipcManager.IsConnected && (tickCounter % waitTicks == 0 || tickCounter == waitTicks / 7.5)) //still open not connected, check retry connection every 30s when not connected (every <150> Ticks * <200>ms)
                {
                    ipcManager.Connect();
                }
                else if (ipcManager.IsConnected)            //open and connected
                {
                    if (!lastConnectState)                  //connection changed to opened
                    {
                        lastConnectState = true;
                        CallOnAll(handler => handler.SetWait());
                    }
                    
                    if (ipcManager.Process(AppSettings.groupStringRead))
                    {
                        RefreshActions(token, !lastProcessState);   //toggles process change
                        lastProcessState = true;
                    }
                    else
                        lastProcessState = false;

                    redrawRequested = true;
                }
                else if (!ipcManager.IsConnected)       //open and disconnected
                {
                    if (lastConnectState)               //changed to disconnected
                    {
                        lastConnectState = false;
                        lastProcessState = false;
                        CallOnAll(handler => handler.SetError());
                        redrawRequested = true;
                    }
                }
            }

            if (redrawRequested)
                RedrawAll(token);

            stopWatch.Stop();
            averageTime += stopWatch.Elapsed.TotalMilliseconds;
            if (tickCounter % (waitTicks / 2) == 0) //every <150> / 2 = 75 Ticks => 75 * <200> = 15s
            {
                Log.Logger.Verbose($"ActionController: Refresh Tick #{tickCounter}, average Refresh-Time over the last {waitTicks / 2} Ticks: {averageTime / (waitTicks / 2):F3}ms");
                averageTime = 0;
            }
        }

        protected void RefreshActions(CancellationToken token, bool forceUpdate = false)
        {
            foreach (var action in currentActions.Values)
            {
                if (token.IsCancellationRequested)
                    return;
                
                if (forceUpdate)
                    action.ForceUpdate = forceUpdate;
                
                if (action.IsInitialized)
                    action.Refresh(imgManager, ipcManager);
            }
        }

        protected void CallOnAll(Action<IHandler> method)
        {
            foreach (var action in currentActions.Values)
                method(action);
        }

        protected void RedrawAll(CancellationToken token)
        {
            try
            {
                foreach (var action in currentActions)
                {
                    if (token.IsCancellationRequested)
                        return;

                    if (action.Value.NeedRedraw || action.Value.ForceUpdate)
                    {
                        //Log.Logger.Verbose($"RedrawAll: Needs Redraw [{action.Value.ActionID}] [{action.Key}] ({action.Value.NeedRedraw}, {action.Value.ForceUpdate}): {(!action.Value.IsRawImage ? action.Value.DrawImage : "raw")}");
                        if (action.Value.IsRawImage)
                            _ = DeckManager.SetImageRawAsync(action.Key, action.Value.DrawImage);
                        else 
                            _ = DeckManager.SetImageRawAsync(action.Key, imgManager.GetImageBase64(action.Value.DrawImage));
                    }

                    action.Value.ResetDrawState();
                }

                redrawRequested = false;
            }
            catch (Exception ex)
            {
                Log.Logger.Verbose($"RedrawAll: Exception {ex.Message}");
            }
        }

        public bool RunAction(string context)
        {
            try
            {
                if (!ipcManager.IsReady || !ipcManager.IsConnected)
                {
                    Log.Logger.Error($"RunAction: IPC not ready {context}");
                    return false;
                }

                if (currentActions.ContainsKey(context))
                {
                    return (currentActions[context] as IHandlerSwitch).Action(ipcManager);
                }
                else
                {
                    Log.Logger.Error($"RunAction: Could not find Context {context}");
                    return false;
                }
            }
            catch
            {
                Log.Logger.Error($"RunAction: Exception while running {context} | {currentActions[context]?.ActionID}");
                return false;
            }
        }

        public void SetTitleParameters(string context, string title, StreamDeckEventPayload.TitleParameters titleParameters)
        {
            try
            {
                if (currentActions.ContainsKey(context))
                {
                    currentActions[context].SetTitleParameters(title, StreamDeckTools.ConvertTitleParameter(titleParameters));
                }
                else
                {
                    Log.Logger.Error($"SetTitleParameters: Could not find Context {context}");
                }
            }
            catch
            {
                Log.Logger.Error($"SetTitleParameters: Exception while updating {context} | {currentActions[context]?.ActionID}");
            }
        }

        protected void SetActionState(IHandler handler)
        {
            if (!IsApplicationOpen || !handler.IsInitialized)
                handler.SetDefault();
            else if (!ipcManager.IsConnected)
                handler.SetError();
            else if (!lastProcessState)
                handler.SetWait();
            else
            {
                handler.ForceUpdate = true;
            }

            handler.NeedRedraw = true;
        }

        public void UpdateAction(string context)
        {
            try
            {
                if (currentActions.ContainsKey(context))
                {
                    currentActions[context].Update(imgManager, ipcManager);
                    SetActionState(currentActions[context]);                        

                    if (!currentActions[context].IsRawImage)
                        imgManager.UpdateImage(currentActions[context].DrawImage);

                    redrawRequested = true;
                }
                else
                {
                    Log.Logger.Error($"UpdateAction: Could not find Context {context}");
                }
            }
            catch
            {
                Log.Logger.Error($"UpdateAction: Exception while updating {context} | {currentActions[context]?.ActionID}");
            }
        }

        public void RegisterAction(string context, IHandler handler)
        {
            try
            {
                if (!currentActions.ContainsKey(context))
                {
                    currentActions.Add(context, handler);
                    handler.Register(imgManager, ipcManager);
                    SetActionState(handler);

                    redrawRequested = true;
                }
                else
                {
                    Log.Logger.Error($"RegisterAction: Context already registered! {context} | {currentActions[context].ActionID}");
                }
            }
            catch
            {
                Log.Logger.Error($"RegisterAction: Exception while registering {context} | {handler?.ActionID}");
            }
        }

        public void DeregisterAction(string context)
        {
            try
            { 
                if (currentActions.ContainsKey(context))
                {
                    currentActions[context].Deregister(imgManager, ipcManager);

                    currentActions.Remove(context);
                }
                else
                {
                    Log.Logger.Error($"DeregisterAction: Could not find Context {context}");
                }                   
            }
            catch
            {
                Log.Logger.Error($"DeregisterAction: Exception while deregistering {context}");
            }
        }

    }
}
