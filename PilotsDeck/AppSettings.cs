using System;
using System.Globalization;
using System.Configuration;

namespace PilotsDeck
{
    public static class AppSettings
    {
        public static readonly string groupStringRead = "PilotsdeckRead";
        public static readonly string groupStringWrite = "PilotsdeckWrite";

        public static readonly string applicationName = Convert.ToString(ConfigurationManager.AppSettings["applicationName"]);
        public static readonly string waitImage = @"Images/Wait.png";

        public static readonly int pollInterval = Convert.ToInt32(ConfigurationManager.AppSettings["pollInterval"]);
        public static readonly int waitTicks = Convert.ToInt32(ConfigurationManager.AppSettings["waitTicks"]);

        public static readonly string stringReplace = Convert.ToString(ConfigurationManager.AppSettings["stringReplace"]);

        public static string fontDefault { get; private set; } = Convert.ToString(ConfigurationManager.AppSettings["fontDefault_en"]);
        public static string fontBold { get; private set; } = Convert.ToString(ConfigurationManager.AppSettings["fontBold_en"]);
        public static string fontItalic { get; private set; } = Convert.ToString(ConfigurationManager.AppSettings["fontItalic_en"]);
        public static string locale { get; private set; } = "en";

        public static readonly bool redrawAlways = Convert.ToBoolean(ConfigurationManager.AppSettings["redrawAlways"]);

        public static void SetLocale()
        {
            string lang = (CultureInfo.CurrentUICulture.Name).Split('-')[0];

            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get($"fontDefault_{lang}")))
            {
                fontDefault = Convert.ToString(ConfigurationManager.AppSettings[$"fontDefault_{lang}"]);
                fontBold = Convert.ToString(ConfigurationManager.AppSettings[$"fontBold_{lang}"]);
                fontItalic = Convert.ToString(ConfigurationManager.AppSettings[$"fontItalic_{lang}"]);
                locale = lang;
            }
        }
    }
}
