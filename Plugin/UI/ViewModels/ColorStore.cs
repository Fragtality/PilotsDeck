using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PilotsDeck.UI.ViewModels
{
    public static class ColorStore
    {
        private static Dictionary<int, bool> Colors { get; set; } = [];
        public static int[] ColorArray { get { return Colors.Select(c => c.Key).ToArray(); } }

        public static void AddDialogColors(int[] dialogColors, System.Drawing.Color colorSelected)
        {
            Colors.TryAdd(System.Drawing.ColorTranslator.ToOle(colorSelected), true);
            foreach (int color in dialogColors)
            {
                if (color != 16777215 && color != 0)
                    Colors.TryAdd(color, true);
            }
            Save();
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(AppConfiguration.ColorFile))
                    File.WriteAllText(AppConfiguration.ColorFile, "{}", Encoding.UTF8);

                Colors = JsonSerializer.Deserialize<Dictionary<int, bool>>(File.ReadAllText(AppConfiguration.ColorFile), AppConfiguration.GetSerializerOptions());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(AppConfiguration.ColorFile, JsonSerializer.Serialize(Colors, AppConfiguration.GetSerializerOptions()));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
