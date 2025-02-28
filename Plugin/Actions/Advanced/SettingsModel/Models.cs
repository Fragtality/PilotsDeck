using CFIT.AppTools;
using System.Drawing;
using System.Text.Json.Serialization;

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

        public ColorRange(float start, float end, string color)
        {
            Range = [start, end];
            Color = color;
        }

        public Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }

        public override string ToString()
        {
            return $"Range: {Conversion.ToString(Range[0])} => {Conversion.ToString(Range[1])}";
        }
    }

    public interface IMarkerDefinition
    {
        public string Position { get; }
        public float Size { get; set; }
        public float Height { get; set; }
        public float Offset { get; set; }
        public string Color { get; set; }

        public Color GetColor();
        public void SetColor(Color color);
        public string ToString();
    }

    public class MarkerDefinition : IMarkerDefinition
    {
        public float ValuePosition { get; set; } = 0;
        [JsonIgnore]
        public string Position => Conversion.ToString(ValuePosition);
        public float Size { get; set; } = 2;
        public float Height { get; set; } = 0;
        public float Offset { get; set; } = 0;
        public string Color { get; set; } = "#ffffff";

        public MarkerDefinition() { }

        public MarkerDefinition(float pos, float size, float height, float offset, string color)
        {
            ValuePosition = pos;
            Size = size;
            Height = height;
            Offset = offset;
            Color = color;
        }

        public MarkerDefinition(float pos, float size, float height, float offset, Color color)
                         : this(pos, size, height, offset, ColorTranslator.ToHtml(color)) { }

        public MarkerDefinition(MarkerDefinition source)
        {
            ValuePosition = source.ValuePosition;
            Size = source.Size;
            Height = source.Height;
            Offset = source.Offset;
            Color = source.Color;
        }

        public virtual Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public virtual void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }

        public override string ToString()
        {
            return $"Value: {Position} / Thickness: {Conversion.ToString(Size)} / Height: {Conversion.ToString(Height)} / Offset: {Conversion.ToString(Offset)}";
        }
    }

    public class MarkerRangeDefinition : IMarkerDefinition
    {
        public float Start { get; set; } = 0;
        public float Stop { get; set; } = 0;
        public float Step { get; set; } = 0;
        [JsonIgnore]
        public string Position => GetRangeDefinition(Start, Stop, Step);
        public float Size { get; set; } = 2;
        public float Height { get; set; } = 0;
        public float Offset { get; set; } = 0;
        public string Color { get; set; } = "#ffffff";

        public MarkerRangeDefinition() { }

        public MarkerRangeDefinition(float start, float stop, float step, float size, float height, float offset, string color)
        {
            Start = start;
            Stop = stop;
            Step = step;
            Size = size;
            Height = height;
            Offset = offset;
            Color = color;
        }

        public MarkerRangeDefinition(float start, float stop, float step, float size, float height, float offset, Color color) 
                             : this (start, stop, step, size, height, offset, ColorTranslator.ToHtml(color)) { }

        public MarkerRangeDefinition(MarkerRangeDefinition source)
        {
            Start = source.Start;
            Stop = source.Stop;
            Step = source.Step;
            Size = source.Size;
            Height = source.Height;
            Offset = source.Offset;
            Color = source.Color;
        }

        public static string GetRangeDefinition(float start, float stop, float step)
        {
            return $"${Conversion.ToString(start)}:{Conversion.ToString(stop)}:{Conversion.ToString(step)}";
        }

        public static bool GetRangeDefinition(object value, out float start, out float stop, out float step)
        {
            step = 1;
            start = 0;
            stop = 10;

            if (value is not string text || string.IsNullOrWhiteSpace(text) || !text.StartsWith('$') || text.Length < 6)
                return false;
            
            string[] parts = text[1..].Split(':');
            if (parts.Length != 3)
                return false;

            if (!Conversion.IsNumberF(parts[0], out start) || !Conversion.IsNumberF(parts[1], out stop) || !Conversion.IsNumberF(parts[2], out step))
                return false;

            return true;
        }

        public virtual Color GetColor()
        {
            return ColorTranslator.FromHtml(Color);
        }

        public virtual void SetColor(Color color)
        {
            Color = ColorTranslator.ToHtml(color);
        }

        public override string ToString()
        {
            return $"Range: {Conversion.ToString(Start)} - {Conversion.ToString(Stop)} / Step: {Conversion.ToString(Step)} / Thickness: {Conversion.ToString(Size)} / Height: {Conversion.ToString(Height)} / Offset: {Conversion.ToString(Offset)}";
        }
    }
}
