using PilotsDeck.Plugin.Render;
using PilotsDeck.Resources.Images;
using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Drawing;

namespace PilotsDeck.Actions.Simple
{
    public class ActionDisplayRadio(StreamDeckEvent sdEvent) : ActionBaseSimple(sdEvent)
    {
        public override string ActionID { get { return $"{this.GetType().Name} (Title: {Title} | ActiveValue: {Settings.Address})"; } }
        protected DateTime IndicationEnd { get; set; } = DateTime.MaxValue;
        protected bool IndicationActive { get; set; } = false;

        protected override void CheckSettings()
        {
            if (!Settings.HasAction)
            {
                Settings.HasAction = true;
                SettingModelUpdated = true;
            }

            if (Settings.HasIndication)
            {
                Settings.HasIndication = false;
                SettingModelUpdated = true;
            }

            if (Settings.SwitchOnCurrentValue)
            {
                Settings.SwitchOnCurrentValue = false;
                SettingModelUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(Settings.DefaultImage))
            {
                Settings.DefaultImage = @"Images/Arrow.png";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.IndicationImage))
            {
                Settings.IndicationImage = @"Images/ArrowBright.png";
                Settings.RectCoord = "3; 1; 64; 32";
                Settings.RectCoordStby = "3; 42; 64; 31";
                SettingModelUpdated = true;
            }
            if (string.IsNullOrWhiteSpace(Settings.DefaultRect))
            {
                Settings.DefaultRect = "0; 0";
                SettingModelUpdated = true;
            }
        }

        protected override void RegisterVariables()
        {
            base.RegisterVariables();

            RessourceStore.AddState(VariableID.Active, Settings.AddressRadioActiv, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);
            if (!Settings.StbyHasDiffFormat)
                RessourceStore.AddState(VariableID.Standby, Settings.AddressRadioStandby, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);
            else
                RessourceStore.AddState(VariableID.Standby, Settings.AddressRadioStandby, "", Settings.DecodeBCDStby, Settings.ScalarStby, Settings.FormatStby);
        }

        protected override void RegisterImages()
        {
            base.RegisterImages();

            RessourceStore.AddImage(ImageID.Indication, Settings.IndicationImage);
        }

        protected override void UpdateVariables()
        {
            base.UpdateVariables();

            RessourceStore.UpdateState(VariableID.Active, Settings.AddressRadioActiv, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);
            if (!Settings.StbyHasDiffFormat)
                RessourceStore.UpdateState(VariableID.Standby, Settings.AddressRadioStandby, "", Settings.DecodeBCD, Settings.Scalar, Settings.Format);
            else
                RessourceStore.UpdateState(VariableID.Standby, Settings.AddressRadioStandby, "", Settings.DecodeBCDStby, Settings.ScalarStby, Settings.FormatStby);
        }

        protected override void UpdateImages()
        {
            base.UpdateImages();

            RessourceStore.UpdateImage(ImageID.Indication, Settings.IndicationImage);
        }

        protected override void DeregisterVariables()
        {
            base.DeregisterVariables();

            RessourceStore.RemoveState(VariableID.Active);
            RessourceStore.RemoveState(VariableID.Standby);
        }

        protected override void DeregisterImages()
        {
            base.DeregisterImages();

            RessourceStore.RemoveImage(ImageID.Indication);
        }

        protected void SetIndication()
        {
            IndicationActive = true;
            NeedRefresh = true;
            IndicationEnd = DateTime.Now + Settings.IndicationSpan;
        }

        public override SimCommand[] OnKeyUp(StreamDeckEvent sdEvent)
        {
            SetIndication();

            return base.OnKeyUp(sdEvent);
        }

        public override void Refresh()
        {
            try
            {
                if (!RessourceStore.HasChanges() && !NeedRefresh && IndicationEnd >= DateTime.Now)
                    return;

                if (IndicationEnd < DateTime.Now)
                {
                    IndicationActive = false;
                    IndicationEnd = DateTime.MaxValue;
                }

                string textAct = RessourceStore.GetState(VariableID.Active)?.FormattedValue ?? "";
                string textStby = RessourceStore.GetState(VariableID.Standby)?.FormattedValue ?? "";
                if (string.IsNullOrWhiteSpace(textAct) && App.PluginController.State == Plugin.PluginState.IDLE)
                    textAct = "0";
                if (string.IsNullOrWhiteSpace(textStby) && App.PluginController.State == Plugin.PluginState.IDLE)
                    textStby = "0";

                if (RessourceStore.GetState(VariableID.Active)?.Variable?.IsChanged == true && !IndicationActive && !NeedRefresh)
                    SetIndication();
                
                ManagedImage background = (IndicationActive ? RessourceStore.GetImage(ImageID.Indication) : RessourceStore.GetImage(ImageID.Background));
                Font fontAct = GetFont(FontStyle.Bold);
                Font fontStb = GetFont(FontStyle.Regular);
                Color colorAct = GetFontColor();
                Color colorStb = GetStandbyColor();

                Renderer render = new(CanvasInfo);
                render.DrawImage(background, Settings.GetRectangleBackground(), CenterType.BOTH, ScaleType.DEVICE_STRETCH);

                render.DrawText(textAct, fontAct, colorAct, Settings.GetRectangleActive(), CenterType.NONE, ScaleType.DEFAULT_STRETCH);
                render.DrawText(textStby, fontStb, colorStb, Settings.GetRectangleStandby(), CenterType.NONE, ScaleType.DEFAULT_STRETCH);

                if (Settings.HasAction && Settings.IsGuarded)
                    RenderGuard(render, null);

                DrawTitle(render, true);

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

        public Color GetStandbyColor(string subColor = "1f1f1f")
        {
            if (Settings.FontInherit)
            {
                int clr = Convert.ToInt32(TitleSettings.FontColor.Replace("#", ""), 16);
                int sub = Convert.ToInt32(subColor, 16);

                return ColorTranslator.FromWin32(clr - sub);
            }
            else
                return GetFontColor(Settings.FontColorStby);
        }
    }
}
