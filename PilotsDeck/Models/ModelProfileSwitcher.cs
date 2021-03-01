using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using StreamDeckLib.Messages;

namespace PilotsDeck
{
    public enum StreamDeckType
    {
        StreamDeck,
        StreamDeckMini,
        StreamDeckXL,
        StreamDeckMobile,
        CorsairGKeys
    }

    public class StreamDeckProfile
    {
        public string Name { get; set; }
        public string Mappings { get; set; } = "";
        public int Type { get; set; }
    }

    public class DeviceMapping
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public bool UseDefault { get; set; } = true;
        public string DefaultProfile { get; set; } = "";
        public List<StreamDeckProfile> Profiles { get; set; } = new List<StreamDeckProfile>();

        public DeviceMapping()
        {

        }

        public DeviceMapping(List<StreamDeckProfile> profileList)
        {
            Profiles = profileList;
        }
    }

    public class ModelProfileSwitcher
    {
        public bool EnableSwitching { get; set; } = false;
        public bool ProfilesInstalled { get; set; } = false;
        public string MappingsJson { get; set; } = "";
        [JsonIgnore]
        public List<DeviceMapping> DeviceMappings { get; set; } = new List<DeviceMapping>();


        public ModelProfileSwitcher()
        {

        }

        public void CopySettings(ModelProfileSwitcher settings)
        {
            EnableSwitching = settings.EnableSwitching;
            ProfilesInstalled = settings.ProfilesInstalled;
            MappingsJson = settings.MappingsJson;
            LoadFromJson();
            CheckDefaultProfiles();
            ExportToJson();
        }

        public void UpdateSettings(List<StreamDeckProfile> manifestProfiles, List<Info.Device> connectedDevices)
        {
            LoadFromJson();
            var newMappings = new List<DeviceMapping>();

            foreach (var device in connectedDevices)
            {
                var deviceProfiles = manifestProfiles.Where(p => p.Type == device.type);
                if (deviceProfiles.Count() > 0)
                {
                    DeviceMapping deviceMapping;
                    var existingMappings = DeviceMappings.Where(d => d.ID == device.id);
                    if (existingMappings.Count() > 0)
                        deviceMapping = existingMappings.First();
                    else
                        deviceMapping = new DeviceMapping(deviceProfiles.ToList());

                    deviceMapping.ID = device.id;
                    deviceMapping.Name = device.name;
                    deviceMapping.Type = device.type;
                    newMappings.Add(deviceMapping);
                }
            }
            
            DeviceMappings = newMappings;
            CheckDefaultProfiles();
            ExportToJson();
        }

        public void CheckDefaultProfiles()
        {
            foreach (var deviceMapping in DeviceMappings)
            {
                if (string.IsNullOrEmpty(deviceMapping.DefaultProfile))
                {
                    if (deviceMapping.Type == (int)StreamDeckType.StreamDeck || deviceMapping.Type == (int)StreamDeckType.StreamDeckXL)
                    {
                        if (deviceMapping.Type == (int)StreamDeckType.StreamDeck && deviceMapping.Profiles.Where(p => p.Name == AppSettings.deckDefaultProfile).Count() > 0)
                            deviceMapping.DefaultProfile = AppSettings.deckDefaultProfile;
                        else if (deviceMapping.Type == (int)StreamDeckType.StreamDeckXL && deviceMapping.Profiles.Where(p => p.Name == AppSettings.deckDefaultProfileXL).Count() > 0)
                            deviceMapping.DefaultProfile = AppSettings.deckDefaultProfileXL;
                        else
                            deviceMapping.UseDefault = false;
                    }
                    else
                        deviceMapping.UseDefault = false;
                }
                else if (deviceMapping.UseDefault)
                {
                    if (deviceMapping.Profiles.Where(p => p.Name == deviceMapping.DefaultProfile).Count() == 0)
                        deviceMapping.UseDefault = false;
                }
            }
        }

        public void LoadFromJson()
        {
            if (string.IsNullOrEmpty(MappingsJson))
                return;

            try
            {
                var jArray = JArray.Parse(MappingsJson).ToObject<List<DeviceMapping>>();
                DeviceMappings.Clear();
                DeviceMappings = jArray;
            }
            catch
            {
                Log.Logger.Error($"ModelProfileSwitcher:LoadFromJson - Exception while parsing Profile-List! | {MappingsJson}");
            }
        }

        public void ExportToJson()
        {
            try
            {
                MappingsJson = JsonConvert.SerializeObject(DeviceMappings);
            }
            catch
            {
                Log.Logger.Error($"ModelProfileSwitcher:ExportToJson - Exception while serializing Profile-List! | {DeviceMappings?.Count}");
            }
        }

        public static bool IsInProfile(string profile, string name)
        {
            if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(name))
                return false;

            string[] profiles = profile.Split(':');
            for (int i = 0; i < profiles.Length; i++)
                if (profiles[i] == name)
                    return true;

            return false;
        }
    }
}
