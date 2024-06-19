using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace PilotsDeck
{
    public class Arc
    {
        public float Radius { get; set; } = 48;
        public float Width { get; set; } = 6;
        public PointF Offset { get; set; } = new PointF(0, 0);
        public float StartAngle { get; set; } = 135;
        public float SweepAngle { get; set; } = 180;

        public RectangleF GetRectangle(PointF center, PointF sizeScalar, bool isSquare)
        {
            PointF pos = new(0, 0);
            PointF scale = sizeScalar;
            if (!isSquare)
            {
                scale = new PointF(ImageRenderer.nonSquareScale, ImageRenderer.nonSquareScale);
            }

            pos.X = (center.X + Offset.X) - ((Radius * scale.X) / 2.0f);
            pos.Y = (center.Y + Offset.Y) - ((Radius * scale.Y) / 2.0f);

            return new RectangleF(pos.X, pos.Y, Radius * scale.X, Radius * scale.Y);
        }
    }

    public class Bar
    {
        public float Width { get; set; } = 58;
        public float Height { get; set; } = 10;

        public RectangleF GetRectangle(PointF buttonSize, PointF sizeScalar)
        {
            return new RectangleF((buttonSize.X / 2.0f) - (Width * sizeScalar.X / 2.0f), (buttonSize.Y / 2.0f) - (Height * sizeScalar.Y / 2.0f), Width * sizeScalar.X, Height * sizeScalar.Y);
        }
    }

    public class ImageRenderer : IDisposable
    {
        public readonly static float nonSquareScale = 1.5f;

        protected Bitmap background;
        protected Graphics render;

        protected PointF canvasSize;
        protected PointF scalar;

        public bool IsSquare { get { return canvasSize.X == canvasSize.Y; } }

        protected StringFormat stringFormat = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.FitBlackBox
        };

        public ImageRenderer(ManagedImage image, StreamDeckType type)
        {
            canvasSize = type.GetCanvasSize();
            scalar = new(canvasSize.X / 72.0f, canvasSize.Y / 72.0f);
            background = new Bitmap((int)canvasSize.X, (int)canvasSize.Y);
            render = Graphics.FromImage(background);

            render.SmoothingMode = SmoothingMode.AntiAlias;
            render.InterpolationMode = InterpolationMode.HighQualityBicubic;
            render.PixelOffsetMode = PixelOffsetMode.HighQuality;
            render.CompositingQuality = CompositingQuality.HighQuality;
            render.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            render.PageUnit = GraphicsUnit.Pixel;

            using Image tmpImage = image.GetImageObject();
            render.DrawImage(tmpImage, GetImageDrawRectangle(image));
            tmpImage.Dispose();
        }

        public PointF GetCanvasCenter()
        {
            return new PointF(canvasSize.X / 2.0f, canvasSize.Y / 2.0f);
        }

        public RectangleF ScaleRectangle(RectangleF rect)
        {
            return new RectangleF(rect.X * scalar.X, rect.Y * scalar.Y, rect.Width * scalar.X, rect.Height * scalar.Y);
        }

        public Font ScaleFont(Font font)
        {
            return new Font(font.Name, font.Size * scalar.Y, font.Style);
        }

        public RectangleF GetImageDrawRectangle(ManagedImage image)
        {
            if (image.Size.X == image.Size.Y && !IsSquare)
                return new RectangleF(canvasSize.X / 4, 0, canvasSize.Y, canvasSize.Y);
            else
                return new RectangleF(0, 0, canvasSize.X, canvasSize.Y);
        }

        public void DrawImage(ManagedImage image)
        {
            render.DrawImage(image.GetImageObject(), GetImageDrawRectangle(image));
        }

        public void DrawImage(Image image, RectangleF drawRectangle)
        {
            render.DrawImage(image, ScaleRectangle(drawRectangle));
        }

        public static bool GetColorFromString(ref string text, out Color color)
        {
            color = default;
            try
            {
                if (!string.IsNullOrWhiteSpace(text) && text.StartsWith("[[#"))
                {
                    string strColor = text[0..9].Replace("[[","");
                    color = ColorTranslator.FromHtml(strColor);
                    text = text[9..];
                    return true;
                }
            }
            catch { }

            return false;
        }

        public void DrawText(string text, Font drawFont, Color drawColor, RectangleF drawRectangle)
        {
            SolidBrush drawBrush;
            if (GetColorFromString(ref text, out Color strColor))
                drawBrush = new SolidBrush(strColor);
            else
                drawBrush = new(drawColor);

            render.DrawString(text, ScaleFont(drawFont), drawBrush, ScaleRectangle(drawRectangle), stringFormat);
            drawBrush.Dispose();
        }

        public void DrawTitle(string title, Font drawFont, Color drawColor, PointF? pos = null)
        { 
            if (pos == null)
                pos = new(canvasSize.X / 2.0f, drawFont.Size + 4);

            SolidBrush drawBrush = new(drawColor);
            render.DrawString(title, drawFont, drawBrush, (PointF)pos, stringFormat);
            drawBrush.Dispose();
        }

        public void DrawBox(Color drawColor, float lineSize, RectangleF drawRectangle)
        {
            Pen pen = new(drawColor, lineSize * scalar.Y);
            RectangleF rect = ScaleRectangle(drawRectangle);
            
            render.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            pen.Dispose();
        }

        public void Rotate(float angle, PointF offset)
        {
            PointF center = GetCanvasCenter();
            render.TranslateTransform(center.X + offset.X , center.Y + offset.Y );
            render.RotateTransform(angle);
            render.TranslateTransform(-(center.X + offset.X ), -(center.Y + offset.Y ));
        }

        public void DrawArc(Arc drawArc, Color drawColor)
        {
            RectangleF drawRect = drawArc.GetRectangle(GetCanvasCenter(), scalar, IsSquare);

            float scale = (IsSquare ? scalar.X : nonSquareScale);
            Pen pen = new(drawColor, drawArc.Width * scale);
            render.DrawArc(pen, drawRect, drawArc.StartAngle, drawArc.SweepAngle);
            pen.Dispose();
        }

        public void DrawArcIndicator(Arc drawArc, Color drawColor, float size, float value, float minimum, float maximum, bool bottom = false)
        {
            RectangleF drawRect = drawArc.GetRectangle(GetCanvasCenter(), scalar, IsSquare);
            float angle = (NormalizedRatio(value, minimum, maximum) * drawArc.SweepAngle) + drawArc.StartAngle;

            size = (size * (IsSquare ? scalar.Y : nonSquareScale)) / 2.0f;
            float orgIndX = drawRect.X + drawRect.Width + (bottom ? -size : size);
            float orgIndY = drawRect.Y + drawRect.Width / 2.0f;
            float top = bottom ? -size : size;
            
            PointF[] triangle = [   new PointF(orgIndX - top, orgIndY),
                                    new PointF(orgIndX + top, orgIndY + size),
                                    new PointF(orgIndX + top, orgIndY - size) ];

            SolidBrush brush = new(drawColor);
            Rotate(angle, drawArc.Offset);
            render.FillPolygon(brush, triangle);
            Rotate(-angle, drawArc.Offset);
            brush.Dispose();
        }

        public void DrawArcCenterLine(Arc drawArc, Color drawColor, float size)
        {
            RectangleF drawRect = drawArc.GetRectangle(GetCanvasCenter(), scalar, IsSquare);
            float orgIndX = drawRect.X + drawRect.Width;
            float orgIndY = (drawRect.Y + drawRect.Width / 2.0f);
            float angle = (drawArc.SweepAngle / 2.0f) + drawArc.StartAngle;


            Pen pen = new(drawColor, size * (IsSquare ? scalar.Y : nonSquareScale));
            Rotate(angle, drawArc.Offset);
            render.DrawLine(pen, orgIndX - (drawArc.Width * (IsSquare ? scalar.X : nonSquareScale) * 0.5f), orgIndY, orgIndX + (drawArc.Width * (IsSquare ? scalar.X : nonSquareScale) * 0.5f), orgIndY);
            Rotate(-angle, drawArc.Offset);
            pen.Dispose();
        }

        public void DrawArcRanges(Arc drawArc, Color[] colors, float[][] ranges, float minimum, float maximum, bool symm = false)
        {
            if (maximum == 0.0f)
                return;

            RectangleF drawRect = drawArc.GetRectangle(GetCanvasCenter(), scalar, IsSquare);
            float rangeAngleStart;
            float rangeAngleSweep;
            float fix = 1.0f;
            for (int i = 0; i < ranges.Length; i++)
            {
                rangeAngleStart = NormalizedRatio(ranges[i][0], minimum, maximum) * drawArc.SweepAngle;
                rangeAngleSweep = NormalizedDiffRatio(ranges[i][1], ranges[i][0], minimum, maximum) * drawArc.SweepAngle;   

                Pen pen = new(colors[i], drawArc.Width * (IsSquare ? scalar.Y : nonSquareScale));
                render.DrawArc(pen, drawRect, drawArc.StartAngle + rangeAngleStart - fix, rangeAngleSweep + fix);

                if (symm)
                {
                    rangeAngleStart = NormalizedDiffRatio(maximum, ranges[i][1], minimum, maximum) * drawArc.SweepAngle;
                    render.DrawArc(pen, drawRect, drawArc.StartAngle + rangeAngleStart - fix, rangeAngleSweep + fix);
                }

                pen.Dispose();
            }
        }

        public void DrawBar(Color mainColor, Bar drawBar)
        {
            SolidBrush brush = new(mainColor);
            render.FillRectangle(brush, drawBar.GetRectangle(canvasSize, scalar));

            brush.Dispose();
        }

        public void DrawBarCenterLine(Bar drawBar, Color centerColor, float centerSize)
        {
            Pen pen = new(centerColor, centerSize * (IsSquare ? scalar.Y : nonSquareScale));
            RectangleF drawParams = drawBar.GetRectangle(canvasSize, scalar);
            float off = (drawParams.Width / 2.0f);//+ 0.5f;
            render.DrawLine(pen, drawParams.X + off, drawParams.Y, drawParams.X + off, drawParams.Y + drawParams.Height);

            pen.Dispose();
        }

        public void DrawBarIndicator(Bar drawBar, Color drawColor, float size, float value, float minimum, float maximum, bool bottom = false)
        {
            if (maximum == 0.0f)
                return;

            size = (size * (IsSquare ? scalar.X : nonSquareScale)) / 2.0f;
            RectangleF drawParams = drawBar.GetRectangle(canvasSize, scalar);
            float indX = (drawParams.X + (NormalizedRatio(value, minimum, maximum) * drawParams.Width));
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y);
            float top = (bottom ? size * -1.0f : size);
            PointF[] triangle = [new(indX - size, indY - top), new(indX + size, indY - top), new(indX, indY + top)];

            SolidBrush brush = new(drawColor);
            render.FillPolygon(brush, triangle);
            brush.Dispose();
        }

        public void DrawBarRanges(Bar drawBar, Color[] colors, float[][] ranges, float minimum, float maximum, bool symm = false)
        {
            if (maximum == 0.0f)
                return;

            float barW;
            RectangleF drawParams = drawBar.GetRectangle(canvasSize, scalar);
            float fix = 0.5f;
            for (int i = 0; i < ranges.Length; i++)
            {
                barW = NormalizedDiffRatio(ranges[i][1], ranges[i][0], minimum, maximum) * drawParams.Width;

                SolidBrush brush = new(colors[i]);
                render.FillRectangle(brush, drawParams.X + NormalizedRatio(ranges[i][0], minimum, maximum) * drawParams.Width, drawParams.Y, barW + fix, drawParams.Height);

                if (symm)
                    render.FillRectangle(brush, (drawParams.X + NormalizedDiffRatio(maximum, ranges[i][1], minimum, maximum) * drawParams.Width), drawParams.Y, barW + fix, drawParams.Height);

                brush.Dispose();
            }
        }

        protected static float NormalizedDiffRatio(float minuend, float subtrahend, float minimumTotal, float maximumTotal)
        {
            SwapMinMax(ref minimumTotal, ref maximumTotal);

            return Ratio((NormalizedValue(minuend, minimumTotal) - NormalizedValue(subtrahend, minimumTotal)), NormalizedValue(maximumTotal, minimumTotal));
        }

        public static float NormalizedValue(float value, float minimum)
        {
            if (minimum < 0.0f)
                value += Math.Abs(minimum);
            else if (minimum > 0.0f)
                value -= minimum;

            return value;
        }

        protected static float NormalizedRatio(float value, float minimum, float maximum)
        {
            SwapMinMax(ref minimum, ref maximum);

            if (minimum < 0.0f)
            {
                maximum += Math.Abs(minimum);
                value += Math.Abs(minimum);
            }
            else if (minimum > 0.0f)
            {
                maximum -= minimum;
                value -= minimum;
            }

            return Ratio(value, maximum);
        }

        protected static float Ratio(float value, float maximum)
        {
            float ratio = value / maximum;
            if (ratio < 0.0f)
                ratio = 0.0f;
            else if (ratio > 1.0f)
                ratio = 1.0f;

            return ratio;
        }

        protected static void SwapMinMax(ref float minimum, ref float maximum)
        {
            if (minimum > maximum)
            {
                (maximum, minimum) = (minimum, maximum);
            }
        }

        public string RenderImage64()
        {
            string image64 = "";

            using (MemoryStream stream = new())
            {
                background.Save(stream, ImageFormat.Png);
                image64 = Convert.ToBase64String(stream.ToArray());
                stream.Dispose();
            }            

            return image64;
        }

        public void Dispose()
        {
            stringFormat.Dispose();
            render.Dispose();
            background.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
