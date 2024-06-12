using System.Collections.Generic;

namespace PilotsDeck
{
    public class ModelProfileSwitcher
    {
        public bool EnableSwitching { get; set; } = false;
        public string AircraftPathString { get; set; } = "";
        public List<string> LoadedMappings { get; set; } = [];

        public void CopySettings(ModelProfileSwitcher settings)
        {
            EnableSwitching = settings.EnableSwitching;
            AircraftPathString = Plugin.ActionController.SimConnector.AicraftPathString;
            LoadedMappings = Plugin.ActionController.ProfileSwitcher.GetProfileListForPI();
            LoadedMappings.Sort();
        }
    }
}
