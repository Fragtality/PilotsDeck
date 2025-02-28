using CFIT.AppLogger;
using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Images;
using PilotsDeck.StreamDeck.Messages;
using System;

namespace PilotsDeck.Actions.Simple
{
    public class ActionSwitchKorry(StreamDeckEvent sdEvent) : ActionSwitch(sdEvent)
    {
        protected override void CheckSettings()
        {
            if (!Settings.HasAction)
            {
                Settings.HasAction = true;
                SettingModelUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(Settings.DefaultImage))
            {
                Settings.DefaultImage = @"Images/None.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.TopImage))
            {
                Settings.TopImage = @"Images/korry/A-FAULT.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.BotImage))
            {
                Settings.BotImage = @"Images/korry/A-ON-BLUE.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.DefaultRect))
            {
                Settings.DefaultRect = "72; 72";
                SettingModelUpdated = true;
            }

            if (Settings.SwitchOnCurrentValue)
            {
                Settings.SwitchOnCurrentValue = false;
                SettingModelUpdated = true;
            }
        }

        protected override void RegisterVariables()
        {
            base.RegisterVariables();

            if (Settings.ShowTopImage)
                RessourceStore.AddState(VariableID.Top, Settings.AddressTop, Settings.TopState, "");
            if (Settings.ShowBotImage)
                RessourceStore.AddState(VariableID.Bottom, Settings.AddressBot, Settings.BotState, "");
        }

        protected override void RegisterImages()
        {
            base.RegisterImages();

            if (Settings.UseImageMapping)
            {
                if (Settings.ShowBotImage)
                    RessourceStore.AddImageMap(ImageID.MapBot, Settings.ImageMapBot);
            }
            else
            {
                if (Settings.ShowTopImage)
                    RessourceStore.AddImage(ImageID.Top, Settings.TopImage);
                if (Settings.ShowBotImage)
                    RessourceStore.AddImage(ImageID.Bottom, Settings.BotImage);
            }
        }

        protected override void UpdateVariables()
        {
            base.UpdateVariables();

            RessourceStore.RemoveState(VariableID.Top);
            RessourceStore.RemoveState(VariableID.Bottom);

            if (Settings.ShowTopImage)
                RessourceStore.AddState(VariableID.Top, Settings.AddressTop, Settings.TopState, "");
            if (Settings.ShowBotImage)
                RessourceStore.AddState(VariableID.Bottom, Settings.AddressBot, Settings.BotState, "");
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            RessourceStore.RemoveImageMap(ImageID.MapBot);
            RessourceStore.RemoveImage(ImageID.Top);
            RessourceStore.RemoveImage(ImageID.Bottom);

            if (Settings.UseImageMapping)
            {
                if (Settings.ShowBotImage)
                    RessourceStore.AddImageMap(ImageID.MapBot, Settings.ImageMapBot);
            }
            else
            {
                if (Settings.ShowTopImage)
                    RessourceStore.AddImage(ImageID.Top, Settings.TopImage);
                if (Settings.ShowBotImage)
                    RessourceStore.AddImage(ImageID.Bottom, Settings.BotImage);
            }
        }

        protected override void DeregisterVariables()
        {
            base.DeregisterVariables();

            if (Settings.ShowTopImage)
                RessourceStore.RemoveState(VariableID.Top);
            if (Settings.ShowBotImage)
                RessourceStore.RemoveState(VariableID.Bottom);
        }

        protected override void DeregisterImages()
        {
            base.DeregisterImages();

            if (Settings.UseImageMapping)
            {
                RessourceStore.RemoveImageMap(ImageID.MapBot);
            }
            else
            {
                RessourceStore.RemoveImage(ImageID.Top);
                RessourceStore.RemoveImage(ImageID.Bottom);
            }
        }

        public override void Refresh()
        {
            try
            {
                if (!RessourceStore.HasChanges() && !NeedRefresh)
                    return;

                Renderer render = new(CanvasInfo);
                render.DrawImage(RessourceStore.GetImage(ImageID.Background), Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);
                
                string value = "";
                CenterType center = IsEncoder ? CenterType.HORIZONTAL : CenterType.NONE;
                if (Settings.ShowTopImage)
                {
                    var rect = Settings.GetRectangleTop();
                    if (IsEncoder)
                        rect.X = 0;
                    var stateTop = RessourceStore.GetState(VariableID.Top);
                    value = stateTop?.Variable?.Value ?? "";

                    ManagedImage image = null;
                    if (Settings.UseImageMapping)
                    {
                        image = RessourceStore.GetImageMap(ImageID.MapTop)?.GetMappedImage(stateTop?.Variable?.Value, null);
                    }
                    else
                    {
                        if ((!Settings.ShowTopNonZero && stateTop?.Compares() == true) || (Settings.ShowTopNonZero && stateTop?.ValueNonZero() == true) || App.PluginController.State == Plugin.PluginState.IDLE)
                            image = RessourceStore.GetImage(ImageID.Top);
                    }

                    if (image != null)
                        render.DrawImage(image, rect, center, ScaleType.DEFAULT_KEEP);
                }

                if (Settings.ShowBotImage)
                {
                    var rect = Settings.GetRectangleBot();
                    if (IsEncoder)
                        rect.X = 0;
                    var stateBot = RessourceStore.GetState(VariableID.Bottom);
                    if (string.IsNullOrWhiteSpace(value))
                        value = stateBot?.Variable?.Value ?? "";

                    ManagedImage image = null;
                    if (Settings.UseImageMapping)
                    {
                        image = RessourceStore.GetImageMap(ImageID.MapBot)?.GetMappedImage(stateBot.Variable?.Value, null);
                    }
                    else
                    {
                        if ((!Settings.ShowBotNonZero && stateBot?.Compares() == true) || (Settings.ShowBotNonZero && stateBot?.ValueNonZero() == true) || App.PluginController.State == Plugin.PluginState.IDLE)
                            image = RessourceStore.GetImage(ImageID.Bottom);
                    }

                    if (image != null)
                        render.DrawImage(image, rect, center, ScaleType.DEFAULT_KEEP);
                }

                if (Settings.HasAction && Settings.IsGuarded)
                    RenderGuard(render, value);

                DrawTitle(render);

                RenderImage64 = render.RenderImage64();
                render.Dispose();
                NeedRedraw = true;
                NeedRefresh = false;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                NeedRefresh = true;
            }
        }
    }
}
