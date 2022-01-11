using System;
using System.Globalization;
using System.Configuration;

namespace PilotsDeck
{
    public static class AppSettings
    {
        public static readonly string groupStringRead = "PilotsdeckRead";
        public static readonly string groupStringWrite = "PilotsdeckWrite";
        public static readonly string deckDefaultProfile = "Profiles/PilotsDeck - Default";
        public static readonly string deckDefaultProfileXL = "Profiles/PilotsDeck - DefaultXL";

        public static readonly string waitImage = @"Images/Wait.png";
        public static readonly string hqImageSuffix = "@2x";

        public static readonly int pollInterval = Convert.ToInt32(ConfigurationManager.AppSettings["pollInterval"]);
        public static readonly int waitTicks = Convert.ToInt32(ConfigurationManager.AppSettings["waitTicks"]);
        public static readonly int longPressTicks = Convert.ToInt32(ConfigurationManager.AppSettings["longPressTicks"]);
        public static readonly int appStartDelay = Convert.ToInt32(ConfigurationManager.AppSettings["appStartDelay"]);
        public static readonly int controlDelay = Convert.ToInt32(ConfigurationManager.AppSettings["controlDelay"]);

        public static readonly string stringReplace = Convert.ToString(ConfigurationManager.AppSettings["stringReplace"]);
        public static readonly bool forceDecimalPoint = Convert.ToBoolean(ConfigurationManager.AppSettings["forceDecimalPoint"]);
        public static NumberFormatInfo numberFormat
        {
            get
            {
                if (forceDecimalPoint)
                    return new CultureInfo("en-US").NumberFormat;
                else
                    return new CultureInfo("de-DE").NumberFormat;
            }
        }

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
