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
