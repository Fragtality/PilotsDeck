using CFIT.AppLogger;
using PilotsDeck.StreamDeck;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;

namespace PilotsDeck.Resources.Images
{
    public class ManagedImage : IManagedRessource
    {
        public virtual string UUID { get { return BaseFile; } }
        public string BaseFile { get; protected set; }
        public string RequestedFile { get; protected set; }
        public ConcurrentDictionary<ImageVariant, byte[]> ImageBytes { get; protected set; } = [];
        public ConcurrentDictionary<ImageVariant, Image> Images { get; protected set; } = [];
        public ConcurrentDictionary<ImageVariant, string> FileNames { get; protected set; } = [];
        public int Registrations { get; set; } = 1;

        public ManagedImage(string file)
        {
            RequestedFile = file;
            try
            {
                if (File.Exists(file))
                {
                    BaseFile = file;
                    AddVariant(ImageVariant.DEFAULT, BaseFile);

                    var variants = ToolsImage.GetSuffixDictionary();
                    foreach (var variant in variants)
                    {
                        if (ToolsImage.GetImageVariant(BaseFile, variant.Value, out string fileVariant))
                            AddVariant(variant.Key, fileVariant);
                    }
                }
                else
                {
                    BaseFile = AppConfiguration.WaitImage;
                    AddVariant(ImageVariant.DEFAULT, BaseFile);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                ImageBytes.Clear();
                Images.Clear();
                FileNames.Clear();
                BaseFile = AppConfiguration.WaitImage;
                AddVariant(ImageVariant.DEFAULT, BaseFile);
            }

            var baseImage = Images[ImageVariant.DEFAULT];
            var fileName = FileNames[ImageVariant.DEFAULT];
            if (!Images.ContainsKey(ImageVariant.DEFAULT_HQ) && baseImage.Width == 144 && baseImage.Height == 144)
                AddVariant(ImageVariant.DEFAULT, fileName);
            if (!Images.ContainsKey(ImageVariant.PLUS) && baseImage.Width == 200 && baseImage.Height == 100)
                AddVariant(ImageVariant.DEFAULT, fileName);
        }

        protected void AddVariant(ImageVariant variant, string file)
        {
            if (ToolsImage.LoadImageBytes(file, out byte[] bytes))
            {
                ImageBytes.TryAdd(variant, bytes);
                Images.TryAdd(variant, ToolsImage.GetImageObjectFromBytes(bytes));
                FileNames.TryAdd(variant, file);
                Logger.Verbose($"Added Image Variant: {variant} - {file}");
            }
        }

        public bool HasImageVariant(ImageVariant variant)
        {
            return Images.ContainsKey(variant);
        }

        public string GetImageVariant(StreamDeckCanvasInfo canvasInfo)
        {
            if (HasImageVariant(canvasInfo.GetImageVariant()))
                return ToolsImage.ReadImageFile64(ImageBytes[canvasInfo.GetImageVariant()]);
            else
                return ToolsImage.ReadImageFile64(ImageBytes[ImageVariant.DEFAULT]);
        }

        public Image GetImageVariant(ImageVariant variant)
        {
            if (Images.TryGetValue(variant, out Image image))
                return image;
            else
                return Images[ImageVariant.DEFAULT];
        }

        public bool HasMatchingImage(PointF canvas)
        {
            return GetMatchingImage(canvas) != null;
        }

        public bool HasMatchingImage(PointF canvas, out Image image)
        {
            image = GetMatchingImage(canvas);
            return image != null;
        }

        public Image GetMatchingImage(PointF canvas)
        {
            Image result = null;

            foreach (var image in Images.Values)
            {
                if (image.Width == canvas.X)
                {
                    result = image;
                    break;
                }
            }

            return result;
        }

        public override string ToString()
        {
            return BaseFile ?? "";
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (ImageBytes != null)
                    {
                        ImageBytes.Clear();
                        ImageBytes = null;
                    }

                    if (Images != null)
                    {
                        foreach (var image in Images.Values)
                            image.Dispose();
                        Images.Clear();
                        Images = null;
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
