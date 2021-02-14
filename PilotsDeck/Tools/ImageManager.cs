using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using Serilog;

namespace PilotsDeck
{
    public class ImageManager : IDisposable
    {
        private Dictionary<string, byte[]> cachedImageFiles = new Dictionary<string, byte[]>(); //filename -> base64
        private Dictionary<string, int> currentRegistrations = new Dictionary<string, int>();

        public ImageManager()
        {

        }

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
                if (AddImage(image))
                {
                    Log.Logger.Verbose($"ImageManager: Cached new Image {image}");
                    return cachedImageFiles[image];
                }
                return cachedImageFiles.Values.First();
            }
            else if (image.Length > 0)
                return cachedImageFiles[image];
            else
                return cachedImageFiles.Values.First();
        }

        public bool AddImage(string image)
        {
            try
            {
                if (string.IsNullOrEmpty(image))
                {
                    Log.Logger.Error($"AddImage: Empty image string!");
                    throw new Exception("AddImage: Empty image string!");
                }

                if (!cachedImageFiles.ContainsKey(image))
                {
                    cachedImageFiles.Add(image, StreamDeckTools.ReadImageBytes(image));
                    currentRegistrations.Add(image, 1);
                }
                else
                    currentRegistrations[image]++;

                return true;
            }
            catch
            {
                Log.Logger.Error($"AddImage: Exception while loading Image {image}");
                return false;
            }
        }

        public void UpdateImage(string image)
        {
            try
            {
                if (string.IsNullOrEmpty(image))
                {
                    Log.Logger.Error($"UpdateImage: Empty image string!");
                    throw new Exception("UpdateImage: Empty image string!");
                }

                if (cachedImageFiles.ContainsKey(image))
                    cachedImageFiles[image] = StreamDeckTools.ReadImageBytes(image);
            }
            catch
            {
                Log.Logger.Error($"UpdateImage: Exception while loading Image {image}");
            }
        }

        public void RemoveImage(string image)
        {
            try
            {
                if (string.IsNullOrEmpty(image))
                {
                    Log.Logger.Error($"RemoveImage: Empty image string!");
                    throw new Exception("RemoveImage: Empty image string!");
                }

                if (cachedImageFiles.ContainsKey(image))
                {
                    if (currentRegistrations[image]-- == 0)
                    {
                        currentRegistrations.Remove(image);
                        cachedImageFiles.Remove(image);
                    }
                }
                else
                    Log.Logger.Error($"RemoveImage: Image was not found {image}");
            }
            catch
            {
                Log.Logger.Error($"RemoveImage: Exception while loading Image {image}");
            }
        }

        public void Dispose()
        {
            InvalidateCaches();
        }

        public void InvalidateCaches()
        {
            cachedImageFiles.Clear();
            currentRegistrations.Clear();
        }
    }
}
