using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources;
using PilotsDeck.Resources.Images;
using PilotsDeck.StreamDeck.Messages;
using System;

namespace PilotsDeck.Actions.Simple
{
    public class ActionSwitchDisplay(StreamDeckEvent sdEvent) : ActionSwitch(sdEvent)
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
            if (string.IsNullOrWhiteSpace(Settings.IndicationImage))
            {
                Settings.IndicationImage = @"Images/Fault.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.OnImage))
            {
                Settings.OnImage = @"Images/KorryOnBlueTop.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.OffImage))
            {
                Settings.OffImage = @"Images/KorryOffWhiteBottom.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.DefaultRect))
            {
                Settings.DefaultRect = "72; 72";
                SettingModelUpdated = true;
            }

            if (Settings.SwitchOnCurrentValue)
            {
                Logger.Information($"Converting Setting 'SwitchOnCurrentValue'");
                Settings.SwitchOnState = Settings.OnState;
                Settings.SwitchOffState = Settings.OffState;

                Settings.SwitchOnCurrentValue = false;
                SettingModelUpdated = true;
            }
        }

        protected override void RegisterVariables()
        {
            base.RegisterVariables();

            RessourceStore.AddState(VariableID.Control, Settings.Address, Settings.OnState, Settings.OffState);

            if (Settings.HasIndication)
                RessourceStore.AddState(VariableID.Indication, Settings.Address, Settings.IndicationValue, "");
        }

        protected override void RegisterImages()
        {
            base.RegisterImages();

            if (!Settings.UseImageMapping)
            {
                RessourceStore.AddImage(ImageID.On, Settings.OnImage);
                RessourceStore.AddImage(ImageID.Off, Settings.OffImage);
            }

            if (Settings.HasIndication)
                RessourceStore.AddImage(ImageID.Indication, Settings.IndicationImage);
        }

        protected override void UpdateVariables()
        {
            base.UpdateVariables();

            RessourceStore.UpdateState(VariableID.Control, Settings.Address, Settings.OnState, Settings.OffState);

            if (Settings.HasIndication)
                RessourceStore.UpdateState(VariableID.Indication, Settings.Address, Settings.IndicationValue, "");
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            RessourceStore.RemoveImage(ImageID.On);
            RessourceStore.RemoveImage(ImageID.Off);

            if (!Settings.UseImageMapping)
            {
                
                RessourceStore.AddImage(ImageID.On, Settings.OnImage);
                RessourceStore.AddImage(ImageID.Off, Settings.OffImage);
            }

            if (Settings.HasIndication)
                RessourceStore.UpdateImage(ImageID.Indication, Settings.IndicationImage);
        }

        protected override void DeregisterVariables()
        {
            base.DeregisterVariables();

            RessourceStore.RemoveState(VariableID.Control);

            if (Settings.HasIndication)
                RessourceStore.RemoveState(VariableID.Indication);
        }

        protected override void DeregisterImages()
        {
            base.DeregisterImages();

            if (!Settings.UseImageMapping)
            {
                RessourceStore.RemoveImage(ImageID.On);
                RessourceStore.RemoveImage(ImageID.Off);
            }

            if (Settings.HasIndication)
                RessourceStore.RemoveImage(ImageID.Indication);
        }

        public override void Refresh()
        {
            try
            {
                if (!RessourceStore.HasChanges() && !NeedRefresh)
                    return;
                
                ManagedImage newImage = null;
                var state = RessourceStore.GetState(VariableID.Control);
                if (!Settings.UseImageMapping)
                {
                    if (state?.Compares() == true)
                        newImage = RessourceStore.GetImage(ImageID.On);
                    else if (state?.ComparesOff() == true)
                        newImage = RessourceStore.GetImage(ImageID.Off);
                    else if (Settings.HasIndication && (RessourceStore.GetState(VariableID.Indication)?.Compares() == true || Settings.IndicationValueAny))
                        newImage = RessourceStore.GetImage(ImageID.Indication);
                }
                else
                {
                    newImage = RessourceStore.GetImageMap(ImageID.Map)?.GetMappedImage(state.Variable?.Value, null) ?? ImageManager.DEFAULT_WAIT;
                }

                Renderer render = new(CanvasInfo);
                render.DrawImage(RessourceStore.GetImage(ImageID.Background), Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);

                if (newImage != null)
                    render.DrawImage(newImage, Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);

                if (Settings.HasAction && Settings.IsGuarded)
                    RenderGuard(render, state?.Variable?.Value ?? "");

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
