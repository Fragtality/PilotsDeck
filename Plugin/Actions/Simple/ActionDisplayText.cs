using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Images;
using PilotsDeck.StreamDeck.Messages;
using PilotsDeck.Tools;
using System;
using System.Drawing;

namespace PilotsDeck.Actions.Simple
{
    public class ActionDisplayText(StreamDeckEvent sdEvent) : ActionBaseSimple(sdEvent)
    {
        public override string ActionID { get { return $"{this.GetType().Name} (Title: {Title} | ReadValue: {Address})"; } }
        protected bool DrawBoxLast { get; set; }

        protected override void CheckSettings()
        {
            DrawBoxLast = Settings.DrawBox;

            if (Settings.HasAction)
            {
                Settings.HasAction = false;
                SettingModelUpdated = true;
            }

            if (Settings.SwitchOnCurrentValue)
            {
                Settings.SwitchOnCurrentValue = false;
                SettingModelUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(Settings.DefaultImage))
            {
                Settings.DefaultImage = @"Images/Empty.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.IndicationImage))
            {
                Settings.IndicationImage = @"Images/Empty.png";
                SettingModelUpdated = true;
            }
        }

        protected override void RegisterVariables()
        {
            base.RegisterVariables();

            RessourceStore.AddState(VariableID.Control, Settings.Address, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);

            if (Settings.HasIndication)
                RessourceStore.AddState(VariableID.Indication, Settings.Address, Settings.IndicationValue, Settings.DecodeBCD, Settings.Scalar, Settings.Format);
        }

        protected override void RegisterImages()
        {
            base.RegisterImages();

            if (Settings.HasIndication)
            {
                if (Settings.UseImageMapping)
                    RessourceStore.AddImageMap(ImageID.Indication, Settings.ImageMap);
                else
                    RessourceStore.AddImage(ImageID.Indication, Settings.IndicationImage);
            }
        }

        public override void UpdateRessources()
        {
            base.UpdateRessources();

            if (DrawBoxLast != Settings.DrawBox)
            {
                Settings.ResetRectText();
                DrawBoxLast = Settings.DrawBox;
                SettingModelUpdated = true;
            }
        }

        protected override void UpdateVariables()
        {
            base.UpdateVariables();

            RessourceStore.UpdateState(VariableID.Control, Settings.Address, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);

            if (Settings.HasIndication)
                RessourceStore.UpdateState(VariableID.Indication, Settings.Address, Settings.IndicationValue, Settings.DecodeBCD, Settings.Scalar, Settings.Format);
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            if (Settings.HasIndication)
            {
                RessourceStore.RemoveImageMap(ImageID.Indication);
                RessourceStore.RemoveImage(ImageID.Indication);

                if (Settings.UseImageMapping)
                    RessourceStore.AddImageMap(ImageID.Indication, Settings.ImageMap);
                else
                    RessourceStore.AddImage(ImageID.Indication, Settings.IndicationImage);
            }
            else
            {
                RessourceStore.RemoveImageMap(ImageID.Indication);
                RessourceStore.RemoveImage(ImageID.Indication);
            }
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

            if (Settings.HasIndication)
            {
                if (Settings.UseImageMapping)
                    RessourceStore.RemoveImageMap(ImageID.Indication);
                else
                    RessourceStore.RemoveImage(ImageID.Indication);
            }
        }

        public override void Refresh()
        {
            try
            {
                if (!RessourceStore.HasChanges() && !NeedRefresh)
                    return;

                string value = RessourceStore.GetState(VariableID.Control)?.StringValue ?? "0";
                string text = RessourceStore.GetState(VariableID.Control)?.FormattedValue ?? "0";

                ManagedImage indImage = null;
                Color drawColor = GetFontColor();
                Color boxColor = Settings.GetBoxColor();

                if (Settings.HasIndication)
                {
                    bool valueCompares = RessourceStore.GetState(VariableID.Indication)?.Compares() ?? false;

                    if (Settings.IndicationHideValue && valueCompares)
                        text = "";

                    if (Settings.IndicationUseColor && valueCompares)
                    {
                        drawColor = ColorTranslator.FromHtml(Settings.IndicationColor);
                        boxColor = ColorTranslator.FromHtml(Settings.IndicationColor);
                    }

                    if (!Settings.UseImageMapping && valueCompares)
                        indImage = RessourceStore.GetImage(ImageID.Indication);
                    else if (Settings.UseImageMapping)
                        indImage = RessourceStore.GetImageMap(ImageID.Indication)?.GetMappedImage(value, Settings.IndicationImage);
                }
                text = ValueState.GetValueMapped(text, Settings.ValueMappings);

                ManagedImage image = RessourceStore.GetImage(ImageID.Background);
                Renderer render = new(CanvasInfo);
                render.DrawImage(image, Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);

                if (indImage != null)
                    render.DrawImage(indImage, Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEFAULT_KEEP);

                if (Settings.DrawBox)
                    render.DrawRectangle(boxColor, Conversion.ToFloat(Settings.BoxSize, 2), Settings.GetRectangleBox(), CenterType.NONE, ScaleType.DEFAULT_STRETCH);

                if (text != "")
                    render.DrawText(text, GetFont(), drawColor, Settings.GetRectangleText(), CenterType.NONE, ScaleType.DEFAULT_STRETCH);

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
