using System;
using System.Drawing;
using Serilog;

namespace PilotsDeck
{
    public class HandlerDisplayRadio : HandlerDisplaySwitch
    {
        public override ModelDisplayText TextSettings { get { return Settings; } }
        public override ModelDisplaySwitch SwitchSettings { get { return Settings; } }
        public new ModelDisplayRadio Settings { get; protected set; }

        public override string ActionID { get { return $"\"{Title}\" [HandlerDisplayRadio] Read1: {Settings.AddressRadioActiv} | Read2: {Settings.AddressRadioStandby} | Write: {SwitchSettings.AddressAction}"; } }

        protected new string[] CurrentValue { get; set; } = new string[2];
        protected virtual string[] CurrentAddress { get; set; } = new string[2];
        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue[0]) && !string.IsNullOrEmpty(CurrentValue[1]);  } }

        protected int ticksIndication = 0;
        protected static readonly int ticksActive = 16;
        protected bool wasPushed = false;

        public HandlerDisplayRadio(string context, ModelDisplayRadio settings) : base(context, settings)
        {
            Settings = settings;
            LastSwitchState = settings.OffState;
            LastSwitchStateLong = settings.OffStateLong;
        }

        protected override bool InitializationTest()
        {
            return !string.IsNullOrEmpty(SwitchSettings.AddressAction) && !string.IsNullOrEmpty(Settings.AddressRadioActiv) && !string.IsNullOrEmpty(Settings.AddressRadioStandby);
        }

        public override void RegisterAddress(IPCManager ipcManager)
        {
            ipcManager.RegisterAddress(Settings.AddressRadioActiv, AppSettings.groupStringRead);
            ipcManager.RegisterAddress(Settings.AddressRadioStandby, AppSettings.groupStringRead);
            CurrentAddress[0] = Settings.AddressRadioActiv;
            CurrentAddress[1] = Settings.AddressRadioStandby;
        }

        public override void UpdateAddress(IPCManager ipcManager)
        {
            CurrentAddress[0] = UpdateAddress(ipcManager, CurrentAddress[0], Settings.AddressRadioActiv);
            CurrentAddress[1] = UpdateAddress(ipcManager, CurrentAddress[1], Settings.AddressRadioStandby);
        }

        public override void DeregisterAddress(IPCManager ipcManager)
        {
            ipcManager.DeregisterValue(Settings.AddressRadioActiv);
            ipcManager.DeregisterValue(Settings.AddressRadioStandby);

            if (Settings.AddressRadioActiv != CurrentAddress[0])
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Settings.AddressRadioActiv} != {CurrentAddress[0]} ] ");
            if (Settings.AddressRadioStandby != CurrentAddress[1])
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {ActionID} [ {Settings.AddressRadioStandby} != {CurrentAddress[1]} ] ");
        }

        public override void RefreshValue(IPCManager ipcManager)
        {
            int results = 0;
            if (RefreshValue(ipcManager, Settings.AddressRadioActiv, out string currentValue))
                results++;
            CurrentValue[0] = currentValue;
            
            if (RefreshValue(ipcManager, Settings.AddressRadioStandby, out currentValue))
                results++;
            CurrentValue[1] = currentValue;

            if (results > 0)
                IsChanged = true;
        }

        public override bool Action(IPCManager ipcManager, bool longPress)
        {
            LastSwitchState = "";
            bool result = base.Action(ipcManager, longPress);
            if (result)
                wasPushed = true;

            return result;
        }

        protected override void Redraw(ImageManager imgManager)
        {
            if (!IsChanged && !ForceUpdate && !wasPushed)
                return;

            string valueAct = CurrentValue[0];
            if (Settings.DecodeBCD)
                valueAct = ModelDisplay.ConvertFromBCD(valueAct);
            valueAct = Settings.ScaleValue(valueAct);
            valueAct = Settings.RoundValue(valueAct);
            valueAct = Settings.FormatValue(valueAct);

            string valueStb = CurrentValue[1];
            if (Settings.DecodeBCD && !Settings.StbyHasDiffFormat || Settings.StbyHasDiffFormat && Settings.DecodeBCDStby)
                valueStb = ModelDisplay.ConvertFromBCD(valueStb);
            valueStb = ModelDisplay.ScaleValue(valueStb, Settings.StbyHasDiffFormat ? Settings.ScalarStby : Settings.Scalar);
            valueStb = ModelDisplay.RoundValue(valueStb, Settings.StbyHasDiffFormat ? Settings.FormatStby : Settings.Format);
            valueStb = ModelDisplay.FormatValue(valueStb, Settings.StbyHasDiffFormat ? Settings.FormatStby : Settings.Format);

            string background = Settings.DefaultImage;
            if (wasPushed)
            {
                ticksIndication++;
                if (ticksIndication < ticksActive)
                    background = Settings.IndicationImage;
                else
                {
                    wasPushed = false;
                    ticksIndication = 0;
                }
            }

            if (lastText != valueAct + valueStb || ForceUpdate || wasPushed && ticksIndication < ticksActive)
            {
                Font fontAct = GetFont(FontStyle.Bold);
                Font fontStb = GetFont(FontStyle.Regular);

                Color colorAct;
                if (Settings.FontInherit && TitleParameters != null)
                    colorAct = ColorTranslator.FromHtml(TitleParameters.FontColor);
                else
                    colorAct = ColorTranslator.FromHtml(Settings.FontColor);

                Color colorStb;
                if (Settings.FontInherit && TitleParameters != null)
                    colorStb = GetDarkenedColor(TitleParameters.FontColor);
                else
                    colorStb = ColorTranslator.FromHtml(Settings.FontColorStby);

                ImageRenderer render = new ImageRenderer(imgManager.GetImageObject(background));
                render.DrawText(valueAct, fontAct, colorAct, Settings.GetRectangleText());
                render.DrawText(valueStb, fontStb, colorStb, ModelDisplayText.GetRectangle(Settings.RectCoordStby));

                DrawImage = render.RenderImage64();
                IsRawImage = true;
                NeedRedraw = true;
                if (!wasPushed)
                    lastText = valueAct + valueStb;
                else
                    lastText = "";
                render.Dispose();
            }
        }

        public static Color GetDarkenedColor(string color, string subColor = "1f1f1f")
        {
            int clr = Convert.ToInt32(color.Replace("#", ""), 16);
            int sub = Convert.ToInt32(subColor, 16);

            return ColorTranslator.FromWin32(clr - sub);
        }

        private Font GetFont(FontStyle style)
        {
            if (Settings.FontInherit && TitleParameters != null)
                return new Font(TitleParameters.FontName, TitleParameters.FontSize, style);
            else
                return new Font(Settings.FontName, Settings.FontSize, style);
        }
    }
}
