using System;
using System.Collections.Generic;
using System.Threading;
using StreamDeckLib;
using StreamDeckLib.Messages;
using Serilog;
using System.Diagnostics;

namespace PilotsDeck
{
       
    public class ActionController : IDisposable
    {
        private Dictionary<string, IHandler> currentActions = null;
        private IPCManager ipcManager = null;
        private ImageManager imgManager = null;
        private ConnectionManager deckMangager = null;
        private readonly object threadLock = new object();

        private long tickCounter = 0;
        private bool lastConnectState = true;
        private bool lastProcessState = false;
        private bool redrawRequested = false;
        private readonly int waitTicks = AppSettings.waitTicks;
        private Stopwatch stopWatch = new Stopwatch();
        private long averageTime = 0;
       

        public ActionController(ConnectionManager manager)
        {
            currentActions = new Dictionary<string, IHandler>();
            ipcManager = new IPCManager(AppSettings.groupStringRead);
            imgManager = new ImageManager();
            deckMangager = manager;
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

        public void Refresh(CancellationToken token)
        {
            lock (threadLock)
            {
                stopWatch.Restart();

                tickCounter++;
                if (tickCounter < waitTicks / 15) //wait till streamdeck<>plugin init is done
                    return;

                bool connected = ipcManager.IsConnected;
                if (!connected && tickCounter % waitTicks == 0 || !connected && tickCounter == waitTicks / 15) //reduce retries while not connected
                    connected = ipcManager.Connect();
                
                if (!connected) //offline
                {
                    if (!lastConnectState && !redrawRequested) //still offline
                        return;
                    else if (redrawRequested)
                    {
                        RedrawAll(token);
                        return;
                    }
                    else //changed to offline
                    {
                        lastConnectState = false;
                        lastProcessState = false;
                        Log.Logger.Information("ActionController: Changed to Offline - SetErrorAll");
                        SetErrorAll(token);
                        return;
                    }
                }
                else //online
                {
                    bool result = ipcManager.Process(AppSettings.groupStringRead);

                    if (result)
                    {
                        if (!lastConnectState || !lastProcessState) //changed to online
                        {
                            lastConnectState = true;
                            lastProcessState = true;
                            Log.Logger.Information("ActionController: Changed to Online and Process okay - RefreshAll forced");
                            RefreshActions(token, true);
                        }
                        else //still online
                        {
                            lastProcessState = true;
                            RefreshActions(token, false);
                        }
                    }
                    else
                        lastProcessState = false;

                    RedrawAll(token);
                }

                stopWatch.Stop();
                averageTime += stopWatch.ElapsedMilliseconds;
                if (tickCounter % (waitTicks / 2) == 0)
                {
                    Log.Logger.Verbose($"ActionController: Refresh Tick #{tickCounter}, average Refresh-Time over the last {waitTicks / 2} Ticks: {averageTime / (waitTicks / 2)}ms");
                    averageTime = 0;
                }
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

        protected void SetErrorAll(CancellationToken token)
        {
            foreach (var action in currentActions.Values)
                action.SetError();

            RedrawAll(token);
        }

        protected void RedrawAll(CancellationToken token)
        {
            //lock (threadLock)
            //{
                try
                {
                    foreach (var action in currentActions)
                    {
                        if (token.IsCancellationRequested)
                            return;

                        if (action.Value.NeedRedraw || action.Value.ForceUpdate)
                        {
                            Log.Logger.Verbose($"RedrawAll: Needs Redraw [{action.Value.ActionID}] [{action.Key}] ({action.Value.NeedRedraw}, {action.Value.ForceUpdate}): {(!action.Value.IsRawImage ? action.Value.DrawImage : "raw")}");
                            if (action.Value.IsRawImage)
                                _ = deckMangager.SetImageRawAsync(action.Key, action.Value.DrawImage);
                            else 
                                _ = deckMangager.SetImageRawAsync(action.Key, imgManager.GetImageBase64(action.Value.DrawImage));
                        }

                        action.Value.ResetDrawState();
                    }

                    redrawRequested = false;
                }
                catch (Exception ex)
                {
                    Log.Logger.Verbose($"RedrawAll: Exception {ex.Message}");
                }
            //}
            
        }

        public bool RunAction(string context)
        {
            lock (threadLock)
            {
                try
                {
                    if (ipcManager.IsReady && currentActions.ContainsKey(context) && currentActions[context] is IHandlerSwitch)
                    {
                        return (currentActions[context] as IHandlerSwitch).Action(ipcManager);
                    }
                    else
                    {
                        Log.Logger.Error($"RunAction: Not ready or could not find Context {context}");
                        return false;
                    }
                }
                catch
                {
                    Log.Logger.Error($"RunAction: Exception while running {context} | {currentActions[context]?.ActionID}");
                    return false;
                }
            }
        }

        public void SetTitleParameters(string context, string title, StreamDeckEventPayload.TitleParameters titleParameters)
        {
            lock (threadLock)
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
        }

        public void UpdateAction(string context)
        {
            lock (threadLock)
            {
                try
                {
                    if (currentActions.ContainsKey(context))
                    {
                        currentActions[context].Update(ipcManager);

                        if (ipcManager.IsConnected && lastConnectState)
                            currentActions[context].ForceUpdate = true;
                        else
                            currentActions[context].SetError();

                        if (!currentActions[context].IsRawImage)
                            imgManager.UpdateImage(currentActions[context].DrawImage);

                        if (currentActions[context] is IHandlerValue)
                            (currentActions[context] as IHandlerValue).UpdateAddress(ipcManager);

                        currentActions[context].NeedRedraw = true;
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
        }

        public void RegisterAction(string context, IHandler handler)
        {
            lock (threadLock)
            {
                try
                { 
                    if (!currentActions.ContainsKey(context))
                    {
                        currentActions.Add(context, handler);
                        if (ipcManager.IsConnected && lastConnectState)
                            handler.ForceUpdate = true;
                        else
                            handler.SetError();

                        imgManager.AddImage(handler.DrawImage);

                        if (currentActions[context] is IHandlerValue)
                            (currentActions[context] as IHandlerValue).RegisterAddress(ipcManager);

                        handler.NeedRedraw = true;
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
        }

        public void DeregisterAction(string context)
        {
            lock (threadLock)
            {
                try
                { 
                    if (currentActions.ContainsKey(context))
                    {
                        if (currentActions[context] is IHandlerValue)
                            (currentActions[context] as IHandlerValue).DeregisterAddress(ipcManager);

                        if (!currentActions[context].IsRawImage)
                            imgManager.RemoveImage(currentActions[context].DrawImage);

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
}
