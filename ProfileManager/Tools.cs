using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace ProfileManager
{
    public static partial class Tools
    {  
        public static void SetButtonImage(Image image, string icon)
        {
            image.Source = new BitmapImage(new Uri($"pack://application:,,,/ProfileManager;component/images/{icon}.png"));
        }

        public static void OpenUriArgs(string url, string args, string directory = null)
        {
            Logger.Debug($"url: '{url}' | args: '{args}' | directory '{directory}'");

            Sys.StartProcess(url, directory, args);
        }

        public static void OpenUri(object sender, RequestNavigateEventArgs e, string directory = null)
        {
            Logger.Debug($"e: '{e}' | sender: '{sender}' | uri '{e.Uri}' | directory '{directory}'");

            if (!e.Uri.ToString().Contains(".exe"))
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
            else
            {
                Sys.StartProcess(e.Uri.AbsolutePath, directory);
            }
            e.Handled = true;
        }

        public static string CreateMD5(string input)
        {
            StringBuilder sb = new();
            try
            {
                byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
                
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return sb.ToString().ToUpper();
        }

        public static readonly Regex rxNumberMatch = new(@"\D*(\d+)\D*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string[] CleanNumbers(string[] versions)
        {
            for (int i = 0; i < versions.Length; i++)
            {
                var match = rxNumberMatch.Match(versions[i]);
                if (match?.Groups?.Count == 2 && !string.IsNullOrWhiteSpace(match?.Groups[1]?.Value))
                    versions[i] = match.Groups[1].Value;
                else
                    return null;
            }

            return versions;
        }

        public enum VersionCompare
        {
            EQUAL = 1,
            LESS,
            LESS_EQUAL,
            GREATER,
            GREATER_EQUAL
        }

        public static bool CheckVersion(string leftVersion, VersionCompare comparison, string rightVersion, out bool compareable, int digits = 3)
        {
            compareable = false;

            if (string.IsNullOrWhiteSpace(leftVersion) || string.IsNullOrWhiteSpace(rightVersion))
                return false;

            string[] leftParts = leftVersion.Split('.');
            string[] rightParts = rightVersion.Split('.');
            if (leftParts.Length < digits || rightParts.Length < digits)
                return false;

            leftParts = CleanNumbers(leftParts);
            rightParts = CleanNumbers(rightParts);
            if (leftParts == null || rightParts == null)
                return false;

            leftVersion = string.Join(".", leftParts);
            rightVersion = string.Join(".", rightParts);
            if (!Version.TryParse(leftVersion, out Version left) || !Version.TryParse(rightVersion, out Version right))
                return false;

            compareable = true;
            return comparison switch
            {
                VersionCompare.LESS => left < right,
                VersionCompare.LESS_EQUAL => left <= right,
                VersionCompare.GREATER => left > right,
                VersionCompare.GREATER_EQUAL => left >= right,
                _ => left == right,
            };
        }
    }
}
