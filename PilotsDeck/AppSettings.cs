using System;
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

        public static readonly string fontDefault = Convert.ToString(ConfigurationManager.AppSettings["fontDefault"]);
        public static readonly string fontBold = Convert.ToString(ConfigurationManager.AppSettings["fontBold"]);
        public static readonly string fontItalic = Convert.ToString(ConfigurationManager.AppSettings["fontItalic"]);
    }
}
