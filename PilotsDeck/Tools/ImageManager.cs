using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using Serilog;

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
            using (MemoryStream stream = new MemoryStream(ImageBytes))
            {
                return Image.FromStream(stream);
            }
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
            if (!(obj is ImageDefinition objAsImage)) return false;
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

    public class ImageManager : IDisposable
    {
        //private Dictionary<string, byte[]> cachedImageFiles = new Dictionary<string, byte[]>(); //filename -> base64
        //private Dictionary<string, int> currentRegistrations = new Dictionary<string, int>(); //filename -> count
        private Dictionary<string, ImageDefinition> cachedImages = new Dictionary<string, ImageDefinition>(); //realname -> obj

        //private static readonly string suffixXL = AppSettings.hqImageSuffix;

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
        //public ImageManager(string[] imageFiles)
        //{
        //    try
        //    {
        //        foreach (var image in imageFiles)
        //            cachedImageFiles.Add(image, StreamDeckTools.ReadImageBytes(image));
        //    }
        //    catch
        //    {
        //        Log.Logger.Error("ImageManager: Exception while loading ImageFiles");
        //    }
        //}

        //public bool IsCached(string image)
        //{
        //    return cachedImageFiles.ContainsKey(image);
        //}

        public string GetImageBase64(string image, StreamDeckType deckType)
        {
            try
            {
                var imageDef = new ImageDefinition(image, deckType, false);
                if (cachedImages.ContainsKey(imageDef.FileNameReal))
                {
                    return cachedImages[imageDef.FileNameReal].GetImageBase64();
                }
                else
                {
                    Log.Logger.Error($"ImageManager: Could not find Image for Base64 Encode. {imageDef}");
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
                if (cachedImages.ContainsKey(imageDef.FileNameReal))
                {
                    return cachedImages[imageDef.FileNameReal].GetImageObject();
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

        //public static string GetImageNameByDeck(string image, StreamDeckType deckType)
        //{
        //    if (image.Contains(suffixXL) && deckType != StreamDeckType.StreamDeckXL)
        //        image = image.Replace(suffixXL, "");

        //    switch (deckType)
        //    {
        //        case StreamDeckType.StreamDeckXL:
        //            if (!image.Contains(suffixXL))
        //            {
        //                int idx = image.IndexOf(".png");
        //                image = image.Insert(idx, suffixXL);
        //            }
        //            break;
        //        default:
        //            break;
        //    }

        //    return image;
        //}

        //public byte[] GetImageBytes(string image, StreamDeckType deckType)
        //{
        //    image = GetImageNameByDeck(image, deckType);

        //    if (!cachedImageFiles.ContainsKey(image))
        //    {
        //        if (AddImage(image, deckType))
        //        {
        //            Log.Logger.Debug($"ImageManager: Cached new Image {image} for deckType {deckType}");
        //            return cachedImageFiles[image];
        //        }
        //        return cachedImageFiles.Values.First();
        //    }
        //    else if (image.Length > 0)
        //        return cachedImageFiles[image];
        //    else
        //        return cachedImageFiles.Values.First();
        //}

        //public static byte[] ReadImageBytes(string image)
        //{
        //    return File.ReadAllBytes(image);
        //}

        //protected byte[] LoadImage(string image)
        //{
        //    if (File.Exists(image))
        //        return File.ReadAllBytes(image);
        //    else if (image.Contains(suffixXL) && File.Exists(image.Replace(suffixXL, "")))
        //        return File.ReadAllBytes(image.Replace(suffixXL, ""));
        //    else
        //        throw new FileNotFoundException();
        //}



        //public bool AddImage(string image, StreamDeckType deckType)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(image))
        //        {
        //            Log.Logger.Error($"AddImage: Empty image string!");
        //        }

        //        image = GetImageNameByDeck(image, deckType);
        //        if (!cachedImageFiles.ContainsKey(GetImageNameByDeck(image, deckType)))
        //        {
        //            cachedImageFiles.Add(image, LoadImage(image));
        //            currentRegistrations.Add(image, 1);
        //        }
        //        else
        //            currentRegistrations[image]++;

        //        return true;
        //    }
        //    catch
        //    {
        //        Log.Logger.Error($"AddImage: Exception while loading Image {image} for deckType {deckType}");
        //        return false;
        //    }
        //}

        //public void UpdateImage(string image, StreamDeckType deckType)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(image))
        //        {
        //            Log.Logger.Error($"UpdateImage: Empty image string!");
        //        }

        //        image = GetImageNameByDeck(image, deckType);
        //        if (cachedImageFiles.ContainsKey(image))
        //            cachedImageFiles[image] = LoadImage(image);
        //    }
        //    catch
        //    {
        //        Log.Logger.Error($"UpdateImage: Exception while loading Image {image} for deckType {deckType}");
        //    }
        //}

        //public void RemoveImage(string image, StreamDeckType deckType)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(image))
        //        {
        //            Log.Logger.Error($"RemoveImage: Empty image string!");
        //        }

        //        image = GetImageNameByDeck(image, deckType);
        //        if (cachedImageFiles.ContainsKey(image))
        //        {
        //            if (currentRegistrations[image] == 1)
        //            {
        //                currentRegistrations.Remove(image);
        //                cachedImageFiles.Remove(image);

        //                Log.Logger.Debug($"RemoveImage: Image removed from cache [{image}] for deckType {deckType}");
        //            }
        //            else
        //            {
        //                currentRegistrations[image]--;
        //                Log.Logger.Debug($"RemoveImage: Registration removed from image [{image}] - Registrations open: {currentRegistrations[image]}");
        //            }
        //        }
        //        else
        //            Log.Logger.Error($"RemoveImage: Image was not found {image}");
        //    }
        //    catch
        //    {
        //        Log.Logger.Error($"RemoveImage: Exception while loading Image {image}");
        //    }
        //}
    }
}
