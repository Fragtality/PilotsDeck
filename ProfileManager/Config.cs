using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using Serilog;
using System;
using System.IO;

namespace ProfileManager
{
    public class Config : ConfigBase, ILoggerConfig
    {
        public static Config Instance { get; } = new Config();

        public string LogDirectory { get { return "log"; } }
        public string LogFile { get { return "ProfileManager.log"; } }
        public RollingInterval LogInterval { get { return RollingInterval.Infinite; } }
        public int SizeLimit { get { return 1024 * 1024; } }
        public int LogCount { get { return 1; } }
        public string LogTemplate { get { return "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message} {NewLine}"; } }
        public LogLevel LogLevel { get { return LogLevel.Debug; } }

        //ConfigBase
        public override string ProductName { get { return PluginBinary; } }
        public static string PluginBinary { get { return "PilotsDeck"; } }
        public override string ProductConfigFile { get { return $"PluginConfig.json"; } }
        public override string ProductConfigPath { get { return Path.Combine(ProductPath, ProductConfigFile); } }
        public override string ProductExePath { get { return Path.Combine(ProductPath, ProductExe); } }
        public override string ProductPath { get { return GetPluginPath(); } }
        public virtual string ProductPathProfiles { get { return Path.Combine(ProductPath, "Profiles"); } }
        public virtual string ProductPathScripts { get { return Path.Combine(ProductPath, "Scripts"); } }

        private static string GetPluginPath()
        {
            // Try HotSpot path first
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string hotSpotPath = Path.Combine(appDataPath, "HotSpot", "StreamDock", "plugins", "com.extension.pilotsdeck.sdPlugin");

            if (Directory.Exists(hotSpotPath))
                return hotSpotPath;

            // Fallback to StreamDeck path
            return Path.Combine(FuncStreamDeck.DeckPluginPath, "com.extension.pilotsdeck.sdPlugin");
        }
    }
}
