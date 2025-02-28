using CFIT.AppLogger;
using CFIT.AppTools;
using PilotsDeck.Resources.Images;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck.Resources
{
    public class ImageManager : IDisposable
    {
        public static readonly string DEFAULT_ENCODER = ToolsImage.ReadImageFile64(AppConfiguration.SdEncoderBackground);
        public static readonly ManagedImage DEFAULT_WAIT = new(AppConfiguration.WaitImage);

        public ConcurrentDictionary<string, ManagedImage> ImageCache { get; protected set; } = [];
        public int Count => ImageCache.Count;

        public ManagedImage this[string file]
        {
            get { return GetImage(file); }
        }

        public ManagedImage GetImage(string file)
        {
            if (!ImageCache.TryGetValue(file, out ManagedImage image))
            {
                image = new ManagedImage(file);
                Logger.Warning($"Image was not cached! {ToolsImage.FileInfoString(file, image)}");
            }

            return image ?? DEFAULT_WAIT;
        }

        public ManagedImage RegisterImage(string file)
        {
            if (ImageCache.TryGetValue(file, out ManagedImage image))
            {
                image.AddRegistration();
                Logger.Verbose($"Registration added to cached Image. {ToolsImage.FileInfoString(file, image)}");
            }
            else
            {
                try
                {
                    image = new ManagedImage(file);
                    ImageCache.Add(file, image);
                    Logger.Verbose($"New Image added to Cache. {ToolsImage.FileInfoString(file, image)}");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }

            return image ?? DEFAULT_WAIT;
        }

        public void DeregisterImage(string file)
        {
            if (ImageCache.TryGetValue(file, out ManagedImage image))
            {
                image.RemoveRegistration();
                if (image.Registrations >= 1)
                    Logger.Verbose($"Registration removed from cached Image. {ToolsImage.FileInfoString(file, image)}");
                else
                    Logger.Verbose($"Registration removed from cached Image. {ToolsImage.FileInfoString(file, image)}");
            }
            else
            {
                Logger.Debug($"Image not found in Cache! (Ref: {file})");
            }
        }

        public int RemoveUnused()
        {
            int result = 0;
            var unusedImages = ImageCache.Values.Where(i => i.Registrations <= 0).ToList();
            
            if (unusedImages.Count != 0)
                Logger.Information($"Removing {unusedImages.Count} unused Images ...");
            
            foreach (var image in unusedImages)
            {
                ImageCache[image.RequestedFile] = null;
                ImageCache.Remove(image.RequestedFile);
                result++;
                Logger.Verbose($"Removed Image {image.RequestedFile} from Cache.");
            }

            if (App.PluginController.State == Plugin.PluginState.IDLE && !ImageCache.IsEmpty && App.PluginController.ActionManager.Count == 0 && App.PluginController.ScriptManager.CountTotal == 0)
            {
                Logger.Warning($"Force Removal of {ImageCache.Count} Images (no other Ressources active)");
                foreach (var image in ImageCache)
                {
                    Logger.Warning($"{image.Key} ({image.Value?.BaseFile}) (Registrations: {image.Value?.Registrations})");
                    image.Value?.Dispose();
                    result++;
                }
                ImageCache.Clear();
            }

            return result;
        }

        public void RemoveAll()
        {
            foreach (var image in ImageCache)
                ImageCache[image.Value.RequestedFile] = null;
            ImageCache.Clear();
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (ImageCache != null)
                    {
                        RemoveAll();
                        ImageCache = null;
                    }
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
