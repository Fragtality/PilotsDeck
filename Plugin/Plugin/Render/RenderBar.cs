using CFIT.AppTools;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Actions.Simple;
using System.Drawing;

namespace PilotsDeck.Plugin.Render
{
    public class RenderBar
    {
        public Renderer Renderer { get; protected set; }
        protected Graphics Render { get { return Renderer.Render; } }
        public PointF DeviceCanvas { get; set; }
        public PointF DefaultScalar { get; set; }
        public bool IsSquareCanvas { get; set; }
        public PointF Offset { get; set; } = new PointF(0, 0);
        public float DynamicWidth { get; set; } = -1;
        public float Width { get; set; } = 58;
        public float Height { get; set; } = 10;
        public bool FixedRanges { get; set; } = true;
        public bool FixedMarkers { get; set; } = true;
        public float MinimumValue { get; set; } = 0;
        public float MaximumValue { get; set; } = 100;
        public float Value { get; set; } = 0;
        public float NormalizedRatio { get { return ToolsRender.NormalizedRatio(Value, MinimumValue, MaximumValue); } }
        public RectangleF DrawRect { get { return GetRectangle(DeviceCanvas, DefaultScalar); } }

        public float GetIndicatorSize(float size)
        {
            return (size * (IsSquareCanvas ? DefaultScalar.X : Renderer.NON_SQUARE_SCALE)) / 2.0f;
        }

        public RectangleF GetRectangle(PointF buttonSize, PointF sizeScalar)
        {
            var rect = new RectangleF(buttonSize.X / 2.0f - Width * sizeScalar.X / 2.0f, buttonSize.Y / 2.0f - Height * sizeScalar.Y / 2.0f, (DynamicWidth >= 0 ? DynamicWidth : Width) * sizeScalar.X, Height * sizeScalar.Y);
            rect.X += Offset.X * sizeScalar.X;
            rect.Y += Offset.Y * sizeScalar.Y;
            return rect;
        }

        public RenderBar(SettingsModelSimple settings, float value, Renderer render)
        {
            Renderer = render;
            float[] barsize = Conversion.ToFloatArray(settings.GaugeSize, [58, 10]);
            float[] offset = Conversion.ToFloatArray(settings.Offset, [0, 0]);
            Width = barsize[0];
            Height = barsize[1];
            Offset = new PointF(offset[0], offset[1]);
            DeviceCanvas = render.DeviceCanvas;
            DefaultScalar = render.DefaultScalar;
            IsSquareCanvas = render.IsSquareCanvas;
            Value = value;
            MinimumValue = Conversion.ToFloat(settings.MinimumValue, 0);
            MaximumValue = Conversion.ToFloat(settings.MaximumValue, 100);
        }

        public RenderBar(ModelDisplayElement settings, float value, Renderer render)
        {
            Renderer = render;
            Width = settings.Size[0];
            Height = settings.Size[1];
            Offset = new(settings.Position[0], settings.Position[1]);
            DeviceCanvas = render.DeviceCanvas;
            DefaultScalar = new(1, 1);
            IsSquareCanvas = true;
            FixedRanges = settings.GaugeFixedRanges;
            FixedMarkers = settings.GaugeFixedMarkers;
            Value = value;
            MinimumValue = settings.GaugeValueMin;
            MaximumValue = settings.GaugeValueMax;
        }

        protected int GetAlpha()
        {
            return Renderer.GetAlpha();
        }

        public void DrawBar(Color mainColor)
        {
            if (Width <= 0)
                return;

            SolidBrush brush = new(Color.FromArgb(GetAlpha(), mainColor));
            Render.FillRectangle(brush, DrawRect);

            brush.Dispose();
        }

        public void DrawBarLine(Color color, float size, float height, float offset, float ratio)
        {
            RectangleF drawParams;
            if (DynamicWidth >= 0)
            {
                float temp = DynamicWidth;
                DynamicWidth = -1;
                drawParams = DrawRect;
                DynamicWidth = temp;
            }
            else
                drawParams = DrawRect;

            float off = drawParams.Width * ratio;

            if (height <= 0)
                height = drawParams.Height;
            if (height <= 0)
                return;

            if (!FixedMarkers && DynamicWidth >= 0 && off > DynamicWidth)
                return;

            if (off < 0 || off > drawParams.Width)
                return;

            Pen pen = new(Color.FromArgb(GetAlpha(), color), size * (IsSquareCanvas ? DefaultScalar.Y : Renderer.NON_SQUARE_SCALE));
            Render.DrawLine(pen, drawParams.X + off, drawParams.Y + offset, drawParams.X + off, drawParams.Y + height);
            pen.Dispose();
        }

        public void DrawBarCenterLine(Color centerColor, float centerSize)
        {
            DrawBarLine(centerColor, centerSize, 0, 0, 0.5f);
        }

        public void DrawBarIndicatorTriangle(Color drawColor, float size, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            size = GetIndicatorSize(size);
            RectangleF drawParams = DrawRect;
            float indX = (drawParams.X + (NormalizedRatio * drawParams.Width));
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y) + offset;
            float top = (bottom ? size * -1.0f : size);
            PointF[] triangle = [new(indX - size, indY - top), new(indX + size, indY - top), new(indX, indY + top)];

            SolidBrush brush = new(Color.FromArgb(GetAlpha(), drawColor));
            Render.FillPolygon(brush, triangle);
            brush.Dispose();
        }

        public void DrawBarIndicatorImage(Image image, float size, float offset, bool flip)
        {
            if (MaximumValue == 0.0f)
                return;

            size = GetIndicatorSize(size);
            RectangleF drawParams = DrawRect;
            float indX = (drawParams.X + (NormalizedRatio * drawParams.Width));
            float indY = drawParams.Y;

            float sizeHalf = (size / 2.0f);
            if (flip)
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            drawParams = new(indX - sizeHalf, indY - sizeHalf + offset, size, size);
            Renderer.DrawImage(image, drawParams);
            if (flip)
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
        }

        public void DrawBarIndicatorCirle(Color drawColor, float size, float lineSize, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            size = GetIndicatorSize(size);
            RectangleF drawParams = DrawRect;
            float indX = (drawParams.X + (NormalizedRatio * drawParams.Width));
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y);

            float sizeHalf = (size / 2.0f);
            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), lineSize);
            Render.DrawArc(pen, indX - sizeHalf, indY - sizeHalf + offset, size, size, 0, 360);
            pen.Dispose();
        }

        public void DrawBarIndicatorLine(Color drawColor, float size, float lineSize, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            size = GetIndicatorSize(size);
            RectangleF drawParams = DrawRect;
            float indX = (drawParams.X + (NormalizedRatio * drawParams.Width));
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y);

            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), lineSize);
            Render.DrawLine(pen, new PointF(indX, indY + offset), new PointF(indX, indY + size + offset));
            pen.Dispose();
        }

        public void DrawBarIndicatorFullCircle(Color drawColor, float size, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            size = GetIndicatorSize(size);
            RectangleF drawParams = DrawRect;
            float indX = (drawParams.X + (NormalizedRatio * drawParams.Width));
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y);

            float sizeHalf = (size / 2.0f);
            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), size);
            Render.DrawArc(pen, indX - sizeHalf, indY - sizeHalf + offset, size, size, 0, 360);
            pen.Dispose();
        }

        public void DrawBarRanges(Color[] colors, float[][] ranges, bool symm = false)
        {
            if (MaximumValue == 0.0f)
                return;

            RectangleF drawParams;

            if (DynamicWidth >= 0)
            {
                float temp = DynamicWidth;
                DynamicWidth = -1;
                drawParams = DrawRect;
                DynamicWidth = temp;
            }
            else
                drawParams = DrawRect;

            float rangeStart, rangeWidth;
            float fix = 0.5f;
            
            for (int i = 0; i < ranges.Length; i++)
            {
                rangeStart = ToolsRender.NormalizedRatio(ranges[i][0], MinimumValue, MaximumValue) * drawParams.Width;
                if (!FixedRanges && DynamicWidth >= 0 && rangeStart > DynamicWidth)
                    continue;
                rangeWidth = ToolsRender.NormalizedDiffRatio(ranges[i][1], ranges[i][0], MinimumValue, MaximumValue) * drawParams.Width;
                if (!FixedRanges && DynamicWidth >= 0 && rangeStart + rangeWidth > DynamicWidth)
                    rangeWidth *= ToolsRender.NormalizedRatio(Value, ranges[i][0], ranges[i][1]);

                SolidBrush brush = new(Color.FromArgb(GetAlpha(), colors[i]));
                Render.FillRectangle(brush, drawParams.X + rangeStart, drawParams.Y, rangeWidth + fix, drawParams.Height);

                if (symm)
                    Render.FillRectangle(brush, (drawParams.X + ToolsRender.NormalizedDiffRatio(MaximumValue, ranges[i][1], MinimumValue, MaximumValue) * drawParams.Width), drawParams.Y, rangeWidth + fix, drawParams.Height);

                brush.Dispose();
            }
        }
    }
}
