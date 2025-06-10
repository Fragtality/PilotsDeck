using PilotsDeck.Resources.Images;
using PilotsDeck.StreamDeck;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace PilotsDeck.Plugin.Render
{
    public class Renderer : IDisposable
    {
        public static readonly float NON_SQUARE_SCALE = 1.5f;
        public static readonly PointF DEFAULT_SIZE = new(72.0f, 72.0f);
        public Graphics Render { get; protected set; }
        protected Bitmap RenderImage { get; set; }
        public PointF DeviceCanvas { get; protected set; }
        public PointF DefaultScalar { get; protected set; }
        public bool IsSquareCanvas { get { return DeviceCanvas.IsSquare(); } }
        public ImageVariant PreferredVariant { get; protected set; }
        public CenterType DefaultCenter { get; set; } = CenterType.HORIZONTAL;
        public ScaleType DefaultScale { get; set; } = ScaleType.DEFAULT_KEEP;
        public float Transparency { get; set; } = 1.0f;

        public Renderer(StreamDeckCanvasInfo canvasInfo)
        {
            DeviceCanvas = canvasInfo.GetCanvasSize();
            PreferredVariant = canvasInfo.GetImageVariant();
            DefaultScalar = ToolsRender.GetScale(DeviceCanvas, DEFAULT_SIZE);

            RenderImage = new Bitmap((int)DeviceCanvas.X, (int)DeviceCanvas.Y);
            RenderImage.SetResolution(App.Configuration.RenderDpi, App.Configuration.RenderDpi);

            Render = Graphics.FromImage(RenderImage);
            Render.SmoothingMode = SmoothingMode.AntiAlias;
            Render.InterpolationMode = InterpolationMode.HighQualityBicubic;
            Render.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Render.CompositingQuality = CompositingQuality.HighQuality;
            Render.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Render.PageUnit = GraphicsUnit.Pixel;
        }

        public int GetAlpha()
        {
            return GetAlpha(Transparency);
        }

        public static int GetAlpha(float transparency)
        {
            if (transparency != 1.0f)
                return (int)(255.0f * transparency);
            else
                return 255;
        }

        protected RectangleF Scale(RectangleF rect, ScaleType scale)
        {
            if (scale == ScaleType.NONE || rect.MatchesSize(DeviceCanvas))
                return rect;

            var scalar = ToolsRender.GetScale(DeviceCanvas, rect);

            if (scale == ScaleType.DEFAULT_STRETCH)
                return new RectangleF(rect.X * DefaultScalar.X, rect.Y * DefaultScalar.Y, rect.Width * DefaultScalar.X, rect.Height * DefaultScalar.Y);
            else if (scale == ScaleType.DEVICE_STRETCH)
                return new RectangleF(rect.X * scalar.X, rect.Y * scalar.Y, rect.Width * scalar.X, rect.Height * scalar.Y);
            else if (scale == ScaleType.DEFAULT_KEEP)
            {
                var rectY = new RectangleF(rect.X * DefaultScalar.Y, rect.Y * DefaultScalar.Y, rect.Width * DefaultScalar.Y, rect.Height * DefaultScalar.Y);
                var rectX = new RectangleF(rect.X * DefaultScalar.X, rect.Y * DefaultScalar.X, rect.Width * DefaultScalar.X, rect.Height * DefaultScalar.X);
                if (rectY.WithinSize(DeviceCanvas))
                    return rectY;
                else if (rectX.WithinSize(DeviceCanvas))
                    return rectX;
                else
                    return rect;
            }
            else if (scale == ScaleType.DEVICE_KEEP)
            {
                var rectY = new RectangleF(rect.X * scalar.Y, rect.Y * scalar.Y, rect.Width * scalar.Y, rect.Height * scalar.Y);
                var rectX = new RectangleF(rect.X * scalar.X, rect.Y * scalar.X, rect.Width * scalar.X, rect.Height * scalar.X);
                if (rectY.WithinSize(DeviceCanvas))
                    return rectY;
                else if (rectX.WithinSize(DeviceCanvas))
                    return rectX;
                else
                    return rect;
            }
            else
                return rect;
        }

        protected PointF Scale(PointF point, ScaleType scale)
        {
            if (scale == ScaleType.NONE || point.MatchesSize(DeviceCanvas))
                return point;

            var scalar = ToolsRender.GetScale(DeviceCanvas, point);

            if (scale == ScaleType.DEFAULT_STRETCH)
                return new PointF(point.X * DefaultScalar.X, point.Y * DefaultScalar.Y);
            else if (scale == ScaleType.DEVICE_STRETCH)
                return new PointF(point.X * scalar.X, point.Y * scalar.Y);
            else if (scale == ScaleType.DEFAULT_KEEP)
            {
                var pointX = new PointF(point.X * DefaultScalar.X, point.Y * DefaultScalar.X);
                var pointY = new PointF(point.X * DefaultScalar.Y, point.Y * DefaultScalar.Y);
                if (pointY.WithinSize(DeviceCanvas))
                    return pointX;
                else if (pointX.WithinSize(DeviceCanvas))
                    return pointX;
                else
                    return point;
            }
            else if (scale == ScaleType.DEVICE_KEEP)
            {
                var pointX = new PointF(point.X * scalar.X, point.Y * scalar.X);
                var pointY = new PointF(point.X * scalar.Y, point.Y * scalar.Y);
                if (pointY.WithinSize(DeviceCanvas))
                    return pointX;
                else if (pointX.WithinSize(DeviceCanvas))
                    return pointX;
                else
                    return point;
            }
            else
                return point;
        }

        protected Font Scale(Font font, RectangleF rect, ScaleType scale)
        {
            var scalar = ToolsRender.GetScale(DeviceCanvas, new PointF(rect.Width, rect.Height));

            if (scale == ScaleType.DEFAULT_KEEP || scale == ScaleType.DEFAULT_STRETCH)
                return new Font(font.Name, font.Size * DefaultScalar.Y, font.Style, GraphicsUnit.Point);
            else if (scale == ScaleType.DEVICE_KEEP || scale == ScaleType.DEVICE_STRETCH)
                return new Font(font.Name, font.Size * scalar.Y, font.Style, GraphicsUnit.Point);
            else
                return new Font(font.Name, font.Size, font.Style, GraphicsUnit.Point);
        }

        protected float Scale(float value, RectangleF rect, ScaleType scale)
        {
            var scalar = ToolsRender.GetScale(DeviceCanvas, rect);

            if (scale == ScaleType.DEFAULT_KEEP || scale == ScaleType.DEFAULT_STRETCH)
                return value * DefaultScalar.Y;
            else if (scale == ScaleType.DEVICE_KEEP || scale == ScaleType.DEVICE_STRETCH)
                return value * scalar.Y;
            else
                return value;
        }

        protected static RectangleF CheckSize(RectangleF rect, PointF defaultSize)
        {
            if (rect.Width == 0 && rect.Height == 0)
                return new RectangleF(rect.X, rect.Y, defaultSize.X, defaultSize.Y);
            else if (rect.Width == 0)
                return new RectangleF(rect.X, rect.Y, defaultSize.X, rect.Height);
            else if (rect.Height == 0)
                return new RectangleF(rect.X, rect.Y, rect.Width, defaultSize.Y);
            else
                return rect;
        }

        public PointF GetImageSize(ManagedImage image)
        {
            return GetMatchingImage(image).GetPoint();
        }

        protected Image GetMatchingImage(ManagedImage image)
        {
            Image result;

            if (image.HasMatchingImage(DeviceCanvas))
                result = image.GetMatchingImage(DeviceCanvas);
            else if (image.HasImageVariant(PreferredVariant))
                result = image.GetImageVariant(PreferredVariant);
            else if ((PreferredVariant == ImageVariant.PLUS || PreferredVariant == ImageVariant.DEFAULT_HQ) && image.HasImageVariant(ImageVariant.DEFAULT_HQ))
                result = image.GetImageVariant(ImageVariant.DEFAULT_HQ);
            else
                result = image.GetImageVariant(ImageVariant.DEFAULT);

            return result;
        }

        public void DrawImage(Image image, RectangleF drawRect)
        {
            DrawImage(image, drawRect, DefaultCenter);
        }

        protected void DrawImage(Image image, RectangleF drawRect, CenterType center)
        {
            if (Transparency < 1.0f)
            {
                ColorMatrix cm = new() { Matrix33 = Transparency };
                ImageAttributes ia = new();
                ia.SetColorMatrix(cm);
                drawRect = drawRect.Center(DeviceCanvas.Center(), center);
                Render.DrawImage(image, drawRect.Convert(), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
            }
            else
                Render.DrawImage(image, drawRect.Center(DeviceCanvas.Center(), center));
        }

        public void DrawImage(ManagedImage managedImage, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null)
        {
            Image image = GetMatchingImage(managedImage);
            drawRect = CheckSize(drawRect, image.GetPoint());
            DrawImage(image, Scale(drawRect, scale ?? DefaultScale), center ?? DefaultCenter);
        }

        public void DrawImage(ManagedImage managedImage, CenterType? center = null, ScaleType? scale = null)
        {
            Image image = GetMatchingImage(managedImage);
            DrawImage(image, Scale(image.GetRectangle(), scale ?? DefaultScale), center ?? DefaultCenter);
        }

        public void DrawText(string text, Font drawFont, Color drawColor, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null, StringAlignment horizontal = StringAlignment.Center, StringAlignment vertical = StringAlignment.Center)
        {
            SolidBrush drawBrush;
            if (ToolsRender.GetColorFromString(ref text, out Color strColor))
                drawBrush = new SolidBrush(Color.FromArgb(GetAlpha(), strColor));
            else
                drawBrush = new(Color.FromArgb(GetAlpha(), drawColor));

            Render.DrawString(text.Replace("[[n","\n"), Scale(drawFont, drawRect, scale ?? DefaultScale), drawBrush, Scale(CheckSize(drawRect, DeviceCanvas), scale ?? DefaultScale).Center(DeviceCanvas.Center(), center ?? DefaultCenter), ToolsRender.GetStringFlags(horizontal, vertical));
            drawBrush.Dispose();
        }

        public void DrawEncoderTitle(string title, Font drawFont, Color drawColor, bool center = false)
        {
            PointF pos = new(DeviceCanvas.X / 2.0f, drawFont.Size + 4);
            if (center)
                pos = new PointF(100, 51.0f);

            SolidBrush drawBrush = new(drawColor);
            Render.DrawString(title, Scale(drawFont, new(0, 0, 200, 100), ScaleType.NONE), drawBrush, pos, ToolsRender.DefaultStringFormat);
            drawBrush.Dispose();
        }

        public void FillRectangle(Color color, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null)
        {
            drawRect = CheckSize(drawRect, DeviceCanvas);
            Render.FillRectangle(new SolidBrush(Color.FromArgb(GetAlpha(), color)), Scale(drawRect, scale ?? DefaultScale).Center(DeviceCanvas.Center(), center ?? DefaultCenter));
        }

        public void DrawRectangle(Color drawColor, float lineSize, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null)
        {
            lineSize = Scale(lineSize, drawRect, scale ?? DefaultScale);
            drawRect = CheckSize(drawRect, DeviceCanvas);
            Render.DrawRectangle(new(Color.FromArgb(GetAlpha(), drawColor), lineSize), Scale(drawRect, scale ?? DefaultScale).Center(DeviceCanvas.Center(), center ?? DefaultCenter));
        }

        public void DrawLine(Color drawColor, float lineSize, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null)
        {
            lineSize = Scale(lineSize, drawRect, scale ?? DefaultScale);
            drawRect = Scale(CheckSize(drawRect, DeviceCanvas), scale ?? DefaultScale).Center(DeviceCanvas.Center(), center ?? DefaultCenter);
            Render.DrawLine(new(Color.FromArgb(GetAlpha(), drawColor), lineSize), drawRect.Location, drawRect.GetDestination());
        }

        public void DrawEllipse(Color drawColor, float lineSize, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null)
        {
            lineSize = Scale(lineSize, drawRect, scale ?? DefaultScale);
            Render.DrawEllipse(new(Color.FromArgb(GetAlpha(), drawColor), lineSize), Scale(CheckSize(drawRect, DeviceCanvas), scale ?? DefaultScale).Center(DeviceCanvas.Center(), center ?? DefaultCenter));
        }

        public void FillEllipse(Color drawColor, RectangleF drawRect, CenterType? center = null, ScaleType? scale = null)
        {
            Render.FillEllipse(new SolidBrush(Color.FromArgb(GetAlpha(), drawColor)), Scale(CheckSize(drawRect, DeviceCanvas), scale ?? DefaultScale).Center(DeviceCanvas.Center(), center ?? DefaultCenter));
        }

        public void RotateCenter(float angle, PointF offset)
        {
            PointF center = DeviceCanvas.Center();
            Render.TranslateTransform(center.X + offset.X, center.Y + offset.Y);
            Render.RotateTransform(angle);
            Render.TranslateTransform(-(center.X + offset.X), -(center.Y + offset.Y));
        }

        public void RotatePoint(float angle, PointF point)
        {
            Render.TranslateTransform(point.X, point.Y);
            Render.RotateTransform(angle);
            Render.TranslateTransform(-point.X, -point.Y);
        }

        public void RotateRectangle(float angle, RectangleF rect, CenterType? centerType = null, ScaleType? scale = null)
        {
            rect = Scale(CheckSize(rect, DeviceCanvas), scale ?? DefaultScale).Center(DeviceCanvas.Center(), centerType ?? DefaultCenter);
            var half = rect.Size.ToPointF().Center();
            float x = rect.X + half.X;
            float y = rect.Y + half.Y;
            float fix = angle >= 0 ? 0.5f : -0.5f;
            Render.TranslateTransform(x, y);
            Render.RotateTransform(angle + fix);
            Render.TranslateTransform(-x, -y);
        }

        public void MirrorX(PointF offset)
        {
            PointF center = DeviceCanvas.Center();
            Render.TranslateTransform(center.X + offset.X, center.Y + offset.Y);
            Render.ScaleTransform(-1, 1);
            Render.TranslateTransform(-(center.X + offset.X), -(center.Y + offset.Y));
        }

        public string RenderImage64()
        {
            string image64 = "";

            using (MemoryStream stream = new())
            {
                RenderImage.Save(stream, ImageFormat.Png);
                image64 = Convert.ToBase64String(stream.ToArray());
                stream.Dispose();
            }

            return image64;
        }


        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Render.Dispose();
                    RenderImage.Dispose();
                }
                _disposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
