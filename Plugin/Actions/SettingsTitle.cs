using PilotsDeck.StreamDeck.Messages;
using System.Drawing;

namespace PilotsDeck.Actions
{
    public class SettingsTitle
    {
        public string FontName { get; set; }
        public int FontSize { get; set; }
        public FontStyle FontStyle { get; set; }
        public string FontColor { get; set; }
        public bool ShowTitle { get; set; } = true;

        public Font GetFont(int size = -1, FontStyle? style = null)
        {
            if (size == -1)
                size = FontSize;
            style ??= FontStyle;
            return new Font(FontName, size, (FontStyle)style);
        }

        public Color GetColor()
        {
            return ColorTranslator.FromHtml(FontColor);
        }

        public SettingsTitle()
        {

        }

        public SettingsTitle(StreamDeckEvent.TitleParameters titleParameters)
        {
            FontName = titleParameters.fontFamily != "" ? titleParameters.fontFamily : "Arial";
            FontSize = titleParameters.fontSize;
            FontColor = titleParameters.titleColor;
            ShowTitle = titleParameters.showTitle;

            if (titleParameters.fontFamily != "")
            {
                FontStyle = FontStyle.Regular;
                if (titleParameters.fontStyle.Contains(App.Configuration.GetFontBoldName()))
                    FontStyle |= FontStyle.Bold;
                if (titleParameters.fontStyle.Contains(App.Configuration.GetFontItalicName()))
                    FontStyle |= FontStyle.Italic;
            }
            else
                FontStyle = FontStyle.Bold;

            if (titleParameters.fontUnderline)
                FontStyle |= FontStyle.Underline;
        }
    }
}
