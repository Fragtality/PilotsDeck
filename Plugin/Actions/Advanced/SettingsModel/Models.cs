using System.Drawing;

namespace PilotsDeck.Actions.Advanced.SettingsModel
{
    public class ColorRange
    {
        public float[] Range { get; set; } = [0, 10];
        public string Color { get; set; } = "#ff8c00";

        public ColorRange()
        {

        }

        public ColorRange(ColorRange source)
        {
            Range = [source.Range[0], source.Range[1]];
            Color = source.Color;
        }

        public ColorRange(float start, float end, Color color)
        {
            Range = [start, end];
            Color = ColorTranslator.ToHtml(color);
        }

        public Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }
    }

    public class MarkerDefinition
    {
        public float ValuePosition { get; set; } = 0;
        public float Size { get; set; } = 2;
        public string Color { get; set; } = "#ffffff";

        public MarkerDefinition()
        {

        }

        public MarkerDefinition(MarkerDefinition source)
        {
            ValuePosition = source.ValuePosition;
            Size = source.Size;
            Color = source.Color;
        }

        public MarkerDefinition(float pos, float size, Color color)
        {
            ValuePosition = pos;
            Size = size;
            Color = ColorTranslator.ToHtml(color);
        }

        public Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }
    }
}
