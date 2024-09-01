using PilotsDeck.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PilotsDeck.Resources.Images
{
    public enum ImageVariant
    {
        DEFAULT,
        DEFAULT_HQ,
        PLUS
    }

    public static class ToolsImage
    {
        public static string FileInfoString(string file, ManagedImage image)
        {
            return $"(Ref: {file}) (Registrations: {image?.Registrations}) (Loaded: {image?.BaseFile})";
        }

        public static string FormatImagePath(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                if (!file.EndsWith(AppConfiguration.ImageExtension))
                    file = $"{file}{AppConfiguration.ImageExtension}";
                if (!file.StartsWith(AppConfiguration.DirImages))
                    file = $"{AppConfiguration.DirImages}{file}";
            }

            return file;
        }

        public static Dictionary<ImageVariant, string> GetSuffixDictionary()
        {
            return new()
            {
                { ImageVariant.DEFAULT_HQ, AppConfiguration.ImageSuffixHq },
                { ImageVariant.PLUS, AppConfiguration.ImageSuffixPlus },
            };
        }

        public static bool GetImageVariant(string file, string suffix, out string result)
        {
            result = null;

            try
            {
                file = Sys.InsertImageQualitySuffix(file, suffix);
                if (File.Exists(file))
                    result = file;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = null;
            }

            return result != null;
        }

        public static bool LoadImageBytes(string file, out byte[] bytes)
        {
            bytes = null;
            try
            {
                bytes = File.ReadAllBytes(file);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return bytes != null && bytes.Length > 0;
        }

        public static Image GetImageObjectFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            return Image.FromStream(stream);
        }

        public static string ReadImageFile64(string file)
        {
            string result = "";

            try
            {
                result = Convert.ToBase64String(File.ReadAllBytes(file), Base64FormattingOptions.None);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public static string ReadImageFile64(byte[] bytes)
        {
            string result = "";

            try
            {
                result = Convert.ToBase64String(bytes, Base64FormattingOptions.None);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }
    }
}
