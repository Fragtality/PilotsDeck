using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PilotsDeck
{
    public class ImageManager : IDisposable
    {
        protected Dictionary<string, ManagedImage> imageCache = []; //RealFileName to Image
        public int Length => imageCache.Count;

        protected static string InsertSuffix(string file, string suffix)
        {
            if (!string.IsNullOrWhiteSpace(file) && !string.IsNullOrWhiteSpace(suffix))
            {
                int idx = file.IndexOf(".png");
                return file.Insert(idx, suffix);
            }
            else
                return file;
        }

        protected static string FindHqFile(string file, string suffix, out bool foundHq)
        {
            string result;
            foundHq = false;

            if (!file.Contains(suffix))
            {
                result = InsertSuffix(file, suffix);
                if (!File.Exists(result))
                {
                    if (File.Exists(file))
                        result = file;
                    else
                        result = InsertSuffix(AppSettings.waitImage, suffix);
                }
                else
                    foundHq = true;
            }
            else
            {
                result = file;
                if (!File.Exists(result))
                {
                    result = AppSettings.waitImage;
                }
                else
                    foundHq = true;
            }

            return result;
        }

        protected static string GetRealFileName(string file, StreamDeckType type)
        {
            string result = AppSettings.waitImage;
            try
            {
                if (type.IsEncoder)
                {
                    result = FindHqFile(file, AppSettings.plusHqImageSuffix, out bool foundHq);
                    if (!foundHq)
                    {
                        result = FindHqFile(file, AppSettings.plusImageSuffix, out foundHq);
                        if (!foundHq)
                            result = FindHqFile(file, "", out _);
                    }
                }
                else
                {
                    if (type.Type == StreamDeckTypeEnum.StreamDeckXL || type.Type == StreamDeckTypeEnum.StreamDeckPlus)
                    {
                        result = FindHqFile(file, AppSettings.hqImageSuffix, out bool foundHq);
                        if (!foundHq)
                            result = FindHqFile(file, "", out _);
                    }
                    else
                        result = FindHqFile(file, "", out _);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ImageManager:GetRealFileName", $"Exception while finding correct FileName to use! (Name: {file}) (Deck: {type.Type}) {(type.IsEncoder ? "(Encoder) " : "")}(Exception: {ex.GetType()})");
            }

            return result;
        }

        public ManagedImage GetImage(string file, StreamDeckType type)
        {
            string fileReal = GetRealFileName(file, type);
            if (!imageCache.TryGetValue(fileReal, out ManagedImage image))
            {
                image = new ManagedImage(fileReal);
                Logger.Log(LogLevel.Error, "ImageManager:GetImage", $"Image is not found in Cache! (Ref: {file}) (Real: {fileReal}) (Deck: {type.Type}){(type.IsEncoder ? " (Encoder) " : "")}");
            }

            return image;
        }

        public ManagedImage AddImage(string file, StreamDeckType type)
        {
            string fileReal = GetRealFileName(file, type);
            if (imageCache.TryGetValue(fileReal, out ManagedImage image))
            {
                image.Registrations++;
                Logger.Log(LogLevel.Debug, "ImageManager:AddImage", $"Registration added to cached Image. (Ref: {file}) (Real: {fileReal}) (Registrations: {image.Registrations}) (Deck: {type.Type}){(type.IsEncoder ? " (Encoder) " : "")}");
            }
            else
            {
                image = new ManagedImage(fileReal)
                {
                    Registrations = 1
                };
                if (image.IsLoaded)
                {
                    imageCache.Add(fileReal, image);
                    Logger.Log(LogLevel.Debug, "ImageManager:AddImage", $"New Image added to Cache. (Ref: {file}) (Real: {fileReal}) (Deck: {type.Type}){(type.IsEncoder ? " (Encoder) " : "")}");
                }
                else
                    Logger.Log(LogLevel.Error, "ImageManager:AddImage", $"New Image could not be added because it has not loaded! (Ref: {file}) (Real: {fileReal}) (Deck: {type.Type}){(type.IsEncoder ? " (Encoder) " : "")}");
            }

            return image;
        }

        public void RemoveImage(string file, StreamDeckType type)
        {
            string fileReal = GetRealFileName(file, type);
            if (imageCache.TryGetValue(fileReal, out ManagedImage image))
            {
                image.Registrations--;
                Logger.Log(LogLevel.Debug, "ImageManager:RemoveImage", $"Registration removed from cached Image. (Ref: {file}) (Real: {fileReal}) (Registrations: {image.Registrations}) (Deck: {type.Type}){(type.IsEncoder ? " (Encoder) " : "")}");
            }
            else
            {
                Logger.Log(LogLevel.Error, "ImageManager:RemoveImage", $"Image is not found in Cache! (Ref: {file}) (Real: {fileReal}) (Deck: {type.Type}){(type.IsEncoder ? " (Encoder) " : "")}");
            }
        }

        public int RemoveUnused()
        {
            int result = 0;
            var unusedImages = imageCache.Values.Where(i => i.Registrations == 0).ToList();
            
            if (unusedImages.Count != 0)
                Logger.Log(LogLevel.Information, "ImageManager:RemoveUnused", $"Removing {unusedImages.Count} unused Images ...");
            
            foreach (var image in unusedImages)
            {
                imageCache[image.FileName] = null;
                imageCache.Remove(image.FileName);
                result++;
                Logger.Log(LogLevel.Debug, "ImageManager:RemoveUnused", $"Removed Image {image.FileName} from Cache.");
            }

            return result;
        }

        public void RemoveAll()
        {
            imageCache.Clear();
        }

        public void Dispose()
        {
            RemoveAll();
            GC.SuppressFinalize(this);
        }
    }
}
