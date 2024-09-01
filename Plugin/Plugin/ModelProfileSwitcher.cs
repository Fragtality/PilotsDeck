using PilotsDeck.StreamDeck.Messages;
using PilotsDeck.Tools;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PilotsDeck.Plugin
{
    public class ModelGlobalSettings
    {
        public bool EnableSwitching { get; set; } = true;
        public bool SwitchBack { get; set; } = true;

        public JsonNode Serialize()
        {
            return new JsonObject()
            {
                [AppConfiguration.ModelSimple] = JsonSerializer.SerializeToNode(this)
            };
        }

        public static ModelGlobalSettings Create(StreamDeckEvent sdEvent)
        {
            var settings = (sdEvent?.payload?.settings?[AppConfiguration.ModelSimple]).Deserialize<ModelGlobalSettings>(JsonOptions.JsonSerializerOptions);
            settings ??= new ModelGlobalSettings();
            return settings;
        }
    }

    public class ModelProfileSwitcherInspector(ModelGlobalSettings globalSettings)
    {
        public bool IsProfileSwitcherModel { get; set; } = true;
        public bool EnableSwitching { get; set; } = globalSettings.EnableSwitching;
        public bool SwitchBack { get; set; } = globalSettings.SwitchBack;
        public string AircraftPathString { get; set; } = "";
        public List<string> LoadedMappings { get; set; } = [];
    }
}
