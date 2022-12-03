using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PilotsDeck
{
    public class ImageDefinition
    {
        public string FileNameCommon { get; set; }
        public string FileNameReal { get; set; }
        public StreamDeckType DeckType { get; set; }
        public byte[] ImageBytes { get; set; }
        public int Registrations { get; set; }
        public bool IsLoaded
        {
            get { return ImageBytes != null && ImageBytes.Length > 0; }
        }

        public ImageDefinition(string image, StreamDeckType deckType, bool loadImage)
        {
            FileNameCommon = image;
            FileNameReal = GetImageFileReal(image, deckType);
            DeckType = deckType;
            if (loadImage)
                Load();
        }

        public void Load()
        {
            try
            {
                ImageBytes = File.ReadAllBytes(FileNameReal);
            }
            catch
            {
                Log.Logger.Error($"ImageDefinition: Exception while reading Images Bytes! {ToString()}");
            }
        }

        public string GetImageBase64()
        {
            return Convert.ToBase64String(ImageBytes, Base64FormattingOptions.None);
        }

        public Image GetImageObject()
        {
            using MemoryStream stream = new(ImageBytes);
            return Image.FromStream(stream);
        }

        public static string GetImageFileReal(string image, StreamDeckType deckType)
        {
            string imageReal = image;

            switch (deckType)
            {
                case StreamDeckType.StreamDeckXL:
                    if (!image.Contains(AppSettings.hqImageSuffix))
                    {
                        int idx = image.IndexOf(".png");
                        imageReal = image.Insert(idx, AppSettings.hqImageSuffix);
                        if (!File.Exists(imageReal))
                        {
                            if (File.Exists(image))
                                imageReal = image;
                            else
                                imageReal = AppSettings.waitImage;
                        }
                    }
                    break;
                default:
                    if (image.Contains(AppSettings.hqImageSuffix))
                        imageReal = image.Replace(AppSettings.hqImageSuffix, "");
                    if (!File.Exists(imageReal))
                        imageReal = AppSettings.waitImage;
                    break;
            }

            return imageReal;
        }

        public override string ToString()
        {
            return $"Real: [{FileNameReal}] - Ref: [{FileNameCommon}]";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not ImageDefinition objAsImage) return false;
            else return Equals(objAsImage);
        }

        public override int GetHashCode()
        {
            return FileNameReal.ToCharArray().Sum(x => x);
        }

        public bool Equals(ImageDefinition other)
        {
            if (other == null) return false;
            return FileNameReal == other.FileNameReal;
        }
    }

    public sealed class ImageManager : IDisposable
    {
        private Dictionary<string, ImageDefinition> cachedImages = new(); //realname -> obj

        public ImageManager()
        {

        }

        public bool AddImage(string image, StreamDeckType deckType)
        {
            return AddImage(new ImageDefinition(image, deckType, false));
        }

        public bool AddImage(ImageDefinition image)
        {
            try
            {
                if (!cachedImages.ContainsKey(image.FileNameReal))
                {
                    if (!image.IsLoaded)
                        image.Load();

                    image.Registrations = 1;
                    cachedImages.Add(image.FileNameReal, image);
                    Log.Logger.Debug($"ImageManager: Image added to Cache. {image}");
                }
                else
                {
                    cachedImages[image.FileNameReal].Registrations++;
                    Log.Logger.Debug($"ImageManager: Registration added to Image. Registrations {cachedImages[image.FileNameReal].Registrations} - {image}");
                }

                return true;
            }
            catch
            {
                Log.Logger.Error($"ImageManager: Exception during AddImage! {image}");
                return false;
            }
        }

        public void RemoveImage(string image, StreamDeckType deckType)
        {
            RemoveImage(new ImageDefinition(image, deckType, false));
        }

        public void RemoveImage(ImageDefinition image)
        {
            try
            {
                if (!cachedImages.ContainsKey(image.FileNameReal))
                {
                    Log.Logger.Error($"ImageManager: Error during RemoveImage, Image does not exist! {image}");
                    return;
                }

                cachedImages[image.FileNameReal].Registrations--;

                if (cachedImages[image.FileNameReal].Registrations == 0)
                {
                    cachedImages.Remove(image.FileNameReal);
                    Log.Logger.Debug($"ImageManager: Image removed from Cache. {image}");
                }
                else
                {
                    Log.Logger.Debug($"ImageManager: Registration removed from Image. {image} - Registrations {cachedImages[image.FileNameReal].Registrations}");
                }
            }
            catch
            {
                Log.Logger.Error($"ImageManager: Exception during RemoveImage! {image}");
            }
        }

        public void UpdateImage(string image, StreamDeckType deckType)
        {
            UpdateImage(new ImageDefinition(image, deckType, false));
        }

        public void UpdateImage(ImageDefinition image)
        {
            try
            {
                if (!cachedImages.ContainsKey(image.FileNameReal))
                {
                    AddImage(image);
                    return;
                }
                else
                {
                    cachedImages[image.FileNameReal].Load();
                    Log.Logger.Debug($"ImageManager: Image reloaded. {image}");
                }
            }
            catch
            {
                Log.Logger.Error($"ImageManager: Exception during UpdateImage! {image}");
            }
        }

        public string GetImageBase64(string image, StreamDeckType deckType)
        {
            try
            {
                var imageDef = new ImageDefinition(image, deckType, false);
                if (cachedImages.TryGetValue(imageDef.FileNameReal, out ImageDefinition value))
                {
                    return value.GetImageBase64();
                }
                else
                {
                    Log.Logger.Debug($"ImageManager: Could not find cached Image for GetImageBase64, added new Defintion. {imageDef}");
                    AddImage(imageDef);
                    return cachedImages[imageDef.FileNameReal].GetImageBase64();
                }
            }
            catch
            {
                Log.Logger.Error($"ImageManager: Exception during GetImageBase64! {image}");
                return "";
            }
        }

        public Image GetImageObject(string image, StreamDeckType deckType)
        {
            try
            {
                var imageDef = new ImageDefinition(image, deckType, false);
                if (cachedImages.TryGetValue(imageDef.FileNameReal, out ImageDefinition value))
                {
                    return value.GetImageObject();
                }
                else
                {
                    Log.Logger.Error($"ImageManager: Could not find Image for ImageObject. {imageDef}");
                    AddImage(imageDef);
                    return cachedImages[imageDef.FileNameReal].GetImageObject();
                }
            }
            catch
            {
                Log.Logger.Error($"ImageManager: Exception during GetImageObject! {image}");
                return null;
            }
        }

        public void Dispose()
        {
            InvalidateCaches();
        }

        public void InvalidateCaches()
        {
            cachedImages.Clear();
        }
    }
}
