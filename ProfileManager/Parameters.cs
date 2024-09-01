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
        public static readonly string STREAMDECK_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Elgato\StreamDeck";
        public static readonly string SD_PROFILE_PATH = $@"{STREAMDECK_PATH}\ProfilesV2";
        public static readonly string SD_PROFILE_MANIFEST = "manifest.json";
        public static readonly string SD_REG_PATH = @"HKEY_CURRENT_USER\SOFTWARE\Elgato Systems GmbH\StreamDeck";
        public static readonly string SD_REG_VALUE_FOLDER = "Folder";
        public static readonly string SD_DEFAULT_FOLDER = @"C:\Program Files\Elgato\";
        public static readonly string SD_BINARY_NAME = "StreamDeck";
        public static readonly string SD_BINARY_EXE = $"{SD_BINARY_NAME}.exe";
        public static readonly string SD_WINDOW_NAME = "Stream Deck";
        public static readonly string SD_PROFILE_EXTENSION = ".streamDeckProfile";

        public static readonly string PLUGIN_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        public static readonly string PLUGIN_UUID = "com.extension.pilotsdeck";   
        public static readonly string PLUGIN_PATH = $@"{STREAMDECK_PATH}\Plugins\{PLUGIN_UUID}.sdPlugin";
        public static readonly string PLUGIN_PROFILE_FOLDER = "Profiles";
        public static readonly string PLUGIN_IMAGE_FOLDER = "Images";
        public static readonly string PLUGIN_IMAGE_EXT = ".png";
        public static readonly string PLUGIN_SCRIPTS_FOLDER = "Scripts";
        public static readonly string PLUGIN_SCRIPTS_EXT = ".lua";
        public static readonly string PLUGIN_WORK_FOLDER = "_work";
        public static readonly string PLUGIN_PROFILE_PATH = $@"{PLUGIN_PATH}\{PLUGIN_PROFILE_FOLDER}";
        public static readonly string PLUGIN_IMAGE_PATH = $@"{PLUGIN_PATH}\{PLUGIN_IMAGE_FOLDER}";
        public static readonly string PLUGIN_SCRIPTS_PATH = $@"{PLUGIN_PATH}\{PLUGIN_SCRIPTS_FOLDER}";
        public static readonly string PROFILE_WORK_PATH = $@"{PLUGIN_PROFILE_PATH}\{PLUGIN_WORK_FOLDER}";
        public static readonly string PLUGIN_TO_WORK_PATH = $@"\{PLUGIN_PROFILE_FOLDER}\{PLUGIN_WORK_FOLDER}";

        public static readonly string PLUGIN_MAPPING_DEVICEINFO = "DeviceInfo.json";
        public static readonly string PLUGIN_MAPPING_FILE = "ProfileMappings.json";

        public static readonly string PACKAGE_EXTENSION_NAME = "PilotsDeck Profile Package";
        public static readonly string PACKAGE_EXTENSION = ".ppp";
        public static readonly string PACKAGE_JSON_FILE = "package.json";
    }
}
