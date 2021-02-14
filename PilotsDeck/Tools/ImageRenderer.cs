using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace PilotsDeck
{
    public class ImageRenderer : IDisposable
    {
        protected Image imageRef;
        protected Bitmap background;
        protected Graphics render;

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
        }

        public void DrawText(string text, Font drawFont, Color drawColor, RectangleF drawRectangle)
        {
            SolidBrush drawBrush = new SolidBrush(drawColor);
            render.DrawString(text, drawFont, drawBrush, drawRectangle, stringFormat);
            drawBrush.Dispose();
        }

        public void Rotate(float angle)
        {
            render.TranslateTransform(36, 36);
            render.RotateTransform(angle);
            render.TranslateTransform(-36, -36);
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

            float off = size / 2.0f;
            float indX = (drawParams.X + (NormalizedRatio(value, minimum, maximum) * drawParams.Width));// + 0.5f;
            float indY = (bottom ? drawParams.Y + drawParams.Height : drawParams.Y);
            float top = (bottom ? off * -1.0f : off);
            PointF[] triangle = { new PointF(indX-off, indY-top), new PointF(indX+off, indY-top), new PointF(indX, indY+top) };

            SolidBrush brush = new SolidBrush(drawColor);
            render.FillPolygon(brush, triangle);
            brush.Dispose();
        }

        public void DrawBarStages(RectangleF drawParams, Color[] colors, float[][] ranges, float minimum, float maximum, bool symm = false)
        {
            if (maximum == 0.0f)
                return;

            for (int i = 0; i < ranges.Length; i++)
            {
                float barW = PercentOfTotal(ranges[i][0], ranges[i][1], minimum, maximum) * drawParams.Width;

                SolidBrush brush = new SolidBrush(colors[i]);
                render.FillRectangle(brush, drawParams.X + NormalizedRatio(ranges[i][0], minimum, maximum) * drawParams.Width, drawParams.Y, barW, drawParams.Height);

                if (symm)
                    render.FillRectangle(brush, (drawParams.X + InvertedRatio(ranges[i][1], minimum, maximum) * drawParams.Width), drawParams.Y, barW, drawParams.Height);

                brush.Dispose();
            }
        }

        protected static float InvertedRatio(float maximum, float minimumTotal, float maximumTotal)
        {
            float ratio = (NormalizedValue(maximumTotal, minimumTotal) - NormalizedValue(maximum, minimumTotal)) / NormalizedValue(maximumTotal, minimumTotal);

            if (ratio < 0.0f)
                ratio = 0.0f;
            else if (ratio > 1.0f)
                ratio = 1.0f;

            return ratio;
        }

        protected static float PercentOfTotal(float minimum, float maximum, float minimumTotal, float maximumTotal)
        {
            SwapMinMax(ref minimum, ref maximum);
            SwapMinMax(ref minimumTotal, ref maximumTotal);

            float ratio = (NormalizedValue(maximum, minimumTotal) - NormalizedValue(minimum, minimumTotal)) / NormalizedValue(maximumTotal, minimumTotal);

            

            if (ratio < 0.0f)
                ratio = 0.0f;
            else if (ratio > 1.0f)
                ratio = 1.0f;

            return ratio;
        }

        public static float NormalizedValue(float value, float minimum)
        {
            if (minimum < 0.0f)
                value += Math.Abs(minimum);
            else if (minimum > 0.0f)
                value -= minimum;

            return value;
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

            float ratio = value / maximum;
            if (ratio < 0.0f)
                ratio = 0.0f;
            else if (ratio > 1.0f)
                ratio = 1.0f;

            return ratio;
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
