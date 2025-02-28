using CFIT.AppLogger;
using PilotsDeck.StreamDeck.Messages;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PilotsDeck.Actions.Advanced.SettingsModel
{
    public class SettingsModelMeta
    {
        public SettingsModelMeta()
        {
            FillDictionaries();
        }

        public bool FillDictionaries()
        {
            bool result = false;

            if (ActionCommands.Count == 0)
            {
                foreach (StreamDeckCommand type in Enum.GetValues(typeof(StreamDeckCommand)))
                    ActionCommands.Add(type, []);
                result = true;
            }

            if (ActionDelays.Count == 0)
            {
                foreach (StreamDeckCommand type in Enum.GetValues(typeof(StreamDeckCommand)))
                    ActionDelays.Add(type, App.Configuration.InterActionDelay);
                result = true;
            }

            return result;
        }

        public void ClearDictionaries()
        {
            ActionCommands.Clear();
            ActionDelays.Clear();
        }

        public static SettingsModelMeta Create(StreamDeckEvent sdEvent, out bool updated)
        {
            var settings = (sdEvent?.payload?.settings?[AppConfiguration.ModelAdvanced]).Deserialize<SettingsModelMeta>();
            settings ??= new SettingsModelMeta();
            updated = false;

            settings.FillDictionaries();

            if (settings.BUILD_VERSION < AppConfiguration.BuildModelVersion)
            {
                if (settings.BUILD_VERSION < 7)
                {
                    foreach (var model in settings.DisplayElements.Values)
                    {
                        var oldSize = model.FontSize;
                        var newSize = AppConfiguration.FontSizeConversionModern(oldSize);
                        model.FontSize = newSize;
                        Logger.Debug($"Changed 'FontSize' {oldSize} => {model.FontSize}");
                    }
                }

                Logger.Information($"Converted Settings for '{sdEvent.context}' from Version {settings.BUILD_VERSION} to {AppConfiguration.BuildModelVersion}");
                settings.BUILD_VERSION = AppConfiguration.BuildModelVersion;
                updated = true;
            }

            return settings;
        }

        public virtual JsonNode Serialize()
        {
            return new JsonObject()
            {
                [AppConfiguration.ModelAdvanced] = JsonSerializer.SerializeToNode(this)
            };
        }

        public virtual void SetSize(PointF size)
        {
            CanvasSize[0] = size.X;
            CanvasSize[1] = size.Y;
        }

        public virtual PointF GetSize()
        {
            return new PointF(CanvasSize[0], CanvasSize[1]);
        }

        public virtual bool IsNewModel { get; set; } = true;
        public virtual int BUILD_VERSION { get; set; } = AppConfiguration.BuildModelVersion;
        public virtual float[] CanvasSize { get; set; } = [100, 100];
        public virtual SortedDictionary<int, ModelDisplayElement> DisplayElements { get; set; } = [];
        public virtual SortedDictionary<StreamDeckCommand, SortedDictionary<int, ModelCommand>> ActionCommands { get; set; } = [];
        public virtual SortedDictionary<StreamDeckCommand, int> ActionDelays { get; set; } = [];
    }
}
