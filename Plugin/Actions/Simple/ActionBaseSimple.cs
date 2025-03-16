using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Images;
using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Drawing;
using System.Text.Json.Nodes;

namespace PilotsDeck.Actions.Simple
{
    public abstract class ActionBaseSimple : IAction
    {
        protected virtual DateTime KeyDown { get; set; }
        protected virtual bool GuardHoldDown { get; set; } = false;

        public abstract string ActionID { get; }
        public virtual string Address { get { return Settings.Address; } }
        public virtual string Context { get; set; }
        public virtual string Title { get; set; }
        public virtual SettingsTitle TitleSettings { get; set; }
        public virtual bool IsEncoder { get; protected set; }
        public virtual StreamDeckCanvasInfo CanvasInfo { get; set; }
        public virtual SettingsModelSimple Settings { get; set; }
        public virtual bool SettingModelUpdated { get; set; } = false;
        public virtual RessourceStore RessourceStore { get; } = new();

        public virtual string RenderImage64 { get; protected set; } = "";
        public virtual bool NeedRedraw { get; set; } = false;
        public virtual bool NeedRefresh { get; set; } = true;
        public virtual bool FirstLoad { get; set; } = true;

        protected static ImageManager ImageManager { get { return App.PluginController.ImageManager; } }

        public ActionBaseSimple(StreamDeckEvent sdEvent)
        {
            Context = sdEvent.context;
            IsEncoder = sdEvent.payload.controller == AppConfiguration.SdEncoder;
            Settings = SettingsModelSimple.Create(sdEvent);
            CanvasInfo = StreamDeckCanvasInfo.GetInfo(sdEvent);
            CheckVersion();
            CheckSettings();
            CleanCommands();
            if (string.IsNullOrWhiteSpace(Settings.DefaultRect))
            {
                Settings.DefaultRect = "0; 0";
                SettingModelUpdated = true;
            }
        }

        public static ActionBaseSimple CreateInstance(StreamDeckEvent sdEvent)
        {
            ActionBaseSimple instance = null;

            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.display")
                instance = new ActionDisplayText(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.display.switch")
                instance = new ActionDisplaySwitch(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.switch")
                instance = new ActionSwitch(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.switch.display")
                instance = new ActionSwitchDisplay(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.switch.korry")
                instance = new ActionSwitchKorry(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.display.radio")
                instance = new ActionDisplayRadio(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.display.gauge")
                instance = new ActionDisplayGauge(sdEvent);
            if (sdEvent.action == $"{AppConfiguration.PluginUUID.ToLower()}.action.display.gauge.dual")
                instance = new ActionDisplayGaugeDual(sdEvent);

            if (instance != null && sdEvent.payload.settings.ToString() == "{}")
                instance.SettingModelUpdated = true;

            if (instance != null && instance.IsEncoder != instance.Settings.IsEncoder)
            {
                instance.Settings.IsEncoder = instance.IsEncoder;
                instance.SettingModelUpdated = true;
            }

            instance?.RegisterRessources();

            return instance;
        }

        public virtual void SetSettingModel(StreamDeckEvent sdEvent)
        {
            Settings = SettingsModelSimple.Create(sdEvent);

            UpdateRessources();
            CheckVersion();
            CheckSettings();
            CleanCommands();
        }

        protected virtual void CheckSettings()
        {

        }

        protected virtual void CheckVersion()
        {
            if (Settings.BUILD_VERSION < AppConfiguration.BuildModelVersion)
            {
                if (Settings.BUILD_VERSION < 4)
                {
                    bool topEmpty = string.IsNullOrWhiteSpace(Settings.AddressTop);
                    bool botEmpty = string.IsNullOrWhiteSpace(Settings.AddressBot);
                    if (topEmpty && !botEmpty && Settings.ShowTopImage)
                    {
                        Settings.ShowTopImage = false;
                        Logger.Debug($"Changed 'ShowTopImage' to false");
                    }

                    if (!topEmpty && botEmpty && Settings.ShowBotImage)
                    {
                        Settings.ShowBotImage = false;
                        Logger.Debug($"Changed 'ShowBotImage' to false");
                    }

                    var oldSize = Conversion.ToFloat(Settings.FontSize);
                    var newSize = AppConfiguration.FontSizeConversionLegacy(oldSize);
                    Settings.FontSize = Conversion.ToString(newSize);
                    Logger.Debug($"Changed 'FontSize' {oldSize} => {Settings.FontSize}");
                }

                if (Settings.BUILD_VERSION >= 4 && Settings.BUILD_VERSION < 7)
                {
                    var oldSize = Conversion.ToFloat(Settings.FontSize);
                    var newSize = AppConfiguration.FontSizeConversionModern(oldSize);
                    Settings.FontSize = Conversion.ToString(newSize);
                    Logger.Debug($"Changed 'FontSize' {oldSize} => {Settings.FontSize}");
                }

                if (Settings.BUILD_VERSION < 8)
                {
                    Settings.UseXpCommandOnce = true;
                }

                Logger.Information($"Converted Settings for '{ActionID}' from Version {Settings.BUILD_VERSION} to {AppConfiguration.BuildModelVersion}");
                Settings.BUILD_VERSION = AppConfiguration.BuildModelVersion;
                SettingModelUpdated = true;
            }

            if (Settings.IsNewModel)
            {
                Settings.IsNewModel = false;
                SettingModelUpdated = true;
            }
        }

        public virtual void CleanCommands()
        {
            if (App.Configuration.CleanInactiveCommands)
            {
                if (!Settings.HasAction)
                {
                    Settings.AddressAction = "";
                    Settings.AddressActionLong = "";
                    Settings.AddressActionOff = "";
                    Settings.AddressActionGuard = "";
                    Settings.AddressActionGuardOff = "";
                    Settings.AddressActionLeft = "";
                    Settings.AddressActionRight = "";
                    Settings.AddressActionTouch = "";

                    Settings.ActionType = SimCommandType.LVAR;
                    Settings.ActionTypeLong = SimCommandType.LVAR;
                    Settings.ActionTypeGuard = SimCommandType.LVAR;
                    Settings.ActionTypeLeft = SimCommandType.LVAR;
                    Settings.ActionTypeRight = SimCommandType.LVAR;
                    Settings.ActionTypeTouch = SimCommandType.LVAR;
                }

                if (!Settings.HasLongPress)
                {
                    Settings.AddressActionLong = "";
                    Settings.ActionTypeLong = SimCommandType.LVAR;
                }

                if (!Settings.IsGuarded)
                {
                    Settings.AddressActionGuard = "";
                    Settings.AddressActionGuardOff = "";
                    Settings.ActionTypeGuard = SimCommandType.LVAR;
                }

                if (!Settings.IsEncoder)
                {
                    Settings.AddressActionLeft = "";
                    Settings.AddressActionRight = "";
                    Settings.AddressActionTouch = "";
                    Settings.ActionTypeLeft = SimCommandType.LVAR;
                    Settings.ActionTypeRight = SimCommandType.LVAR;
                    Settings.ActionTypeTouch = SimCommandType.LVAR;
                }

                SettingModelUpdated = true;
            }
        }

        public virtual JsonNode GetSettingModel()
        {
            return Settings.Serialize();
        }

        public virtual void SetTitleParameters(string title, StreamDeckEvent.TitleParameters titleParameters)
        {
            Title = title;
            TitleSettings = new SettingsTitle(titleParameters);
            RefreshTitle();
            NeedRefresh = true;
        }

        public virtual void RegisterRessources()
        {
            RegisterVariables();
            RegisterCommands();
            RegisterImages();

            NeedRefresh = true;
        }

        protected virtual void RegisterVariables()
        {
            
        }

        protected virtual void RegisterCommands()
        {
            if (!Settings.HasAction)
                return;

            RessourceStore.AddCommand(SwitchID.Switch, Settings);

            if (Settings.HasLongPress)
                RessourceStore.AddCommand(SwitchID.SwitchLong, Settings);

            if (IsEncoder)
            {
                RessourceStore.AddCommand(SwitchID.SwitchLeft, Settings);
                RessourceStore.AddCommand(SwitchID.SwitchRight, Settings);
                RessourceStore.AddCommand(SwitchID.SwitchTouch, Settings);
            }

            if (Settings.IsGuarded)
                RessourceStore.AddCommand(SwitchID.GuardCmd, Settings);
        }

        protected virtual void RegisterImages()
        {
            RessourceStore.AddImage(ImageID.Background, Settings.DefaultImage);

            if (Settings.UseImageMapping)
                RessourceStore.AddImageMap(ImageID.Map, Settings.ImageMap);

            if (Settings.HasAction && Settings.IsGuarded)
            {
                if (Settings.UseImageGuardMapping)
                    RessourceStore.AddImageMap(ImageID.MapGuard, Settings.ImageGuardMap);
                else
                    RessourceStore.AddImage(ImageID.Guard, Settings.ImageGuard);
            }
        }

        public virtual void UpdateRessources()
        {
            UpdateVariables();
            UpdateCommands();
            UpdateImages();
            Logger.Verbose("Updated Ressources");
            NeedRefresh = true;
        }

        protected virtual void UpdateVariables()
        {

        }

        protected virtual void UpdateCommands()
        {
            RessourceStore.UpdateCommand(SwitchID.Switch, Settings);

            RessourceStore.UpdateCommand(SwitchID.SwitchLong, Settings);

            if (IsEncoder)
            {
                RessourceStore.UpdateCommand(SwitchID.SwitchLeft, Settings);
                RessourceStore.UpdateCommand(SwitchID.SwitchRight, Settings);
                RessourceStore.UpdateCommand(SwitchID.SwitchTouch, Settings);
            }

            RessourceStore.UpdateCommand(SwitchID.GuardCmd, Settings);
        }

        protected virtual void UpdateImages()
        {
            RessourceStore.UpdateImage(ImageID.Background, Settings.DefaultImage);

            RessourceStore.UpdateImageMap(ImageID.Map, Settings.UseImageMapping, Settings.ImageMap);

            if (Settings.HasAction)
            {
                if (Settings.IsGuarded)
                {
                    RessourceStore.UpdateImageMap(ImageID.MapGuard, Settings.UseImageGuardMapping, Settings.ImageGuardMap);
                    if (Settings.UseImageGuardMapping)
                        RessourceStore.RemoveImage(ImageID.Guard);
                    else
                        RessourceStore.UpdateImage(ImageID.Guard, Settings.ImageGuard);
                }
                else
                {
                    if (Settings.UseImageGuardMapping)
                        RessourceStore.RemoveImageMap(ImageID.MapGuard);
                    else
                        RessourceStore.RemoveImage(ImageID.Guard);
                }
            }
        }

        public virtual void DeregisterRessources()
        {
            DeregisterVariables();
            DeregisterCommands();
            DeregisterImages();
        }

        protected virtual void DeregisterVariables()
        {
            
        }

        protected virtual void DeregisterCommands()
        {
            RessourceStore.RemoveCommand(SwitchID.Switch);

            if (Settings.HasLongPress)
                RessourceStore.RemoveCommand(SwitchID.SwitchLong);

            if (IsEncoder)
            {
                RessourceStore.RemoveCommand(SwitchID.SwitchLeft);
                RessourceStore.RemoveCommand(SwitchID.SwitchRight);
                RessourceStore.RemoveCommand(SwitchID.SwitchTouch);
            }

            if (Settings.IsGuarded)
                RessourceStore.RemoveCommand(SwitchID.GuardCmd);
        }

        protected virtual void DeregisterImages()
        {
            RessourceStore.RemoveImage(ImageID.Background);
            RessourceStore.RemoveImageMap(ImageID.Map);

            if (Settings.HasAction && Settings.IsGuarded)
            {
                if (Settings.UseImageGuardMapping)
                    RessourceStore.RemoveImageMap(ImageID.MapGuard);
                else
                    RessourceStore.RemoveImage(ImageID.Guard);
            }
        }

        protected virtual void RefreshTitle()
        {
            NeedRefresh = true;
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

        protected virtual void RenderGuard(Renderer render, string currentControlValue)
        {
            if (Settings.IsGuarded && RessourceStore.GetState(VariableID.GuardMon)?.Compares() == true)
            {
                currentControlValue ??= RessourceStore.GetVariable(VariableID.GuardMon)?.Value ?? "";

                ManagedImage guardImage;
                if (Settings.UseImageGuardMapping)
                    guardImage = RessourceStore.GetImageMap(ImageID.MapGuard)?.GetMappedImage(currentControlValue, null);
                else
                    guardImage = RessourceStore.GetImage(ImageID.Guard);

                if (guardImage != null)
                    render.DrawImage(guardImage, Settings.GetRectangleGuard(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);
            }
        }

        public virtual void Refresh()
        {

        }

        public virtual void ResetDrawState()
        {
            NeedRedraw = false;
        }

        public virtual Font GetFont(FontStyle? style = null)
        {
            if (Settings.FontInherit && TitleSettings != null)
                return TitleSettings.GetFont(-1, style);
            else
                return new Font(Settings.FontName, Conversion.ToFloat(Settings.FontSize, 10), (style ?? Settings.FontStyle));
        }

        public virtual Color GetFontColor(string color = null)
        {
            if (Settings.FontInherit && TitleSettings != null)
                return TitleSettings.GetColor();
            else if (color != null)
                return ColorTranslator.FromHtml(color);
            else
                return ColorTranslator.FromHtml(Settings.FontColor);
        }

        public virtual SimCommand[] OnTouchTap(StreamDeckEvent sdEvent)
        {
            SimCommand simCommand = null;

            if (Settings.IsGuarded && RessourceStore.GetState(VariableID.GuardMon)?.Compares() == true && RessourceStore.GetCommand(SwitchID.GuardCmd, out ActionCommand guardCommand) != null)
            {
                simCommand = guardCommand?.GetSimCommand(Context, sdEvent.payload.ticks, true);
            }
            else if (RessourceStore.GetCommand(SwitchID.SwitchTouch, out ActionCommand touchCommand) != null)
            {
                simCommand = touchCommand?.GetSimCommand(Context, sdEvent.payload.ticks, true);
            }

            if (simCommand == null)
            {
                Logger.Warning($"Could not build SimCommand for Action '{Title}' ({GetType()?.Name})");
                _ = App.DeckController.SendShowAlert(Context);
                return null;
            }
            else
                return [simCommand];
        }

        public virtual SimCommand[] OnDialDown(StreamDeckEvent sdEvent)
        {
            return OnKeyDown(sdEvent);
        }

        public virtual SimCommand[] OnDialUp(StreamDeckEvent sdEvent)
        {
            return OnKeyUp(sdEvent);
        }

        public virtual SimCommand[] OnDialRotate(StreamDeckEvent sdEvent)
        {
            SimCommand simCommand = null;

            if (Settings.IsGuarded && RessourceStore.GetState(VariableID.GuardMon)?.Compares() == true)
            {
                simCommand = null;
            }
            else if (sdEvent.payload.ticks < 0 && RessourceStore.GetCommand(SwitchID.SwitchLeft, out ActionCommand leftCommand) != null)
            {
                simCommand = leftCommand?.GetSimCommand(Context, sdEvent.payload.ticks, true);
            }
            else if (sdEvent.payload.ticks > 0 && RessourceStore.GetCommand(SwitchID.SwitchRight, out ActionCommand rightCommand) != null)
            {
                simCommand = rightCommand?.GetSimCommand(Context, sdEvent.payload.ticks, true);
            }

            if (simCommand == null)
            {
                Logger.Warning($"Could not build SimCommand for Action '{Title}' ({GetType()?.Name})");
                _ = App.DeckController.SendShowAlert(Context);
                return null;
            }
            else
                return [simCommand];
        }

        public virtual SimCommand[] OnKeyDown(StreamDeckEvent sdEvent)
        {
            KeyDown = DateTime.Now;
            SimCommand simCommand = null;

            if (Settings.IsGuarded && RessourceStore.GetState(VariableID.GuardMon)?.Compares() == true
                && RessourceStore.GetCommand(SwitchID.GuardCmd, out ActionCommand guardCommand)?.IsHoldable == true)
            {
                GuardHoldDown = true;
                simCommand = guardCommand?.GetSimCommand(Context, sdEvent.payload.ticks, false, false);
            }
            else if (RessourceStore.GetCommand(SwitchID.Switch, out ActionCommand switchCommand)?.IsHoldable == true)
            {
                simCommand = switchCommand?.GetSimCommand(Context, sdEvent.payload.ticks, false, false);
            }

            if (simCommand == null)
                return null;
            else
                return [simCommand];
        }

        public virtual SimCommand[] OnKeyUp(StreamDeckEvent sdEvent)
        {
            SimCommand simCommand = null;
            ActionCommand switchCommand = RessourceStore.GetCommand(SwitchID.Switch);
            bool longPress = (DateTime.Now - KeyDown) >= RessourceStore.GetCommand(SwitchID.SwitchLong, out ActionCommand longCommand)?.LongPressTime;
            Logger.Debug($"Key TimeDiff: {DateTime.Now - KeyDown}");
            bool hasLong = longCommand != null;

            if (Settings.IsGuarded && (GuardHoldDown || RessourceStore.GetState(VariableID.GuardMon)?.Compares() == true) && RessourceStore.GetCommand(SwitchID.GuardCmd, out ActionCommand guardCommand) != null)
            {
                Logger.Debug($"Build Guard Command");
                simCommand = guardCommand?.GetSimCommand(Context, sdEvent.payload.ticks);
            }
            else if (hasLong && longPress && switchCommand?.IsHoldable == false)
            {
                Logger.Debug($"Build Long Command");
                simCommand = longCommand?.GetSimCommand(Context, sdEvent.payload.ticks);
            }
            else if (switchCommand != null)
            {
                Logger.Debug($"Build Command");
                simCommand = switchCommand?.GetSimCommand(Context, sdEvent.payload.ticks);
            }

            GuardHoldDown = false;

            if (simCommand == null)
            {
                Logger.Warning($"Could not build SimCommand for Action '{Title}' ({GetType()?.Name})");
                _ = App.DeckController.SendShowAlert(Context);
                return null;
            }
            else
                return [simCommand];
        }
    }
}
