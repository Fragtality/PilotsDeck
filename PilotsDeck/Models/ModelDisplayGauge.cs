﻿using System;
using System.Drawing;

namespace PilotsDeck
{
    public enum GaugeOrientation
    {
        UP = -90,
        DOWN = 90,
        LEFT = 180,
        RIGHT = 0
    }

    public class ModelDisplayGauge : ModelDisplay
    {
        public virtual string MinimumValue { get; set; } = "0";
        public string MaximumValue { get; set; } = "100";
        
        public virtual string BarSize { get; set; } = "58; 10";
        public virtual int BarOrientation { get; set; } = (int)GaugeOrientation.RIGHT;
        public virtual string BarColor { get; set; } = "#006400";
        public virtual string IndicatorColor { get; set; } = "#c7c7c7";
        public virtual int IndicatorSize { get; set; } = 10;
        public virtual bool IndicatorFlip { get; set; } = false;

        public virtual bool CenterLine { get; set; } = false;
        public virtual string CenterLineColor { get; set; } = "#ffffff";
        public virtual int CenterLineThickness { get; set; } = 2;

        public virtual bool DrawWarnRange { get; set; } = false;
        public virtual bool SymmRange { get; set; } = false;
        public virtual string CriticalColor { get; set; } = "#8b0000";
        public virtual string WarnColor { get; set; } = "#ff8c00";
        public virtual string CriticalRange { get; set; } = "0; 10";
        public virtual string WarnRange { get; set; } = "11; 25";

        public virtual bool ShowText { get; set; } = true;
        public virtual bool UseWarnColors { get; set; } = true;
        public virtual bool FontInherit { get; set; } = true;
        public virtual string FontName { get; set; } = "Arial";
        public virtual int FontSize { get; set; } = 10;
        public virtual int FontStyle { get; set; } = (int)System.Drawing.FontStyle.Regular;
        public virtual string FontColor { get; set; } = "#ffffff";
        public virtual string RectCoord { get; set; } = "6; 45; 60; 21";

        public ModelDisplayGauge()
        {
            DefaultImage = @"Images\Empty.png";
            ErrorImage = @"Images\Error.png";
        }

        public virtual void GetFontParameters(StreamDeckTools.StreamDeckTitleParameters titleParameters, string value, out Font drawFont, out Color drawColor)
        {
            if (FontInherit && titleParameters != null)
            {
                drawFont = StreamDeckTools.ConvertFontParameter(titleParameters);
                drawColor = StreamDeckTools.ConvertColorParameter(titleParameters);
            }
            else
            {
                drawFont = new Font(FontName, FontSize, (FontStyle)FontStyle);
                drawColor = ColorTranslator.FromHtml(FontColor);
            }

            if (UseWarnColors)
            {
                if (ValueWithinRange(value, WarnRange))
                    drawColor = ColorTranslator.FromHtml(WarnColor);
                else if (ValueWithinRange(value, CriticalRange))
                    drawColor = ColorTranslator.FromHtml(CriticalColor);

                if (SymmRange && float.TryParse(MinimumValue, out float minimumTotal) && float.TryParse(MaximumValue, out float maximumTotal))
                {
                    float[][] ranges = GetWarnRange();

                    string rangeStr = Convert.ToString(ImageRenderer.NormalizedValue(maximumTotal, minimumTotal) - ImageRenderer.NormalizedValue(ranges[1][1], minimumTotal));
                    rangeStr += ";" + Convert.ToString(ImageRenderer.NormalizedValue(maximumTotal, minimumTotal) - ImageRenderer.NormalizedValue(ranges[1][0], minimumTotal));
                    if (ValueWithinRange(value, rangeStr))
                        drawColor = ColorTranslator.FromHtml(WarnColor);
                    else
                    {
                        rangeStr = Convert.ToString(ImageRenderer.NormalizedValue(maximumTotal, minimumTotal) - ImageRenderer.NormalizedValue(ranges[0][1], minimumTotal));
                        rangeStr += ";" + Convert.ToString(ImageRenderer.NormalizedValue(maximumTotal, minimumTotal) - ImageRenderer.NormalizedValue(ranges[0][0], minimumTotal));
                        if (ValueWithinRange(value, rangeStr))
                            drawColor = ColorTranslator.FromHtml(CriticalColor);
                    }
                }
            }
        }

        public static bool ValueWithinRange(string value, string range)
        {
            float[] rangeNum = GetNumValues(range, 0, 100);
            if (float.TryParse(value, out float valueNum))
            {
                if (rangeNum[0] <= valueNum && valueNum <= rangeNum[1])
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public RectangleF GetRectangleBar(int size = 72)
        {
            //RectangleF drawRect = new RectangleF(12, 30, 48, 12); //X= 36 - <W>/2 (64 ~ 4) //Y= 36 - <H>/2 (8 ~ 32)
            float[] barsize = GetNumValues(BarSize, 64, 8);

            return new RectangleF(size/2 - barsize[0]/2, size/2 - barsize[1] / 2, barsize[0], barsize[1]);
        }

        public Color[] GetColorRange()
        {
            return new Color[] { ColorTranslator.FromHtml(CriticalColor), ColorTranslator.FromHtml(WarnColor) };
        }

        public float[][] GetWarnRange()
        {
            return new float[][] { GetNumValues(CriticalRange, 0, 10), GetNumValues(WarnRange, 11, 25)};
        }

        public static float[] GetNumValues(string valString, float defA, float defB)
        {
            string[] parts = valString.Trim().Split(';');

            if (parts.Length != 2 || !float.TryParse(parts[0], out float a) || !float.TryParse(parts[1], out float b))
            {
                a = defA;
                b = defB;
            }

            return new float[] { a, b };
        }

        public static float GetNumValue(string valString, float def)
        {
            if (!float.TryParse(valString, out float result))
                result = def;

            return result;
        }
    }
}
