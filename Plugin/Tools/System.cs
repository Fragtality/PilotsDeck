using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PilotsDeck.Tools
{
    public static class Sys
    {
        public static string GetRegistryValue(string path, string value)
        {
            try
            {
                return (string)Registry.GetValue(path, value, null);
            }
            catch
            {
                return null;
            }
        }

        public static void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                e.Handled = true;
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static bool GetProcessRunning(string name)
        {
            return Process.GetProcessesByName(name).FirstOrDefault()?.ProcessName == name;
        }

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

        public static bool IsEnter(KeyEventArgs e)
        {
            if (e.Key != Key.Enter || e.Key != Key.Return)
                return false;
            else
                return true;
        }

        public static void SetButtonImage(this Image image, string icon)
        {
            image.Source = new BitmapImage(new Uri($"pack://application:,,,/{AppConfiguration.SC_CLIENT_NAME};component/UI/Icons/{icon}.png"));
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

        public static void SetClipboard(string text)
        {
            Thread thread = new(() =>
            {
                Clipboard.SetText(text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public static string GetClipboard()
        {
            string result = null;
            Thread thread = new(() =>
            {
                result = Clipboard.GetText();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }
    }
}
