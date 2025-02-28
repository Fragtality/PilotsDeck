using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Actions.Advanced;
using PilotsDeck.Actions.Simple;
using PilotsDeck.Plugin;
using PilotsDeck.Resources;
using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PilotsDeck.Actions
{
    public class ActionManager
    {
        public static DeckController DeckController => App.DeckController;
        public ConcurrentDictionary<string, IAction> Actions { get; } = [];
        public ProfileSwitcherManager ProfileSwitcherManager { get; } = new();
        protected PluginState LastState { get; set; } = PluginState.IDLE;
        public int Count { get { return Actions.Count; } }
        public long Redraws { get; set; } = 0;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public IAction this[string context]
        {
            get
            {
                if (!Actions.TryGetValue(context, out IAction action))
                {
                    Logger.Error($"The context '{context}' is not registered");
                    action = null;
                }
                return action;
            }
        }

        protected void CallOnAll(Action<IAction> method)
        {
            foreach (var action in Actions.Values)
                method(action);
        }

        protected void CallOnAll(Action<IAction> method, Func<IAction, bool> predicate)
        {
            foreach (var action in Actions.Values.Where(predicate))
                method(action);
        }

        public void RegisterAction(StreamDeckEvent sdEvent)
        {
            if (Actions.ContainsKey(sdEvent.context))
            {
                Logger.Error($"The context '{sdEvent.context}' is already registered");
                return;
            }

            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.profile.switcher")
                ProfileSwitcherManager.RegisterProfileSwitcher(sdEvent.context);
            else
            {
                IAction action = ActionMeta.CreateInstance(sdEvent);
                action ??= ActionBaseSimple.CreateInstance(sdEvent);
                Actions.TryAdd(sdEvent.context, action);
                if (action?.SettingModelUpdated == true)
                {
                    _ = App.DeckController.SendSetSettings(action.Context, action.GetSettingModel());
                    action.SettingModelUpdated = false;
                }
            }
        }

        public void DeregisterAction(StreamDeckEvent sdEvent)
        {
            if (ProfileSwitcherManager.HasContext(sdEvent.context))
            {
                ProfileSwitcherManager.DeregisterProfileSwitcher(sdEvent.context);
                return;
            }
            
            if (!Actions.TryGetValue(sdEvent.context, out IAction action))
            {
                Logger.Error($"The context '{sdEvent.context}' is not registered");
                return;
            }

            if (Actions.TryRemove(sdEvent.context, out _))
            {
                App.ActiveDesigner.Remove(sdEvent.context);
                action.DeregisterRessources();
            }
        }

        public SimCommand[] OnTouchTap(StreamDeckEvent sdEvent)
        {
            return this[sdEvent.context]?.OnTouchTap(sdEvent);
        }

        public SimCommand[] OnDialDown(StreamDeckEvent sdEvent)
        {
            return this[sdEvent.context]?.OnDialDown(sdEvent);
        }

        public SimCommand[] OnDialUp(StreamDeckEvent sdEvent)
        {
            return this[sdEvent.context]?.OnDialUp(sdEvent);
        }

        public SimCommand[] OnDialRotate(StreamDeckEvent sdEvent)
        {
            return this[sdEvent.context]?.OnDialRotate(sdEvent);
        }

        public SimCommand[] OnKeyDown(StreamDeckEvent sdEvent)
        {
            if (ProfileSwitcherManager.HasContext(sdEvent.context))
                return null;
            else
                return this[sdEvent.context]?.OnKeyDown(sdEvent);
        }

        public SimCommand[] OnKeyUp(StreamDeckEvent sdEvent)
        {
            if (ProfileSwitcherManager.HasContext(sdEvent.context))
            {
                ProfileSwitcherManager.ToggleEnableSwitching();
                return null;
            }
            else
                return this[sdEvent.context]?.OnKeyUp(sdEvent);
        }

        public void OpenActionDesigner(string context)
        {
            var action = this[context];
            if (action == null || action is not ActionMeta)
                return;

            App.DesignerQueue.Enqueue(action as ActionMeta);
            SendMessage(App.WindowHandle, AppConfiguration.WM_PILOTSDECK_REQ_DESIGNER, IntPtr.Zero, IntPtr.Zero);
            Logger.Verbose("Send WM_PILOTSDECK_REQ_DESIGNER to WindowHandle");
        }

        public void PropertyInspectorDidAppear(StreamDeckEvent sdEvent)
        {
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.profile.switcher")
                ProfileSwitcherManager.SetAddPropertyInspector(sdEvent.context);
        }

        public void PropertyInspectorDidDisappear(StreamDeckEvent sdEvent)
        {
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.profile.switcher")
                ProfileSwitcherManager.RemoveClearPropertyInspector(sdEvent.context);
        }

        public void SendPropertyInspectorModel(StreamDeckEvent sdEvent)
        {
            if (ProfileSwitcherManager.HasContext(sdEvent.context))
                _ = DeckController.SendToPropertyInspector(sdEvent.context, ProfileSwitcherManager.CreatePropertyInspectorModel());
            else
                _ = DeckController.SendToPropertyInspector(sdEvent.context, new PropertyInspectorModel());
        }

        public void TransferSettingModel(string context, string msg)
        {
            try
            {
                var action = this[context];
                if (action == null)
                    return;

                if (msg == "SettingsModelCopy")
                {
                    StreamDeckEvent sdEvent = new()
                    {
                        payload = new StreamDeckEvent.Payload()
                    };
                    sdEvent.payload.settings = action.GetSettingModel();
                    sdEvent.action = action.GetType().FullName;
                    string json = JsonSerializer.Serialize(sdEvent);
                    Logger.Debug($"Copy to Clipboard: {json}");
                    ClipboardHelper.SetClipboard(json.Base64Encode());
                }
                else if (msg == "SettingsModelPaste")
                {
                    string json = ClipboardHelper.GetClipboard();
                    if (string.IsNullOrWhiteSpace(json) || json?.Length < 1)
                    {
                        Logger.Warning($"Clipboard does not contain Text");
                        return;
                    }
                    if (!json.StartsWith("{\"action\""))
                        json = json.Base64Decode();

                    Logger.Debug($"Pasted from Clipboard: {json}");
                    StreamDeckEvent sdEvent = JsonSerializer.Deserialize<StreamDeckEvent>(json);
                    if (sdEvent?.action != action.GetType().FullName)
                    {
                        Logger.Warning($"Settings do not match the current Action");
                        return;
                    }

                    action.SetSettingModel(sdEvent);
                    _ = App.DeckController.SendSetSettings(action.Context, action.GetSettingModel());
                    action.SettingModelUpdated = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void SetSettingModel(StreamDeckEvent sdEvent)
        {
            SetSettingModel(this[sdEvent.context], sdEvent);
        }

        public static void SetSettingModel(IAction action, StreamDeckEvent sdEvent)
        {
            if (action == null)
                return;

            action.SetSettingModel(sdEvent);
            if (action.SettingModelUpdated == true)
            {
                _ = App.DeckController.SendSetSettings(action.Context, action.GetSettingModel());
                action.SettingModelUpdated = false;
            }
        }

        public void SetTitleParameters(StreamDeckEvent sdEvent)
        {
            if (Actions.TryGetValue(sdEvent.context, out IAction action))
            {
                action.SetTitleParameters(sdEvent.payload.title, sdEvent.payload.titleParameters);
                action.NeedRefresh = true;
            }
        }

        public void Refresh(bool forced)
        {
            if (forced)
                CallOnAll(a => a.NeedRefresh = true);
            CallOnAll(a => a.Refresh());
        }

        public void Redraw(PluginState state)
        {
            try
            {
                foreach (var action in Actions)
                {
                    if (action.Value.NeedRedraw || LastState != state)
                    {
                        Redraws++;
                        Logger.Verbose($"--REDRAW-- for State '{state}' - Needs Redraw, Refresh ({action.Value.NeedRedraw}, {action.Value.NeedRefresh}) ID {action.Value.ActionID}");
                        if (action.Value.IsEncoder)
                        {
                            _ = DeckController.SetFeedbackItemImageRaw(AppConfiguration.SdTargetImage, action.Key, SelectImage(action.Value, state));
                            if (action.Value.FirstLoad)
                            {
                                _ = DeckController.SendSetImageRaw(action.Key, ImageManager.DEFAULT_ENCODER);
                                action.Value.FirstLoad = false;
                            }
                        }
                        else
                            _ = DeckController.SendSetImageRaw(action.Key, SelectImage(action.Value, state));

                        action.Value.ResetDrawState();
                    }
                }

                LastState = state;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected static string SelectImage(IAction action, PluginState state)
        {
            if (state == PluginState.WAIT)
                return ImageManager.DEFAULT_WAIT.GetImageVariant(action.CanvasInfo);
            else
                return action.RenderImage64;
        }
    }
}
