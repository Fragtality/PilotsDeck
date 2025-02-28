using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI
{
    public static class ColorStore
    {
        private static Dictionary<int, bool> Colors { get; set; } = [];
        public static int[] ColorArray { get { return [.. Colors.Select(c => c.Key)]; } }

        public static void AddDialogColors(int[] dialogColors, Color colorSelected)
        {
            Colors.TryAdd(ColorTranslator.ToOle(colorSelected), true);
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

                Colors = JsonSerializer.Deserialize<Dictionary<int, bool>>(File.ReadAllText(AppConfiguration.ColorFile), JsonOptions.JsonWriteOptions);
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
                File.WriteAllText(AppConfiguration.ColorFile, JsonSerializer.Serialize(Colors, JsonOptions.JsonWriteOptions));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static bool ShowColorDialog(Color currentColor, Action<Color> onColorSelected)
        {
            System.Windows.Forms.ColorDialog colorDialog = new()
            {
                Color = currentColor,
                CustomColors = ColorArray,
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AddDialogColors(colorDialog.CustomColors, colorDialog.Color);
                onColorSelected?.Invoke(colorDialog.Color);
                return true;
            }
            else
                return false;
        }

        public static void BindColorLabel(Label label, Action<Color> onColorSelected = null)
        {
            onColorSelected ??= (c) => label.Background = new System.Windows.Media.SolidColorBrush(c.Convert());
            label.MouseLeftButtonUp += (_, _) => ShowColorDialog((label?.Background as System.Windows.Media.SolidColorBrush)?.Color.Convert() ?? Color.White, onColorSelected);
        }
    }
}
