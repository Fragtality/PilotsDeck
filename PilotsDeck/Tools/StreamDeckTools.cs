﻿using Serilog;
using StreamDeckLib.Messages;
using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace PilotsDeck
{
    public static class StreamDeckTools
    {
        public class ModelPropertyInspector
        {
            public string ImageFiles { get; set; } = "";
            public string KorryFiles { get; set; } = "";
            public string ActionTypes { get; set; } = "";
            public string GaugeOrientations { get; set; } = "";
            public bool IsEncoder { get; set; } = false;

            public ModelPropertyInspector(bool isEncoder, bool sendKorrys)
            {
                ImageFiles = String.Join("|", ReadImageDirectory().Where(f => !f.Contains(AppSettings.hqImageSuffix) && !f.Contains(AppSettings.plusImageSuffix)));
                if (sendKorrys)
                    KorryFiles = String.Join("|", ReadImageDirectory(@"korry\").Where(f => !f.Contains(AppSettings.hqImageSuffix) && !f.Contains(AppSettings.plusImageSuffix)));

                Array values = Enum.GetValues(typeof(ActionSwitchType));
                for (int i = 0; i < values.Length; i++)
                {
                    ActionTypes += (int)values.GetValue(i) + "=" + Enum.GetName(typeof(ActionSwitchType), values.GetValue(i));
                    if (i + 1 < values.Length)
                        ActionTypes += "|";
                }

                values = Enum.GetValues(typeof(GaugeOrientation));
                for (int i = 0; i < values.Length; i++)
                {
                    GaugeOrientations += (int)values.GetValue(i) + "=" + Enum.GetName(typeof(GaugeOrientation), values.GetValue(i));
                    if (i + 1 < values.Length)
                        GaugeOrientations += "|";
                }

                IsEncoder = isEncoder;
            }
        }

        public class ModelPropertyInspectorFonts : ModelPropertyInspector
        {
            public string FontNames { get; set; } = "";
            public string FontStyles { get; set; } = "";

            public ModelPropertyInspectorFonts(bool isEncoder, bool sendKorrys = false) : base(isEncoder, sendKorrys)
            {
                try
                {
                    InstalledFontCollection installedFontCollection = new();
                    string list = "";
                    foreach (var family in installedFontCollection.Families)
                    {
                        list += family.Name + "|";
                    }
                    FontNames = list.Remove(list.Length - 1, 1);

                    Array values = Enum.GetValues(typeof(FontStyle));
                    for (int i = 0; i < values.Length; i++)
                    {
                        FontStyles += (int)values.GetValue(i) + "=" + Enum.GetName(typeof(FontStyle), values.GetValue(i));
                        FontStyles += "|";
                    }
                    FontStyles += (int)(FontStyle.Bold | FontStyle.Italic) + "=" + FontStyle.Bold.ToString() + " + " + FontStyle.Italic.ToString() + "|";
                    FontStyles += (int)(FontStyle.Bold | FontStyle.Underline) + "=" + FontStyle.Bold.ToString() + " + " + FontStyle.Underline.ToString() + "|";
                    FontStyles += (int)(FontStyle.Italic | FontStyle.Underline) + "=" + FontStyle.Italic.ToString() + " + " + FontStyle.Underline.ToString() + "|";
                    FontStyles += (int)(FontStyle.Bold | FontStyle.Strikeout) + "=" + FontStyle.Bold.ToString() + " + " + FontStyle.Strikeout.ToString() + "|";
                    FontStyles += (int)(FontStyle.Italic | FontStyle.Strikeout) + "=" + FontStyle.Italic.ToString() + " + " + FontStyle.Strikeout.ToString();
                }
                catch
                {
                    Log.Logger.Error("ModelPropertyInspectorFonts: Exception while loading Fonts");
                }
            }
        }

        public static string[] ReadImageDirectory(string dirPostfix = "")
        {
            try
            {
                string[] images = Directory.GetFiles(@"Images\" + dirPostfix);
                for (int i=0; i < images.Length; i++)
                    images[i] = images[i].Replace("\\", "/");
  
                return images;
            }
            catch
            {
                Log.Logger.Error("ReadImageDirectory: Exception while loading ImageFiles");
            }

            return Array.Empty<string>();
        }

        public class StreamDeckTitleParameters
        {
            public string FontName { get; set; }
            public int FontSize { get; set; }
            public int FontStyle { get; set; }
            public string FontColor { get; set; }

            public Font GetFont(int size = -1)
            {
                if (size == -1)
                    size = FontSize;
                return new Font(FontName, size, (FontStyle)FontStyle);
            }

            public Color GetColor()
            {
                return ColorTranslator.FromHtml(FontColor);
            }
        }

        public static string TitleLog(string title)
        {
            return title.Replace("\t","").Replace("\r","").Replace("\n", "");
        }

        public static StreamDeckTitleParameters ConvertTitleParameter(StreamDeckEventPayload.TitleParameters titleParameters)
        {
            StreamDeckTitleParameters result = new()
            {
                FontName = (titleParameters.fontFamily != "" ? titleParameters.fontFamily : "Arial"),
                FontSize = titleParameters.fontSize,
                FontColor = titleParameters.titleColor
            };

            if (titleParameters.fontFamily != "")
            {
                result.FontStyle = (int)FontStyle.Regular;
                if (titleParameters.fontStyle.Contains(AppSettings.fontBold))
                    result.FontStyle |= (int)FontStyle.Bold;
                if (titleParameters.fontStyle.Contains(AppSettings.fontItalic))
                    result.FontStyle |= (int)FontStyle.Italic;
            }
            else
                result.FontStyle = (int)FontStyle.Bold;

            if (titleParameters.fontUnderline)
                result.FontStyle |= (int)FontStyle.Underline;

            return result;
        }

        public static Font ConvertFontParameter(StreamDeckTitleParameters titleParameters)
        {
            return new Font(titleParameters.FontName, titleParameters.FontSize, (FontStyle)titleParameters.FontStyle); //GraphicsUnit.Point ?
        }

        public static Color ConvertColorParameter(StreamDeckTitleParameters titleParameters)
        {
            return ColorTranslator.FromHtml(titleParameters.FontColor);
        }
    }
}
