using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace PilotsDeck
{
    public class Arc
    {
        public int Radius { get; set; } = 48;
        public int Width { get; set; } = 6;
        public int Offset { get; set; } = 0;
        public int StartAngle { get; set; } = 135;
        public int SweepAngle { get; set; } = 180;

        public RectangleF GetRectangle(int buttonSize, int sizeScalar)
        {
            int org = ((buttonSize - Radius * sizeScalar) / 2) + Offset; // btn - <R> / 2 => x/y
            return new RectangleF(org, org, Radius * sizeScalar, Radius * sizeScalar);
        }
    }

    public class ImageRenderer : IDisposable
    {
        protected Image imageRef;
        protected Bitmap background;
        protected Graphics render;

        //public static readonly int buttonSize = 144;
        //protected static int buttonSizeH = buttonSize/2;
        public static int buttonSize = 72;
        protected int buttonSizeH;
        protected int sizeScalar;

        protected StringFormat stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.FitBlackBox
        };

        public ImageRenderer(Image image)
        {
            imageRef = image;
            background = new Bitmap(imageRef);
            render = Graphics.FromImage(background);

            render.SmoothingMode = SmoothingMode.HighQuality;
            render.InterpolationMode = InterpolationMode.HighQualityBicubic;
            render.PixelOffsetMode = PixelOffsetMode.HighQuality;
            render.CompositingQuality = CompositingQuality.HighQuality;

            buttonSize = background.Width;
            buttonSizeH = buttonSize / 2;
            sizeScalar = buttonSize / 72;
        }

        public void DrawImage(Image image, Rectangle drawRectangle)
        {
            render.DrawImage(image, drawRectangle.X, drawRectangle.Y, drawRectangle.Width, drawRectangle.Height);
        }

        public void DrawText(string text, Font drawFont, Color drawColor, RectangleF drawRectangle)
        {
            SolidBrush drawBrush = new SolidBrush(drawColor);
            render.DrawString(text, drawFont, drawBrush, drawRectangle, stringFormat);
            drawBrush.Dispose();
        }

        public void DrawBox(Color drawColor, int lineSize, RectangleF drawRectangle)
        {
            Pen pen = new Pen(drawColor, lineSize);
            render.DrawRectangle(pen, drawRectangle.X, drawRectangle.Y, drawRectangle.Width, drawRectangle.Height);
            pen.Dispose();
        }

        public void Rotate(float angle, float offset = 0)
        {
            render.TranslateTransform(buttonSizeH + offset, buttonSizeH + offset);
            render.RotateTransform(angle);
            render.TranslateTransform(-(buttonSizeH + offset), -(buttonSizeH + offset));
        }

        public void DrawArc(Arc drawArc, Color drawColor)
        {
            RectangleF drawRect = drawArc.GetRectangle(buttonSize, sizeScalar);
            
            Pen pen = new Pen(drawColor, drawArc.Width * sizeScalar);
            render.DrawArc(pen, drawRect, drawArc.StartAngle, drawArc.SweepAngle);
            pen.Dispose();
        }

        public void DrawArcIndicator(Arc drawArc, Color drawColor, float size, float value, float minimum, float maximum, bool bottom = false)
        {
            RectangleF drawRect = drawArc.GetRectangle(buttonSize, sizeScalar);
            float angle = (NormalizedRatio(value, minimum, maximum) * drawArc.SweepAngle) + drawArc.StartAngle;
            
            size /= 2;
            float orgIndX = drawRect.X + drawRect.Width + (bottom ? -size : size);
            float orgIndY = drawRect.Y + drawRect.Width / 2;
            float top = bottom ? -size : size;
            
            PointF[] triangle = {   new PointF(orgIndX - top, orgIndY),
                                    new PointF(orgIndX + top, orgIndY + size),
                                    new PointF(orgIndX + top, orgIndY - size) };

            SolidBrush brush = new SolidBrush(drawColor);
            Rotate(angle, drawArc.Offset);
            render.FillPolygon(brush, triangle);
            Rotate(-angle, drawArc.Offset);
            brush.Dispose();
        }

        public void DrawArcCenterLine(Arc drawArc, Color drawColor, int size)
        {
            RectangleF drawRect = drawArc.GetRectangle(buttonSize, sizeScalar);
            float orgIndX = drawRect.X + drawRect.Width;
            float orgIndY = (drawRect.Y + drawRect.Width / 2);
            float angle = (drawArc.SweepAngle / 2) + drawArc.StartAngle;

            Pen pen = new Pen(drawColor, size);
            Rotate(angle, drawArc.Offset);
            render.DrawLine(pen, orgIndX - drawArc.Width * 0.5f, orgIndY, orgIndX + drawArc.Width * 0.5f, orgIndY); ;
            Rotate(-angle, drawArc.Offset);
            pen.Dispose();
        }

        public void DrawArcRanges(Arc drawArc, Color[] colors, float[][] ranges, float minimum, float maximum, bool symm = false)
        {
            if (maximum == 0.0f)
                return;

            RectangleF drawRect = drawArc.GetRectangle(buttonSize, sizeScalar);
            float rangeAngleStart;
            float rangeAngleSweep;
            float fix = 1.0f;
            for (int i = 0; i < ranges.Length; i++)
            {
                rangeAngleStart = NormalizedRatio(ranges[i][0], minimum, maximum) * drawArc.SweepAngle;
                rangeAngleSweep = NormalizedDiffRatio(ranges[i][1], ranges[i][0], minimum, maximum) * drawArc.SweepAngle;   

                Pen pen = new Pen(colors[i], drawArc.Width);
                render.DrawArc(pen, drawRect, drawArc.StartAngle + rangeAngleStart - fix, rangeAngleSweep + fix);

                if (symm)
                {
                    rangeAngleStart = NormalizedDiffRatio(maximum, ranges[i][1], minimum, maximum) * drawArc.SweepAngle;
                    render.DrawArc(pen, drawRect, drawArc.StartAngle + rangeAngleStart - fix, rangeAngleSweep + fix);
                }

                pen.Dispose();
            }
        }

        public void DrawBar(Color mainColor, RectangleF drawParams)
        {
            SolidBrush brush = new SolidBrush(mainColor);
            render.FillRectangle(brush, drawParams);

            brush.Dispose();
        }

        public void DrawBarCenterLine(RectangleF drawParams, Color centerColor, int centerSize)
        {
            Pen pen = new Pen(centerColor, centerSize);
            float off = (drawParams.Width / 2.0f);//+ 0.5f;
            render.DrawLine(pen, drawParams.X + off, drawParams.Y, drawParams.X + off, drawParams.Y + drawParams.Height);

            pen.Dispose();
        }

        public void DrawBarIndicator(RectangleF drawParams, Color drawColor, float size, float value, float minimum, float maximum, bool bottom = false)
        {
            if (maximum == 0.0f)
                return;

            size /= 2.0f;
            float indX = (drawParams.X + (NormalizedRatio(value, minimum, maximum) * drawParams.Width));
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y);
            float top = (bottom ? size * -1.0f : size);
            PointF[] triangle = { new PointF(indX - size, indY - top), new PointF(indX + size, indY - top), new PointF(indX, indY + top) };

            SolidBrush brush = new SolidBrush(drawColor);
            render.FillPolygon(brush, triangle);
            brush.Dispose();
        }

        public void DrawBarRanges(RectangleF drawParams, Color[] colors, float[][] ranges, float minimum, float maximum, bool symm = false)
        {
            if (maximum == 0.0f)
                return;

            float barW;
            float fix = 0.5f;
            for (int i = 0; i < ranges.Length; i++)
            {
                barW = NormalizedDiffRatio(ranges[i][1], ranges[i][0], minimum, maximum) * drawParams.Width;

                SolidBrush brush = new SolidBrush(colors[i]);
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
                float temp = minimum;
                minimum = maximum;
                maximum = temp;
            }
        }

        public string RenderImage64()
        {
            string image64 = "";

            using (MemoryStream stream = new MemoryStream())
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
            imageRef.Dispose();
        }


    }
}
