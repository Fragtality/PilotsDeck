using System;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PilotsDeck.Tools
{
    public static class Img
    {
        public static string InsertImageQualitySuffix(string file, string suffix)
        {
            if (!string.IsNullOrWhiteSpace(file) && !string.IsNullOrWhiteSpace(suffix))
            {
                int idx = file.IndexOf(AppConfiguration.ImageExtension);
                return file.Insert(idx, suffix);
            }
            else
                return file;
        }

        public static System.Drawing.Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{AppConfiguration.SC_CLIENT_NAME}.UI.Icons.{filename}");
            return new System.Drawing.Icon(stream);
        }

        public static BitmapImage GetBitmapFromFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                return null;

            try
            {
                FileStream fileStream = new(file, FileMode.Open, FileAccess.Read);
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = fileStream;
                img.EndInit();
                return img;
            }
            catch
            {
                return null;
            }
        }

        public static void SetButtonImage(this Image image, string icon)
        {
            image.Source = GetAssemblyImage(icon);
        }

        public static BitmapImage GetAssemblyImage(string icon)
        {
            return new BitmapImage(new Uri($"pack://application:,,,/{AppConfiguration.SC_CLIENT_NAME};component/UI/Icons/{icon}.png"));
        }
    }
}
