using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using Serilog;

namespace PilotsDeck
{
    public class ModelProfileSwitcher
    {
        public class Profile
        {
            public string Name { get; set; }
            public string Mappings { get; set; }
        }

        public bool EnableSwitching { get; set; } = false;
        public bool ProfilesInstalled { get; set; } = false;
        public bool UseDefault { get; set; } = true;
        public string DefaultProfile { get; set; } = AppSettings.deckDefaultProfile;
        public string MappingsJson { get; set; } = "";
        [JsonIgnore]
        public List<Profile> ProfileMappings { get; set; } = new List<Profile>();


        public ModelProfileSwitcher()
        {

        }

        public void UpdateSettings(List<string> manifestProfiles)
        {
            LoadFromJson();

            foreach (var profile in manifestProfiles)
            {
                if (ProfileMappings.Where(p => p.Name == profile).Count() > 0)
                    continue;
                else
                    ProfileMappings.Add(new Profile() { Name = profile, Mappings = "" });
            }

            foreach (var profile in ProfileMappings.ToList())
            {
                if (manifestProfiles.Contains(profile.Name))
                    continue;
                else
                    ProfileMappings.RemoveAll(p => p.Name == profile.Name);
            }

            
            if (UseDefault && ProfileMappings.Where(p => p.Name == DefaultProfile).Count() == 0)
            {
                if (ProfileMappings.Where(p => p.Name == AppSettings.deckDefaultProfile).Count() > 0)
                    DefaultProfile = AppSettings.deckDefaultProfile;
                else
                    UseDefault = false;
            }

            ExportToJson();
        }

        public void LoadFromJson()
        {
            if (string.IsNullOrEmpty(MappingsJson))
                return;

            try
            {
                var jArray = JArray.Parse(MappingsJson).ToObject<List<Profile>>();
                ProfileMappings.Clear();
                ProfileMappings = jArray;
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
                MappingsJson = JsonConvert.SerializeObject(ProfileMappings);
            }
            catch
            {
                Log.Logger.Error($"ModelProfileSwitcher:ExportToJson - Exception while serializing Profile-List! | {ProfileMappings?.Count}");
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
