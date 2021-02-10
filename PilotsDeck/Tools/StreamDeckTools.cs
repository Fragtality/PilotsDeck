using System;
using System.Drawing;
using System.Drawing.Text;
using StreamDeckLib.Messages;
using System.IO;
using Serilog;

namespace PilotsDeck
{
    public static class StreamDeckTools
    {
        public class ModelPropertyInspector
        {
            public string ImageFiles { get; set; } = "";
            public string ActionTypes { get; set; } = "";

            public ModelPropertyInspector()
            {
                ImageFiles = String.Join("|", ReadImageDirectory());

                Array values = Enum.GetValues(typeof(ActionSwitchType));
                for (int i = 0; i < values.Length; i++)
                {
                    ActionTypes += (int)values.GetValue(i) + "=" + Enum.GetName(typeof(ActionSwitchType), values.GetValue(i));
                    if (i + 1 < values.Length)
                        ActionTypes += "|";
                }
            }
        }

        public class ModelPropertyInspectorFonts : ModelPropertyInspector
        {
            public string FontNames { get; set; } = "";
            public string FontStyles { get; set; } = "";

            public ModelPropertyInspectorFonts() : base()
            {
                try
                {
                    InstalledFontCollection installedFontCollection = new InstalledFontCollection();
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

        public static string[] ReadImageDirectory()
        {
            try
            {
                string[] images = Directory.GetFiles(@"Images\");
                for (int i=0; i < images.Length; i++)
                    images[i] = images[i].Replace("\\", "/");
                return images;
            }
            catch
            {
                Log.Logger.Error("ReadImageDirectory: Exception while loading ImageFiles");
            }

            return new string[0];
        }

        public static string ReadImageBase64(string imageLocation)
        {
            return Convert.ToBase64String(File.ReadAllBytes(imageLocation), Base64FormattingOptions.None);
        }

        public static string ToImageBase64(byte[] image)
        {
            return Convert.ToBase64String(image, Base64FormattingOptions.None);
        }

        public static byte[] ReadImageBytes(string image)
        {
            return File.ReadAllBytes(image);
        }

        public class StreamDeckTitleParameters
        {
            public string FontName { get; set; }
            public int FontSize { get; set; }
            public int FontStyle { get; set; }
            public string FontColor { get; set; }
        }

        public static StreamDeckTitleParameters ConvertFontParameter(StreamDeckEventPayload.TitleParameters titleParameters)
        {
            StreamDeckTitleParameters result = new StreamDeckTitleParameters()
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
    }
}
