using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

namespace PilotsDeck
{
    public static class ImageTools
    {
        public static void ConvertFontParameter(ModelDisplayText model, out Font drawFont, out Color drawColor)
        {
            drawFont = new Font(model.FontName, model.FontSize, (FontStyle)model.FontStyle); //GraphicsUnit.Point ?
            drawColor = ColorTranslator.FromHtml(model.FontColor);
        }

        public static StringFormat stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.FitBlackBox
            };

        public static void SetGraphicsQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
        }

        public static string DrawText(string text, Image background, Font drawFont, Color drawColor, RectangleF drawRectangle)
        {
            string image64 = "";
            Bitmap bitmap = new Bitmap(background);
            Graphics render = Graphics.FromImage(bitmap);
            SetGraphicsQuality(render);
            SolidBrush drawBrush = new SolidBrush(drawColor);
            
            render.DrawString(text, drawFont, drawBrush, drawRectangle, stringFormat);

            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                image64 = Convert.ToBase64String(stream.ToArray());
                stream.Dispose();
            }
            render.Dispose();
            background.Dispose();
            drawBrush.Dispose();
            bitmap.Dispose();

            return image64;
        }
    }
}
