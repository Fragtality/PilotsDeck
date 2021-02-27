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
        protected override bool CanRedraw { get { return !string.IsNullOrEmpty(CurrentValue[0]) && !string.IsNullOrEmpty(CurrentValue[1]); } }

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
            CurrentAddress = RegisterAddress(ipcManager, Settings.AddressRadioActiv, Settings.AddressRadioStandby, CurrentAddress);
        }

        public static string[] RegisterAddress(IPCManager ipcManager, string firstAddress, string secondAddress, string[] currentAddresses)
        {
            ipcManager.RegisterAddress(firstAddress, AppSettings.groupStringRead);
            ipcManager.RegisterAddress(secondAddress, AppSettings.groupStringRead);
            currentAddresses[0] = firstAddress;
            currentAddresses[1] = secondAddress;

            return currentAddresses;
        }

        public override void UpdateAddress(IPCManager ipcManager)
        {
            CurrentAddress = UpdateAddress(ipcManager, Settings.AddressRadioActiv, Settings.AddressRadioStandby, CurrentAddress);
        }

        public static string[] UpdateAddress(IPCManager ipcManager, string firstAddress, string secondAddress, string[] currentAddresses)
        {
            currentAddresses[0] = UpdateAddress(ipcManager, currentAddresses[0], firstAddress);
            currentAddresses[1] = UpdateAddress(ipcManager, currentAddresses[1], secondAddress);

            return currentAddresses;
        }

        public override void DeregisterAddress(IPCManager ipcManager)
        {
            DeregisterAddress(ipcManager, Settings.AddressRadioActiv, Settings.AddressRadioStandby, CurrentAddress, ActionID);
        }

        public static void DeregisterAddress(IPCManager ipcManager, string firstAddress, string secondAddress, string[] currentAddresses, string actionID)
        {
            ipcManager.DeregisterValue(firstAddress);
            ipcManager.DeregisterValue(secondAddress);

            if (firstAddress != currentAddresses[0])
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {actionID} [ {firstAddress} != {currentAddresses[0]} ] ");
            if (secondAddress != currentAddresses[1])
                Log.Logger.Error($"DeregisterValue: LastAddress and Address different for {actionID} [ {secondAddress} != {currentAddresses[1]} ] ");
        }

        public override void RefreshValue(IPCManager ipcManager)
        {
            CurrentValue = RefreshValue(ipcManager, Settings.AddressRadioActiv, Settings.AddressRadioStandby, CurrentValue, out bool isChanged);
            IsChanged = isChanged;
        }

        public static string[] RefreshValue(IPCManager ipcManager, string firstAddress, string secondAddress, string[] currentValues, out bool isChanged)
        {
            int results = 0;
            if (RefreshValue(ipcManager, firstAddress, out string currentValue))
                results++;
            currentValues[0] = currentValue;

            if (RefreshValue(ipcManager, secondAddress, out currentValue))
                results++;
            currentValues[1] = currentValue;

            if (results > 0)
                isChanged = true;
            else
                isChanged = false;

            return currentValues;
        }

        //public override bool Action(IPCManager ipcManager, bool longPress)
        //{
        //    LastSwitchState = "";
        //    bool result = base.Action(ipcManager, longPress);
        //    if (result)
        //        wasPushed = true;

        //    return result;
        //}

        public override bool OnButtonUp(IPCManager ipcManager, long tick)
        {
            bool result = base.OnButtonUp(ipcManager, tick);
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
                render.DrawText(valueStb, fontStb, colorStb, ModelDisplayText.GetRectangleF(Settings.RectCoordStby));

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
