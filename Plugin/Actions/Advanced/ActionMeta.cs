using CFIT.AppLogger;
using PilotsDeck.Actions.Advanced.Elements;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources;
using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;


namespace PilotsDeck.Actions.Advanced
{
    public enum DISPLAY_ELEMENT
    {
        GAUGE = 1,
        IMAGE,
        PRIMITIVE,
        TEXT,
        VALUE,      
    }

    public enum ELEMENT_MANIPULATOR
    {
        COLOR = 1,
        FORMAT,
        INDICATOR,
        ROTATE,
        TRANSPARENCY,
        VISIBLE,
        SIZEPOS,
        FLASH,
    }

    public class ActionMeta : IAction
    {
        protected virtual VariableManager VariableManager { get { return App.PluginController.VariableManager; } }

        public virtual string ActionID { get { return $"{this.GetType().Name} (Title: {Title})"; } }
        public virtual string Context { get; set; }
        public virtual string Title { get; set; }
        public virtual SettingsTitle TitleSettings { get; set; }
        public virtual bool IsEncoder { get; set; }
        public virtual StreamDeckCanvasInfo CanvasInfo { get; set; }
        public virtual PointF CanvasSize { get; set; }
        public virtual SettingsModelMeta Settings { get; set; }
        public virtual bool SettingModelUpdated { get; set; } = false;

        public virtual string RenderImage64 { get; protected set; } = "";
        public virtual bool NeedRedraw { get; set; } = false;
        public virtual bool NeedRefresh { get; set; } = false;
        public virtual bool FirstLoad { get; set; } = true;

        protected virtual DateTime LastKeyDown { get; set; }
        public virtual bool HasStreamDeckInteraction { get; protected set; } = false;

        public virtual ConcurrentDictionary<int, DisplayElement> DisplayElements { get; protected set; } = [];
        public virtual ConcurrentDictionary<StreamDeckCommand, ConcurrentDictionary<int, ActionCommand>> ActionCommands { get; protected set; } = [];
        public virtual ConcurrentDictionary<StreamDeckCommand, int> ActionDelays { get; protected set; } = [];

        public static ActionMeta CreateInstance(StreamDeckEvent sdEvent)
        {
            ActionMeta instance = null;
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.meta")
                instance = new ActionMeta(sdEvent);


            return instance;
        }

        public ActionMeta(StreamDeckEvent sdEvent)
        {
            Context = sdEvent.context;
            IsEncoder = sdEvent.payload.controller == AppConfiguration.SdEncoder;
            CanvasInfo = StreamDeckCanvasInfo.GetInfo(sdEvent);
            CanvasSize = CanvasInfo.GetCanvasSize();
            SetSettingModel(sdEvent);
        }

        public virtual void SetSettingModel(StreamDeckEvent sdEvent)
        {
            DeregisterRessources();

            try
            {
                Settings = SettingsModelMeta.Create(sdEvent, out bool updated);
                
                if (updated)
                    SettingModelUpdated = true;

                if (Settings.IsNewModel)
                {
                    Settings.SetSize(CanvasSize);
                    SettingModelUpdated = true;
                }

                PointF size = Settings.GetSize();
                if (!CanvasSize.MatchesSize(size))
                {
                    PointF scale = ToolsRender.GetScale(CanvasSize, size);
                    foreach (var element in Settings.DisplayElements.Values)
                    {
                        element.SetPosition(element.GetPosition().Scale(scale));
                        element.SetSize(element.GetSize().Scale(scale));
                        element.FontSize *= scale.Y;
                        if (element.ElementType == DISPLAY_ELEMENT.GAUGE && element.Manipulators.Count > 0)
                        {
                            foreach (var manipulator in element.Manipulators.Values)
                                manipulator.IndicatorSize *= scale.Y;
                        }
                    }
                    Settings.SetSize(CanvasSize);
                    SettingModelUpdated = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Logger.Warning($"Using default SettingModel for Action {ActionID}");
                Settings = new SettingsModelMeta();
            }

            CreateDisplayElements();
            CreateCommands();

            if (Settings.IsNewModel)
            {
                Settings.IsNewModel = false;
                SettingModelUpdated = true;
            }

            RegisterRessources();
        }

        public virtual JsonNode GetSettingModel()
        {
            return Settings.Serialize();
        }

        public static int GetNextID(ICollection<int> list)
        {
            if (list.Count > 0)
                return list.Max() + 1;
            else
                return 0;
        }

        protected virtual int GetNextElementID()
        {
            return GetNextID(Settings.DisplayElements.Keys);
        }

        protected virtual int GetNextCommandID(StreamDeckCommand sdCommand)
        {
            return GetNextID(Settings.ActionCommands[sdCommand].Keys);
        }

        protected virtual void CreateDisplayElements()
        {
            DisplayElement instance;
            foreach (var elementModel in Settings.DisplayElements)
            {
                instance = DisplayElement.CreateInstance(elementModel.Value.ElementType, elementModel.Value, this);
                if (instance == null)
                {
                    Logger.Warning($"Could not create Instance for '{elementModel.Value?.ElementType}'");
                    continue;
                }

                DisplayElements.TryAdd(elementModel.Key, instance);
                if (instance.Settings.IsNewModel)
                {
                    Logger.Debug($"New Model for Instance '{elementModel.Value?.ElementType}'");
                    instance.Settings.IsNewModel = false;
                    SettingModelUpdated = true;
                }
            }
        }

        protected virtual void CreateCommands()
        {
            foreach (var type in Settings.ActionCommands)
            {
                var dict = new ConcurrentDictionary<int, ActionCommand>();
                foreach (var cmd in type.Value)
                {
                    cmd.Value.DeckCommandType = type.Key;
                    dict.TryAdd(cmd.Key, new ActionCommand(cmd.Value));
                }
                ActionCommands.TryAdd(type.Key, dict);
            }

            foreach (var type in Settings.ActionDelays)
                ActionDelays[type.Key] = type.Value;
        }

        public virtual int AddDisplayElement(DISPLAY_ELEMENT type, ModelDisplayElement model = null, string customName = null)
        {
            DisplayElement instance = DisplayElement.CreateInstance(type, model, this);
            if (!string.IsNullOrWhiteSpace(customName))
                instance.Settings.Name = customName;

            if (instance == null)
            {
                Logger.Warning($"Could not create Instance for Element '{type}'");
                return -1;
            }

            int id = GetNextElementID();
            if (!Settings.DisplayElements.TryAdd(id, instance.Settings))
            {
                Logger.Warning($"Could not add Instance '{type}' to Settings");
                return -1;
            }
            else
                Logger.Debug($"Added {instance.GetType().Name} for ID '{id}'");

            instance.Settings.IsNewModel = false;

            return id;
        }

        public virtual bool RemoveDisplayElement(int id)
        {
            if (!Settings.DisplayElements.ContainsKey(id))
                return false;

            if (!Settings.DisplayElements.Remove(id))
                return false;
            
            var elements = Settings.DisplayElements.Values.ToList();
            Settings.DisplayElements.Clear();
            int n = 0;
            foreach (var element in elements)
                Settings.DisplayElements.TryAdd(n++, element);
            
            Logger.Debug($"Removed DisplayElement for ID '{id}'");
            return true;
        }

        public virtual int AddCommand(ActionCommand command, StreamDeckCommand sdCommand, int? id = null)
        {
            id ??= GetNextCommandID(sdCommand);
            command.DeckCommandType = sdCommand;

            if (Settings.ActionCommands[sdCommand]?.TryAdd((int)id, command.Settings) == false)
            {
                Logger.Warning($"Could not add Command for '{sdCommand}' to Settings");
                return -1;
            }
            else
            {
                command.Conditions.TryAdd(0, new ConditionHandler());
                Logger.Debug($"Added {sdCommand} for ID '{id}'");
                return (int)id;
            }
        }

        public virtual bool RemoveCommand(StreamDeckCommand sdCommand, int id)
        {
            if (Settings.ActionCommands[sdCommand]?.ContainsKey(id) == true)
            {
                if (!Settings.ActionCommands[sdCommand].Remove(id))
                    return false;

                var oldDict = Settings.ActionCommands[sdCommand];
                Settings.ActionCommands[sdCommand] = [];
                int n = 0;
                foreach (var cmd in oldDict.Values)
                    Settings.ActionCommands[sdCommand].TryAdd(n++, cmd);
                
                Logger.Debug($"Removed Command for ID '{id}'");
                return true;
            }
            else
                return false;
        }

        public virtual int AddActionCondition(StreamDeckCommand type, int commandID, ConditionHandler condition, int? id = null)
        {
            id ??= GetNextID(Settings.ActionCommands[type][commandID].Conditions.Keys);
            if (!Settings.ActionCommands[type][commandID].Conditions.TryAdd((int)id, condition))
            {
                Logger.Warning($"Could not add Condition ID '{id}' to Settings");
                return -1;
            }
            else
                Logger.Debug($"Added Condition for ID '{id}'");

            return (int)id;
        }

        public virtual bool RemoveActionCondition(StreamDeckCommand type, int commandID, int conditionID)
        {
            if (Settings.ActionCommands[type][commandID]?.Conditions?.ContainsKey(conditionID) == true)
            {
                if (!Settings.ActionCommands[type][commandID].Conditions.Remove(conditionID, out _))
                    return false;                
                
                var oldDict = Settings.ActionCommands[type][commandID].Conditions;
                Settings.ActionCommands[type][commandID].Conditions = [];
                int n = 0;
                foreach (var condition in oldDict.Values)
                    Settings.ActionCommands[type][commandID].Conditions.TryAdd(n++, condition);
                
                Logger.Debug($"Removed Condition for ID '{conditionID}'");
                return true;
            }
            else
                return false;
        }

        public virtual bool SwapElement(int first, int second)
        {
            if (second >= 0 && second < Settings.DisplayElements.Count)
            {
                return Swap(Settings.DisplayElements, first, second);
            }
            else
                return false;
        }

        public virtual bool SwapManipulator(int elementID, int first, int second)
        {
            if (second >= 0 && second < Settings.DisplayElements[elementID].Manipulators.Count)
            {
                return Swap(Settings.DisplayElements[elementID].Manipulators, first, second);
            }
            else
                return false;
        }

        public virtual bool SwapManipulatorCondition(int elementID, int manipulatorID, int first, int second)
        {
            if (second >= 0 && second < Settings.DisplayElements[elementID].Manipulators[manipulatorID].Conditions.Count)
            {
                return Swap(Settings.DisplayElements[elementID].Manipulators[manipulatorID].Conditions, first, second);
            }
            else
                return false;
        }

        public virtual bool SwapActionCondition(StreamDeckCommand type, int cmdID, int first, int second)
        {
            if (second >= 0 && second < Settings.ActionCommands[type][cmdID].Conditions.Count)
            {
                return Swap(Settings.ActionCommands[type][cmdID].Conditions, first, second);
            }
            else
                return false;
        }

        public virtual bool SwapCommand(StreamDeckCommand type, int first, int second)
        {
            if (second >= 0 && second < Settings.ActionCommands[type].Count)
            {
                return Swap(Settings.ActionCommands[type], first, second);
            }
            else
                return false;
        }

        protected virtual bool Swap<E>(SortedDictionary<int, E> dict, int first, int second)
        {
            if (dict.TryGetValue(first, out E firstElement) && dict.TryGetValue(second, out E secondElement))
            {
                dict.Remove(first);
                dict.Remove(second);
                dict.Add(first, secondElement);
                dict.Add(second, firstElement);
                UpdateRessources(true);
                return true;
            }
            else
                return false;
        }

        public virtual void SetTitleParameters(string title, StreamDeckEvent.TitleParameters titleParameters)
        {
            Title = title;
            TitleSettings = new SettingsTitle(titleParameters);
            RefreshTitle();
            NeedRefresh = true;
        }

        protected virtual void RefreshTitle()
        {
            NeedRefresh = true;
        }

        public virtual void RegisterRessources()
        {
            foreach (var element in DisplayElements.Values)
                element.RegisterRessources();

            foreach (var type in ActionCommands.Values)
                foreach(var command in type.Values)
                    command.RegisterRessources();

            NeedRefresh = true;
        }

        public virtual void UpdateRessources(bool force)
        {
            DeregisterRessources();

            CreateDisplayElements();
            CreateCommands();

            RegisterRessources();

            if (SettingModelUpdated || force)
            {
                _ = App.DeckController.SendSetSettings(Context, GetSettingModel());
                SettingModelUpdated = false;
            }
        }

        public virtual void UpdateRessources()
        {
            UpdateRessources(false);
        }

        public virtual void DeregisterRessources()
        {
            foreach (var element in DisplayElements.Values)
                element.DeregisterRessources();
            DisplayElements.Clear();

            foreach (var type in ActionCommands.Values)
                foreach (var command in type.Values)
                    command.DeregisterRessources();
            ActionCommands.Clear();
        }

        protected virtual void DrawTitle(Renderer render, bool center = false)
        {
            if (IsEncoder)
            {
                var titleParam = TitleSettings ?? new SettingsTitle();
                if (titleParam.ShowTitle)
                    render.DrawEncoderTitle(Title, titleParam.GetFont(12), titleParam.GetColor(), center);
            }
        }

        public virtual void Refresh()
        {
            try
            {
                NeedRefresh = DisplayElements.Values.Where(e => e.HasChanges()).Any() || NeedRefresh;

                if (NeedRefresh)
                {
                    foreach (var element in DisplayElements.Values)
                        element.RunManipulators();

                    Renderer render = new(CanvasInfo)
                    {
                        DefaultCenter = CenterType.NONE,
                        DefaultScale = ScaleType.NONE,
                    };
                    foreach (var element in DisplayElements.Values)
                        element.RenderElement(render);

                    DrawTitle(render);

                    RenderImage64 = render.RenderImage64();
                    render.Dispose();
                    NeedRedraw = true;
                    NeedRefresh = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                NeedRedraw = false;
                NeedRefresh = false;
            }
            HasStreamDeckInteraction = false;
        }

        public virtual void ResetDrawState()
        {
            NeedRedraw = false;
        }

        public virtual SimCommand[] GetUntimedCommands(StreamDeckCommand sdCommand, int ticks)
        {
            List<SimCommand> commands = [];

            foreach (var actionCmd in ActionCommands[sdCommand].Values)
            {
                if (actionCmd.CompareConditions())
                {
                    commands.Add(actionCmd.GetSimCommand(Context, (sdCommand != StreamDeckCommand.KEY_DOWN && sdCommand != StreamDeckCommand.DIAL_DOWN), ticks,
                        (sdCommand == StreamDeckCommand.DIAL_LEFT || sdCommand == StreamDeckCommand.DIAL_RIGHT || sdCommand == StreamDeckCommand.TOUCH_TAP)));
                    if (ActionDelays[sdCommand] > 0)
                        commands.Add(new DelayCommand(ActionDelays[sdCommand]));
                }
            }

            if (commands.Count > 1 && commands[^1] is DelayCommand)
                commands.RemoveAt(commands.Count - 1);

            if (commands.Count > 0)
                return [.. commands];
            else
                return null;
        }

        public virtual SimCommand[] GetTimedCommands(TimeSpan diff, StreamDeckCommand sdCommand, int ticks)
        {
            List<ActionCommand> actionCmds = [];
            int maxTime = 0;

            foreach (var actionCmd in ActionCommands[sdCommand].Values)
            {
                if (actionCmd.CompareConditions() && actionCmd.CompareTime(diff))
                {
                    actionCmds.Add(actionCmd);
                    if (actionCmd.TimeAfterLastDown > 0 && actionCmd.TimeAfterLastDown > maxTime)
                        maxTime = actionCmd.TimeAfterLastDown;
                }
            }

            var filtered = actionCmds.Where(c => c.TimeAfterLastDown >= maxTime).ToList();
            List<SimCommand> commands = [];
            foreach (var cmd in filtered)
            {
                commands.Add(cmd.GetSimCommand(Context, true, ticks));
                if (ActionDelays[sdCommand] > 0)
                    commands.Add(new DelayCommand(ActionDelays[sdCommand]));
            }

            if (commands.Count > 1 && commands[^1] is DelayCommand)
                commands.RemoveAt(commands.Count - 1);

            if (commands.Count > 0)
                return [.. commands];
            else
                return null;
        }

        public virtual SimCommand[] OnDialDown(StreamDeckEvent sdEvent)
        {
            LastKeyDown = DateTime.Now;

            return GetUntimedCommands(StreamDeckCommand.DIAL_DOWN, 1);
        }

        public virtual SimCommand[] OnDialUp(StreamDeckEvent sdEvent)
        {
            var diff = DateTime.Now - LastKeyDown;
            LastKeyDown = DateTime.Now;
            HasStreamDeckInteraction = true;

            return GetTimedCommands(diff, StreamDeckCommand.DIAL_UP, 1);
        }

        public virtual SimCommand[] OnDialRotate(StreamDeckEvent sdEvent)
        {
            HasStreamDeckInteraction = true;
            StreamDeckCommand sdCommand = sdEvent.payload.ticks > 0 ? StreamDeckCommand.DIAL_RIGHT : StreamDeckCommand.DIAL_LEFT;

            return GetUntimedCommands(sdCommand, sdEvent.payload.ticks);
        }

        public virtual SimCommand[] OnKeyDown(StreamDeckEvent sdEvent)
        {
            LastKeyDown = DateTime.Now;

            return GetUntimedCommands(StreamDeckCommand.KEY_DOWN, 1);
        }        

        public virtual SimCommand[] OnKeyUp(StreamDeckEvent sdEvent)
        {
            var diff = DateTime.Now - LastKeyDown;
            LastKeyDown = DateTime.Now;
            HasStreamDeckInteraction = true;

            return GetTimedCommands(diff, StreamDeckCommand.KEY_UP, 1);
        }

        public virtual SimCommand[] OnTouchTap(StreamDeckEvent sdEvent)
        {
            HasStreamDeckInteraction = true;
            return GetUntimedCommands(StreamDeckCommand.TOUCH_TAP, 1);
        }
    }
}
