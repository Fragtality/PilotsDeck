using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Serilog;

namespace PilotsDeck
{
    public class ImageManager : IDisposable
    {
        private Dictionary<string, byte[]> cachedImageFiles = new Dictionary<string, byte[]>(); //filename -> base64

        public ImageManager(string[] imageFiles)
        {
            try
            {
                foreach (var image in imageFiles)
                    cachedImageFiles.Add(image, StreamDeckTools.ReadImageBytes(image));
            }
            catch
            {
                Log.Logger.Error("ImageManager: Exception while loading ImageFiles");
            }
        }

        public bool IsCached(string image)
        {
            return cachedImageFiles.ContainsKey(image);
        }

        public string GetImageBase64(string image)
        {
            return StreamDeckTools.ToImageBase64(GetImageBytes(image));
        }

        public Image GetImageObject(string image)
        {
            using (MemoryStream stream = new MemoryStream(GetImageBytes(image)))
            {
                return Image.FromStream(stream);
            }
        }

        public byte[] GetImageBytes(string image)
        {
            if (!cachedImageFiles.ContainsKey(image))
            {
                AddImage(image);
                Log.Logger.Verbose($"ImageManager: Cached new Image {image}");
                return cachedImageFiles[image];
            }
            else if (image.Length > 0)
                return cachedImageFiles[image];
            else
                return new byte[1];
        }

        public void AddImage(string image)
        {
            try
            {
                if (!cachedImageFiles.ContainsKey(image) && image.Length > 0)
                    cachedImageFiles.Add(image, StreamDeckTools.ReadImageBytes(image));
            }
            catch
            {
                Log.Logger.Error($"AddImage: Exception while loading Image {image}");
            }
        }

        public void UpdateImage(string image)
        {
            try
            {
                if (cachedImageFiles.ContainsKey(image))
                    cachedImageFiles[image] = StreamDeckTools.ReadImageBytes(image);
            }
            catch
            {
                Log.Logger.Error($"AddImage: Exception while loading Image {image}");
            }
        }

        public void RemoveImage(string image)
        {
            if (cachedImageFiles.ContainsKey(image))
                cachedImageFiles.Remove(image);
        }

        public void Dispose()
        {
            InvalidateCaches();
            cachedImageFiles = null;
        }

        public void InvalidateCaches()
        {
            cachedImageFiles.Clear();
        }

        //public string DrawTextToImage(string text, string image, ModelDisplayText model, string colorOverride = null)
        //{
        //    if (!IsCached(image))
        //        AddImage(image);

        //    using (MemoryStream stream = new MemoryStream(cachedImageFiles[image]))
        //    {
        //        ImageTools.ConvertFontParameter(model, out Font drawFont, out Color drawColor);
        //        if (colorOverride != null)
        //            drawColor = ColorTranslator.FromHtml(colorOverride);

        //        return ImageTools.DrawText(text, Image.FromStream(stream), drawFont, drawColor, model.FontRect);
        //    }
        //}
    }
}
