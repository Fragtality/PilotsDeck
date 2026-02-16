using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PilotsDeck.UI.ActionDesignerUI.ColorStore
{
    public class ColorStoreJson
    {
        public virtual List<int> ColorStore { get; set; } = [];
        public virtual Queue<int> RecentColors { get; set; } = [];

        public static ColorStoreJson Load()
        {
            try
            {
                if (!File.Exists(AppConfiguration.ColorFile))
                {
                    Logger.Information("Initializing ColorStore");
                    File.WriteAllText(AppConfiguration.ColorFile, JsonSerializer.Serialize(new ColorStoreJson(), JsonOptions.JsonWriteOptions), Encoding.UTF8);
                }

                string json = File.ReadAllText(AppConfiguration.ColorFile);
                if (json.Length < 4 || !json.Contains("\"ColorStore\":"))
                {
                    Logger.Information("Resetting ColorStore - unexpected Format");
                    File.WriteAllText(AppConfiguration.ColorFile, JsonSerializer.Serialize(new ColorStoreJson(), JsonOptions.JsonWriteOptions), Encoding.UTF8);
                    json = File.ReadAllText(AppConfiguration.ColorFile);
                }

                return JsonSerializer.Deserialize<ColorStoreJson>(json, JsonOptions.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return new ColorStoreJson();
            }
        }

        public virtual bool Save()
        {
            bool result;

            try
            {
                File.WriteAllText(AppConfiguration.ColorFile, JsonSerializer.Serialize(this, JsonOptions.JsonWriteOptions), Encoding.UTF8);
                result = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        public virtual IEnumerable<System.Windows.Media.Color> GetCustomColors()
        {
            return [.. ColorStore.Select(c => ColorTranslator.FromOle(c).Convert())];
        }

        public virtual IEnumerable<System.Windows.Media.Color> GetRecentColors()
        {
            return [.. RecentColors.Select(c => ColorTranslator.FromOle(c).Convert())];
        }

        public virtual bool AddCustomColor(Color color)
        {
            bool result = false;
            try
            {
                var c = ColorTranslator.ToOle(color);
                if (!ColorStore.Contains(c))
                {
                    ColorStore.Add(c);
                    result = Save();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        public virtual bool RemoveCustomColor(Color color)
        {
            bool result;
            try
            {
                result = ColorStore.Remove(ColorTranslator.ToOle(color)) && Save();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        public virtual void AddRecentColor(Color color)
        {
            try
            {
                var c = ColorTranslator.ToOle(color);
                if (!RecentColors.Contains(c))
                    RecentColors.Enqueue(c);

                if (RecentColors.Count > App.Configuration.RecentColorQueue)
                {
                    RecentColors.Dequeue();
                }

                Save();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
