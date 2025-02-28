using CFIT.AppTools;
using PilotsDeck.Actions.Advanced.SettingsModel;
using PilotsDeck.Actions.Simple;
using System.Drawing;

namespace PilotsDeck.Plugin.Render
{
    public class RenderArc
    {
        public Renderer Renderer { get; protected set; }
        protected Graphics Render { get { return Renderer.Render; } }
        public PointF DeviceCanvas { get; set; }
        public PointF DefaultScalar { get; set; }
        public bool IsSquareCanvas { get; set; }
        public float Radius { get; set; } = 48;
        public float Width { get; set; } = 6;
        public PointF Offset { get; set; } = new PointF(0, 0);
        public float StartAngle { get; set; } = 135;
        public float SweepAngle { get; set; } = 180;
        public bool FixedRanges { get; set; } = true;
        public bool FixedMarkers { get; set; } = true;
        public float MinimumValue { get; set; } = 0;
        public float MaximumValue { get; set; } = 100;
        public float Value { get; set; } = 0;
        public float NormalizedRatio { get { return ToolsRender.NormalizedRatio(Value, MinimumValue, MaximumValue); } }
        public RectangleF DrawRect { get { return GetRectangle(DeviceCanvas.Center(), DefaultScalar, IsSquareCanvas); } }
        public float Angle { get { return (NormalizedRatio * SweepAngle) + StartAngle; } }

        public float GetIndicatorSize(float size)
        {
            return (size * (IsSquareCanvas ? DefaultScalar.Y : Renderer.NON_SQUARE_SCALE)) / 2.0f;
        }

        protected int GetAlpha()
        {
            return Renderer.GetAlpha();
        }

        public RectangleF GetRectangle(PointF center, PointF sizeScalar, bool isSquare)
        {
            PointF pos = new(0, 0);
            PointF scale = sizeScalar;
            if (!isSquare)
            {
                scale = new PointF(Renderer.NON_SQUARE_SCALE, Renderer.NON_SQUARE_SCALE);
            }

            pos.X = center.X + Offset.X - Radius * scale.X / 2.0f;
            pos.Y = center.Y + Offset.Y - Radius * scale.Y / 2.0f;

            return new RectangleF(pos.X, pos.Y, Radius * scale.X, Radius * scale.Y);
        }

        public RenderArc(SettingsModelSimple settings, float value, Renderer render)
        {
            Renderer = render;
            float[] arcsize = Conversion.ToFloatArray(settings.GaugeSize, [48, 6]);
            float[] offset = Conversion.ToFloatArray(settings.Offset, [0, 0]);
            Radius = arcsize[0];
            Width = arcsize[1];
            Offset = new PointF(offset[0], offset[1]);
            StartAngle = Conversion.ToFloat(settings.StartAngle, 135);
            SweepAngle = Conversion.ToFloat(settings.SweepAngle, 180);
            DeviceCanvas = render.DeviceCanvas;
            DefaultScalar = render.DefaultScalar;
            IsSquareCanvas = render.IsSquareCanvas;
            Value = value;
            MinimumValue = Conversion.ToFloat(settings.MinimumValue, 0);
            MaximumValue = Conversion.ToFloat(settings.MaximumValue, 100);
        }

        public RenderArc(ModelDisplayElement settings, float value, Renderer render)
        {
            Renderer = render;
            Radius = settings.Size[0];
            Width = settings.Size[1];
            Offset = new (settings.Position[0], settings.Position[1]);
            StartAngle = settings.GaugeAngleStart;
            SweepAngle = settings.GaugeAngleSweep;
            DeviceCanvas = render.DeviceCanvas;
            DefaultScalar = new(1, 1);
            IsSquareCanvas = true;
            FixedRanges = settings.GaugeFixedRanges;
            FixedMarkers = settings.GaugeFixedMarkers;
            Value = value;
            MinimumValue = settings.GaugeValueMin;
            MaximumValue = settings.GaugeValueMax;
        }

        public void DrawArc(Color drawColor)
        {
            if (Width <= 0)
                return;

            float scale = (IsSquareCanvas ? DefaultScalar.X : Renderer.NON_SQUARE_SCALE);
            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), Width * scale);
            Render.DrawArc(pen, DrawRect, StartAngle, SweepAngle);
            pen.Dispose();
        }

        public void DrawArcIndicatorTriangle(Color drawColor, float size, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            float angle = Angle;

            size = GetIndicatorSize(size);
            float orgIndX = (DrawRect.X + DrawRect.Width + (bottom ? -size : size)) + offset;
            float orgIndY = (DrawRect.Y + DrawRect.Width / 2.0f);
            float top = bottom ? -size : size;

            PointF[] triangle = [   new PointF(orgIndX - top, orgIndY),
                                    new PointF(orgIndX + top, orgIndY + size),
                                    new PointF(orgIndX + top, orgIndY - size) ];

            SolidBrush brush = new(Color.FromArgb(GetAlpha(), drawColor));
            Renderer.RotateCenter(angle, Offset);
            Render.FillPolygon(brush, triangle);
            Renderer.RotateCenter(-angle, Offset);
            brush.Dispose();
        }

        public void DrawArcIndicatorImage(Image image, float size, float offset, bool flip)
        {
            if (MaximumValue == 0.0f)
                return;

            RectangleF drawRect = DrawRect;
            float angle = Angle;

            float orgIndX = drawRect.X + drawRect.Width;
            float orgIndY = drawRect.Y + drawRect.Width - (Radius / 2.0f) - (size / 2.0f);

            Renderer.RotateCenter(angle, Offset);
            if (flip)
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            drawRect = new(orgIndX + offset, orgIndY, size, size);
            Renderer.DrawImage(image, drawRect);
            image.RotateFlip(RotateFlipType.Rotate270FlipNone);
            if (flip)
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Renderer.RotateCenter(-angle, Offset);
            
        }

        public void DrawArcIndicatorCircle(Color drawColor, float size, float lineSize, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            RectangleF drawRect = DrawRect;
            float angle = Angle;

            size = GetIndicatorSize(size);
            float orgIndX = drawRect.X + drawRect.Width + (bottom ? -size : size);
            float orgIndY = drawRect.Y + drawRect.Width / 2.0f;

            float sizeHalf = (size / 2.0f);
            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), lineSize);
            Renderer.RotateCenter(angle, Offset);
            Render.DrawArc(pen, orgIndX - sizeHalf + offset, orgIndY - sizeHalf, size, size, 0, 360);
            Renderer.RotateCenter(-angle, Offset);
            pen.Dispose();
        }

        public void DrawArcIndicatorLine(Color drawColor, float size, float lineSize, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            RectangleF drawRect = DrawRect;
            float angle = Angle;

            size = GetIndicatorSize(size);
            float orgIndX = drawRect.X + drawRect.Width + (bottom ? -size : size);
            float orgIndY = drawRect.Y + drawRect.Width / 2.0f;

            float sizeHalf = (size / 2.0f);
            var pointStart = new PointF(orgIndX - sizeHalf + offset, orgIndY);
            var pointEnd = new PointF(orgIndX - sizeHalf + offset, orgIndY + size).RotatePoint(pointStart, 90);
            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), lineSize);
            Renderer.RotateCenter(angle, Offset);
            Render.DrawLine(pen, pointStart, pointEnd);
            Renderer.RotateCenter(-angle, Offset);
            pen.Dispose();
        }

        public void DrawArcIndicatorFullCircle(Color drawColor, float size, float offset, bool bottom = false)
        {
            if (MaximumValue == 0.0f)
                return;

            RectangleF drawRect = DrawRect;
            float angle = Angle;

            size = GetIndicatorSize(size);
            float orgIndX = drawRect.X + drawRect.Width + (bottom ? -size : size);
            float orgIndY = drawRect.Y + drawRect.Width / 2.0f;

            float sizeHalf = (size / 2.0f);
            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), size);
            Renderer.RotateCenter(angle, Offset);
            Render.DrawArc(pen, orgIndX - sizeHalf + offset, orgIndY - sizeHalf, size, size, 0, 360);
            Renderer.RotateCenter(-angle, Offset);
            pen.Dispose();
        }

        public void DrawArcLine(MarkerDefinition marker)
        {
            if (!FixedMarkers && marker.ValuePosition > Value)
                return;

            if (marker.ValuePosition < MinimumValue || marker.ValuePosition > MaximumValue)
                return;

            DrawArcLine(marker.GetColor(), marker.Size, marker.Height, marker.Offset, ToolsRender.NormalizedRatio(marker.ValuePosition, MinimumValue, MaximumValue));
        }

        public void DrawArcLine(Color drawColor, float size, float height, float offset, float ratio)
        {
            if (Radius == 0.0f)
                return;

            RectangleF drawRect = DrawRect;
            float orgIndX = drawRect.X + drawRect.Width + offset;
            float orgIndY = (drawRect.Y + drawRect.Width / 2.0f);
            float angle = (SweepAngle * ratio) + StartAngle;
            if (height <= 0)
                height = Width;
            if (height <= 0)
                return;

            Pen pen = new(Color.FromArgb(GetAlpha(), drawColor), GetIndicatorSize(size));
            Renderer.RotateCenter(angle, Offset);
            Render.DrawLine(pen, orgIndX - (height * (IsSquareCanvas ? DefaultScalar.X : Renderer.NON_SQUARE_SCALE) * 0.5f), orgIndY, orgIndX + (height * (IsSquareCanvas ? DefaultScalar.X : Renderer.NON_SQUARE_SCALE) * 0.5f), orgIndY);
            Renderer.RotateCenter(-angle, Offset);
            pen.Dispose();
        }

        public void DrawArcCenterLine(Color drawColor, float size)
        {
            DrawArcLine(drawColor, size, Width, 0, 0.5f);
        }

        public void DrawArcRanges(Color[] colors, float[][] ranges, bool symm = false)
        {
            if (MaximumValue == 0.0f || Radius == 0.0f)
                return;

            RectangleF drawRect = DrawRect;
            float rangeAngleStart;
            float rangeAngleSweep;
            float ratioSweep;
            float fix =  1.1f;
            if (SweepAngle < 0)
                fix *= -1;

            for (int i = 0; i < ranges.Length; i++)
            {
                rangeAngleStart = ToolsRender.NormalizedRatio(ranges[i][0], MinimumValue, MaximumValue) * SweepAngle;
                ratioSweep = ToolsRender.NormalizedDiffRatio(ranges[i][1], ranges[i][0], MinimumValue, MaximumValue);
                rangeAngleSweep = ratioSweep * SweepAngle;
                if (!FixedRanges && Value < ranges[i][0])
                    continue;
                if (!FixedRanges && Value < ranges[i][1])
                    rangeAngleSweep *= ToolsRender.NormalizedRatio(Value, ranges[i][0], ranges[i][1]);

                Pen pen = new(Color.FromArgb(GetAlpha(), colors[i]), Width * (IsSquareCanvas ? DefaultScalar.Y : Renderer.NON_SQUARE_SCALE));
                Render.DrawArc(pen, drawRect, StartAngle + rangeAngleStart - fix, rangeAngleSweep + fix);

                if (symm)
                {
                    rangeAngleStart = ToolsRender.NormalizedDiffRatio(MaximumValue, ranges[i][1], MinimumValue, MaximumValue) * SweepAngle;
                    Render.DrawArc(pen, drawRect, StartAngle + rangeAngleStart - fix, rangeAngleSweep + fix);
                }

                pen.Dispose();
            }
        }
    }
}
