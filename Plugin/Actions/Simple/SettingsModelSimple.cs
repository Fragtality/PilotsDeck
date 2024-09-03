using PilotsDeck.Simulator;
using PilotsDeck.StreamDeck.Messages;
using PilotsDeck.Tools;
using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace PilotsDeck.Actions.Simple
{
    public class SettingsModelSimple
    {
        public static SettingsModelSimple Create(StreamDeckEvent sdEvent)
        {
            var settings = (sdEvent?.payload?.settings?[AppConfiguration.ModelSimple]).Deserialize<SettingsModelSimple>(JsonOptions.JsonSerializerOptions);
            settings ??= new SettingsModelSimple()
            {
                BUILD_VERSION = AppConfiguration.BuildModelVersion
            };

            return settings;
        }

        public JsonNode Serialize()
        {
            return new JsonObject()
            {
                [AppConfiguration.ModelSimple] = JsonSerializer.SerializeToNode(this)
            };
        }

        public virtual void ResetRectText()
        {
            if (DrawBox)
                RectCoord = "-1; 0; 0; 0";
            else
                RectCoord = "-1; 1; 72; 72";

        }

        public virtual RectangleF GetRectangleBackground()
        {
            return GetRectangleF($"0; 0; {DefaultRect}");
        }

        public virtual RectangleF GetRectangleTop()
        {
            return GetRectangleF(TopRect);
        }

        public virtual RectangleF GetRectangleBot()
        {
            return GetRectangleF(BotRect);
        }

        public virtual RectangleF GetRectangleBox()
        {
            return GetRectangleF(BoxRect);
        }

        public virtual RectangleF GetRectangleStandby()
        {
            return GetRectangleF(RectCoordStby);
        }

        public virtual RectangleF GetRectangleActive()
        {
            return GetRectangleF(RectCoord);
        }

        public virtual RectangleF GetRectangleFirst()
        { 
            return GetRectangleF(RectCoord);
        }

        public virtual RectangleF GetRectangleSecond()
        {
            return GetRectangleF(RectCoord2);
        }

        public virtual RectangleF GetRectangleText()
        {
            if (!DrawBox)
                return GetRectangleF(RectCoord);
            else
            {
                RectangleF box = GetRectangleF(BoxRect);
                RectangleF text = GetRectangleF(RectCoord);
                float size = (float)Math.Round(Conversion.ToFloat(BoxSize, 2) / 2.0d, 0, MidpointRounding.ToEven);
                text.X = text.X + box.X + size;
                text.Y = text.Y + box.Y + size;
                text.Width = text.Width + box.Width - size * 2.0f;
                text.Height = text.Height + box.Height - size * 2.0f;

                return text;
            }
        }

        public static Rectangle GetRectangle(string rect)
        {
            return Rectangle.Round(GetRectangleF(rect));
        }

        public static RectangleF GetRectangleF(string rect)
        {
            //x y w h
            string[] parts = rect.Trim().Split(';');
            if (parts.Length == 4)
            {
                int parses = 0;
                for (int i = 0; i < parts.Length; i++)
                    if (Conversion.IsNumber(parts[i]))
                        parses++;

                if (parses == parts.Length)
                    return new RectangleF(Conversion.ToFloat(parts[0]), Conversion.ToFloat(parts[1]), Conversion.ToFloat(parts[2]), Conversion.ToFloat(parts[3]));
                else
                    return new RectangleF(11, 23, 48, 40); //-1 -1 -2 0 from actual
            }
            return new RectangleF(11, 23, 48, 40);
        }

        public virtual Color GetBoxColor()
        {
            return ColorTranslator.FromHtml(BoxColor);
        }

        public virtual int BUILD_VERSION { get; set; } = 0;
        public virtual bool IsNewModel { get; set; } = true;

        public virtual string DefaultImage { get; set; } = "";
        public virtual string DefaultRect { get; set; } = "";

        public virtual bool SwitchOnCurrentValue { get; set; } = false;
        public virtual bool IsEncoder { get; set; } = false;
        public virtual bool HasAction { get; set; } = false;

        public virtual bool UseImageMapping { get; set; } = false;
        public virtual string ImageMap { get; set; } = "";
        public virtual string Address { get; set; } = "";
        public virtual bool DrawBox { get; set; } = false;
        public virtual bool DecodeBCD { get; set; } = false;
        public virtual string Scalar { get; set; } = "1";
        public virtual string Format { get; set; } = "";

        public virtual string BoxSize { get; set; } = "2";
        public virtual string BoxColor { get; set; } = "#ffffff";
        public virtual string BoxRect { get; set; } = "9; 21; 54; 44";

        public virtual bool HasIndication { get; set; } = false;
        public virtual bool IndicationHideValue { get; set; } = false;
        public virtual bool IndicationUseColor { get; set; } = false;
        public virtual string IndicationColor { get; set; } = "#ffcc00";
        public virtual string IndicationImage { get; set; } = @"";
        public virtual string IndicationValue { get; set; } = "0";
        public virtual string ValueMappings { get; set; } = "";

        public virtual bool FontInherit { get; set; } = true;
        public virtual string FontName { get; set; } = "Arial";
        public virtual string FontSize { get; set; } = "12";
        public virtual FontStyle FontStyle { get; set; } = FontStyle.Regular;
        public virtual string FontColor { get; set; } = "#ffffff";
        public virtual string RectCoord { get; set; } = "-1; 0; 0; 0";

        public virtual string AddressAction { get; set; } = "";
        public virtual bool DoNotRequestBvar { get; set; } = true;
        public virtual string AddressMonitor { get; set; } = "";
        public virtual string AddressActionOff { get; set; } = "";
        public virtual SimCommandType ActionType { get; set; } = SimCommandType.LVAR;
        public virtual bool ToggleSwitch { get; set; } = false;
        public virtual bool HoldSwitch { get; set; } = false;
        public virtual bool UseControlDelay { get; set; } = false;
        public virtual bool UseLvarReset { get; set; } = false;
        public virtual string SwitchOnState { get; set; } = "";
        public virtual string SwitchOffState { get; set; } = "";

        public virtual bool HasLongPress { get; set; } = false;
        public virtual string AddressActionLong { get; set; } = "";
        public virtual SimCommandType ActionTypeLong { get; set; } = SimCommandType.LVAR;

        public virtual string SwitchOnStateLong { get; set; } = "";
        public virtual string SwitchOffStateLong { get; set; } = "";

        //Rotate Controls
        public virtual string AddressActionLeft { get; set; } = "";
        public virtual SimCommandType ActionTypeLeft { get; set; } = SimCommandType.LVAR;
        public virtual string SwitchOnStateLeft { get; set; } = "";
        public virtual string SwitchOffStateLeft { get; set; } = "";

        public virtual string AddressActionRight { get; set; } = "";
        public virtual SimCommandType ActionTypeRight { get; set; } = SimCommandType.LVAR;
        public virtual string SwitchOnStateRight { get; set; } = "";
        public virtual string SwitchOffStateRight { get; set; } = "";

        //Touch Control
        public virtual string AddressActionTouch { get; set; } = "";
        public virtual SimCommandType ActionTypeTouch { get; set; } = SimCommandType.LVAR;
        public virtual string SwitchOnStateTouch { get; set; } = "";
        public virtual string SwitchOffStateTouch { get; set; } = "";

        //Guarded Switch
        public virtual bool IsGuarded { get; set; } = false;
        public virtual string AddressGuardActive { get; set; } = "";
        public virtual string GuardActiveValue { get; set; } = "";
        public virtual string AddressActionGuard { get; set; } = "";
        public virtual string AddressActionGuardOff { get; set; } = "";
        public virtual SimCommandType ActionTypeGuard { get; set; } = SimCommandType.LVAR;
        public virtual string SwitchOnStateGuard { get; set; } = "";
        public virtual string SwitchOffStateGuard { get; set; } = "";
        public virtual string ImageGuard { get; set; } = "Images/GuardCross.png";
        public virtual string GuardRect { get; set; } = "0; 0; 72; 72";
        public virtual Rectangle GetRectangleGuard()
        {
            return GetRectangle(GuardRect);
        }
        public virtual bool UseImageGuardMapping { get; set; } = false;
        public virtual string ImageGuardMap { get; set; } = "";


        public virtual string OnImage { get; set; } = @"Images/KorryOnBlueTop.png";
        public virtual string OffImage { get; set; } = @"Images/KorryOffWhiteBottom.png";
        public virtual string OnState { get; set; } = "";
        public virtual string OffState { get; set; } = "";

        public virtual bool IndicationValueAny { get; set; } = false;

        public virtual string AddressTop { get; set; } = "";
        public virtual string AddressBot { get; set; } = "";
        public virtual bool UseOnlyTopAddr { get; set; } = false;
        public virtual bool ShowTopImage { get; set; } = true;
        public virtual bool ShowBotImage { get; set; } = true;

        public virtual string TopState { get; set; } = "";
        public virtual bool ShowTopNonZero { get; set; } = false;
        public virtual string BotState { get; set; } = "";
        public virtual bool ShowBotNonZero { get; set; } = false;
        public virtual string ImageMapBot { get; set; } = "";

        public virtual string TopImage { get; set; } = "Images/korry/A-FAULT.png";
        public virtual string BotImage { get; set; } = "Images/korry/A-ON-Blue.png";

        public virtual string TopRect { get; set; } = "9; 21; 54; 20";
        public virtual string BotRect { get; set; } = "9; 45; 54; 20";

        public virtual string AddressRadioActiv { get; set; } = "";
        public virtual string AddressRadioStandby { get; set; } = "";

        public virtual bool StbyHasDiffFormat { get; set; } = false;
        public virtual bool DecodeBCDStby { get; set; } = false;
        public virtual string ScalarStby { get; set; } = "1";
        public virtual string FormatStby { get; set; } = "";

        public virtual string FontColorStby { get; set; } = "#e0e0e0";
        public virtual string RectCoordStby { get; set; } = "3; 42; 64; 31";
        public virtual string IndicationTime { get; set; } = "1500";
        [JsonIgnore]
        public virtual TimeSpan IndicationSpan { get { return TimeSpan.FromMilliseconds(Conversion.ToDouble(IndicationTime, 1500)); } }

        //Gauge
        public virtual string MinimumValue { get; set; } = "0";
        public string MaximumValue { get; set; } = "100";

        public virtual string GaugeSize { get; set; } = "58; 10";
        public virtual GaugeOrientation BarOrientation { get; set; } = GaugeOrientation.RIGHT;
        public virtual string GaugeColor { get; set; } = "#006400";
        public virtual bool UseColorSwitching { get; set; } = false;
        public virtual string AddressColorOff { get; set; } = "";
        public virtual string StateColorOff { get; set; } = "";
        public virtual string GaugeColorOff { get; set; } = "#636363";

        public virtual bool DrawArc { get; set; } = false;
        public virtual string StartAngle { get; set; } = "135";
        public virtual string SweepAngle { get; set; } = "180";
        public virtual string Offset { get; set; } = "0; 0";

        public virtual string IndicatorColor { get; set; } = "#c7c7c7";
        public virtual string IndicatorSize { get; set; } = "10";
        public virtual bool IndicatorFlip { get; set; } = false;

        public virtual bool CenterLine { get; set; } = false;
        public virtual string CenterLineColor { get; set; } = "#ffffff";
        public virtual string CenterLineThickness { get; set; } = "2";

        public virtual bool DrawWarnRange { get; set; } = false;
        public virtual bool SymmRange { get; set; } = false;
        public virtual string CriticalColor { get; set; } = "#8b0000";
        public virtual string WarnColor { get; set; } = "#ff8c00";
        public virtual string CriticalRange { get; set; } = "0; 10";
        public virtual string WarnRange { get; set; } = "10; 20";

        public virtual bool ShowText { get; set; } = true;
        public virtual bool UseWarnColors { get; set; } = true;

        public string Address2 { get; set; } = "";
        public virtual string RectCoord2 { get; set; } = "6; 6; 60; 21";
    }
}
