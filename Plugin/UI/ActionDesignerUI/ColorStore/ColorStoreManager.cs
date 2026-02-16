using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PilotsDeck.UI.ActionDesignerUI.ColorStore
{
    public static class ColorStoreManager
    {
        private static ColorStoreJson CustomStore { get; set; } = new ColorStoreJson();

        public static void Load()
        {
            CustomStore = ColorStoreJson.Load();
        }

        public static void Save()
        {
            CustomStore.Save();
        }

        public static IEnumerable<System.Windows.Media.Color> GetCustomColors()
        {
            return CustomStore.GetCustomColors();
        }

        public static IEnumerable<System.Windows.Media.Color> GetRecentColors()
        {
            return CustomStore.GetRecentColors();
        }

        public static void AddCustomColor(System.Windows.Media.Color color)
        {
            CustomStore.AddCustomColor(color.Convert());
        }

        public static void RemoveCustomColor(System.Windows.Media.Color color)
        {
            CustomStore.RemoveCustomColor(color.Convert());
        }

        public static void AddRecentColor(System.Windows.Media.Color color)
        {
            CustomStore.AddRecentColor(color.Convert());
        }

        public static bool ShowColorDialog(Color currentColor, Window parent, Action<Color> onColorSelected)
        {
            DialogColor colorDialog = new(currentColor.Convert(), parent);

            if (colorDialog.ShowDialog() == true)
            {
                onColorSelected?.Invoke(colorDialog.SelectedColor);
                return true;
            }
            else
                return false;
        }

        public static void BindColorLabel(Label label, Window parent, Action<Color> onColorSelected = null)
        {
            onColorSelected ??= (c) => label.Background = new System.Windows.Media.SolidColorBrush(c.Convert());
            label.MouseLeftButtonUp += (_, _) => ShowColorDialog((label?.Background as System.Windows.Media.SolidColorBrush)?.Color.Convert() ?? Color.White, parent, onColorSelected);
        }
    }
}
