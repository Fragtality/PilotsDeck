using System;
using System.Drawing;
using System.IO;

namespace PilotsDeck
{
    public class ManagedImage
    {
        public string FileName { get; set; }
        public byte[] Bytes { get; set; } = null;
        public Point Size { get; set; }
        public int Registrations { get; set; } = 0;

        public bool IsSquare { get { return Size.X == Size.Y; } }
        public bool IsLoaded { get; protected set; }

        public ManagedImage(string file)
        {
            FileName = file;
            if (Load())
                IsLoaded = GetImageTypeAndSize();
            else
                IsLoaded = false;
        }

        protected bool Load()
        {
            try
            {
                Bytes = File.ReadAllBytes(FileName);
                return Bytes != null && Bytes.Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedImage:Load", $"Exception while reading Images Bytes from File! (Name: {FileName}) (Exception: {ex.GetType()})");
            }

            return false;
        }

        protected bool GetImageTypeAndSize()
        {
            try
            {
                using Image tmpImage = GetImageObject();
                GraphicsUnit units = GraphicsUnit.Pixel;
                RectangleF tmpRect = tmpImage.GetBounds(ref units);
                Size = new Point((int)tmpRect.Width, (int)tmpRect.Height);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedImage:GetImageTypeAndSize", $"Exception while getting Images Parameters! (Name: {FileName}) (Exception: {ex.GetType()})");
            }

            return false;
        }

        public string GetImageBase64()
        {
            return Convert.ToBase64String(Bytes, Base64FormattingOptions.None);
        }

        public Image GetImageObject()
        {
            using MemoryStream stream = new(Bytes);
            return Image.FromStream(stream);
        }
    }
}
