using System;
using System.Reflection;

namespace ProfileManager
{
    public enum StreamDeckTypeEnum
    {
        UNKNOWN = -1,
        StreamDeck = 0,
        StreamDeckMini = 1,
        StreamDeckXL = 2,
        StreamDeckMobile = 3,
        CorsairGKeys = 4,
        StreamDeckPlus = 7
    }

    public enum SimulatorType
    {
        UNKNOWN = -1,
        FSX = 0,
        P3D = 1,
        MSFS = 2,
        XP = 3,
    }

    public class Parameters
    {
        private static string _streamDeckPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Elgato\StreamDeck";
        public static string STREAMDECK_PATH
        {
            get => _streamDeckPath;
            set => _streamDeckPath = value;
        }
        public static string SD_PROFILE_PATH => $@"{_streamDeckPath}\{(DOCK_PROFILE_ENABLED ? "profiles" : "ProfilesV3")}";
        public static string SD_PROFILE_MANIFEST = "manifest.json";
        public static string SD_WINDOW_NAME { get; private set; } = "Stream Deck";
        public static string SD_PROFILE_EXTENSION { get; private set; } = ".streamDeckProfile";
        public static bool DOCK_PROFILE_ENABLED { get; private set; } = false;
        public static readonly string PLUGIN_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        public static readonly string PLUGIN_UUID = "com.extension.pilotsdeck";
        public static string PLUGIN_PATH => $@"{STREAMDECK_PATH}\Plugins\{PLUGIN_UUID}.sdPlugin";
        public static readonly string PLUGIN_PROFILE_FOLDER = "Profiles";
        public static readonly string PLUGIN_IMAGE_FOLDER = "Images";
        public static readonly string PLUGIN_IMAGE_EXT = ".png";
        public static readonly string PLUGIN_SCRIPTS_FOLDER = "Scripts";
        public static readonly string PLUGIN_SCRIPTS_EXT = ".lua";
        public static readonly string PLUGIN_WORK_FOLDER = "_work";
        public static string PLUGIN_PROFILE_PATH => $@"{PLUGIN_PATH}\{PLUGIN_PROFILE_FOLDER}";
        public static string PLUGIN_IMAGE_PATH => $@"{PLUGIN_PATH}\{PLUGIN_IMAGE_FOLDER}";
        public static string PLUGIN_SCRIPTS_PATH => $@"{PLUGIN_PATH}\{PLUGIN_SCRIPTS_FOLDER}";
        public static string PROFILE_WORK_PATH => $@"{PLUGIN_PROFILE_PATH}\{PLUGIN_WORK_FOLDER}";
        public static string PLUGIN_TO_WORK_PATH => $@"\{PLUGIN_PROFILE_FOLDER}\{PLUGIN_WORK_FOLDER}";

        public static readonly string PLUGIN_MAPPING_DEVICEINFO = "DeviceInfo.json";
        public static readonly string PLUGIN_MAPPING_FILE = "ProfileMappings.json";

        public static readonly int PACKAGE_BUILDVERSION = 1;
        public static readonly string PACKAGE_EXTENSION_NAME = "PilotsDeck Profile Package";
        public static readonly string PACKAGE_EXTENSION = ".ppp";
        public static readonly string PACKAGE_JSON_FILE = "package.json";
        public static readonly string PACKAGE_PATH_EXTRAS = "Extras";
        public static void ChangeHotSpotMode()
        {
            DOCK_PROFILE_ENABLED = true;
            _streamDeckPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\HotSpot\StreamDock";
            SD_WINDOW_NAME = "VSD Craft";
            SD_PROFILE_EXTENSION = ".SDProfile";
        }

        public static bool IsStreamDockMode => DOCK_PROFILE_ENABLED;
        public static string PlatformName => DOCK_PROFILE_ENABLED ? "StreamDock" : "StreamDeck";
        public static string PlatformSoftwareName => DOCK_PROFILE_ENABLED ? "VSD Craft" : "Stream Deck";

    }
}
