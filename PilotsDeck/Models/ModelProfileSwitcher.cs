﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PilotsDeck
{
    public class StreamDeckProfile
    {
        public string Name { get; set; }
        public string Mappings { get; set; } = "";
        public int Type { get; set; }

        public StreamDeckProfile()
        {

        }

        public StreamDeckProfile(string name, int type, string mappings)
        {
            Name = name;
            Type = type;
            Mappings = mappings;
        }
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

        public DeviceMapping(string id, string name, int type)
        {
            ID = id;
            Name = name;
            Type = type;
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
                var profilesForDevice = manifestProfiles.Where(p => p.Type == device.type);
                if (profilesForDevice.Any()) //only save mappings if deckType is in use
                {
                    DeviceMapping deviceMapping;
                    var existingDevice = DeviceMappings.Where(d => d.ID == device.id);
                    if (existingDevice.Any()) //device is not new, use existing data
                    {
                        deviceMapping = existingDevice.First();
                        var newProfileList = new List<StreamDeckProfile>();
                        foreach (var oldProfile in deviceMapping.Profiles) //copy over oldProfiles, but only if they are still listed in manifest
                        {
                            if (manifestProfiles.Where(p => p.Name == oldProfile.Name).Any())
                                newProfileList.Add(oldProfile);
                        }
                        foreach (var newProfile in profilesForDevice) //add new profiles from manifest (if they not already known)
                        {
                            if (!deviceMapping.Profiles.Where(p => p.Name == newProfile.Name).Any())
                                newProfileList.Add(newProfile);
                        }
                        deviceMapping.Profiles = newProfileList;
                    }
                    else //device is new, create new data
                    {
                        deviceMapping = new DeviceMapping(device.id, device.name, device.type);
                        foreach (var profile in profilesForDevice)
                            deviceMapping.Profiles.Add(profile);
                    }

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
                    if (deviceMapping.Type == (int)StreamDeckTypeEnum.StreamDeck || deviceMapping.Type == (int)StreamDeckTypeEnum.StreamDeckXL || deviceMapping.Type == (int)StreamDeckTypeEnum.StreamDeckPlus)
                    {
                        if (deviceMapping.Type == (int)StreamDeckTypeEnum.StreamDeck && deviceMapping.Profiles.Where(p => p.Name == AppSettings.deckDefaultProfile).Any())
                            deviceMapping.DefaultProfile = AppSettings.deckDefaultProfile;
                        else if (deviceMapping.Type == (int)StreamDeckTypeEnum.StreamDeckXL && deviceMapping.Profiles.Where(p => p.Name == AppSettings.deckDefaultProfileXL).Any())
                            deviceMapping.DefaultProfile = AppSettings.deckDefaultProfileXL;
                        else if (deviceMapping.Type == (int)StreamDeckTypeEnum.StreamDeckPlus && deviceMapping.Profiles.Where(p => p.Name == AppSettings.deckDefaultProfilePlus).Any())
                            deviceMapping.DefaultProfile = AppSettings.deckDefaultProfilePlus;
                        else
                            deviceMapping.UseDefault = false;
                    }
                    else
                        deviceMapping.UseDefault = false;
                }
                else if (deviceMapping.UseDefault)
                {
                    if (!deviceMapping.Profiles.Where(p => p.Name == deviceMapping.DefaultProfile).Any())
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
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ModelProfileSwitcher:LoadFromJson", $"Exception while parsing Profile-List! (Mappings: {MappingsJson}) (Exception: {ex.GetType()})");
            }
        }

        public void ExportToJson()
        {
            try
            {
                MappingsJson = JsonConvert.SerializeObject(DeviceMappings);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ModelProfileSwitcher:ExportToJson", $"Exception while serializing Profile-List! (Count: {DeviceMappings?.Count}) (Exception: {ex.GetType()})");
            }
        }

        public static bool IsInProfile(string profile, string name)
        {
            if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(name))
                return false;

            string[] profiles = profile.Split(':');
            for (int i = 0; i < profiles.Length; i++)
                if (name.Contains(profiles[i]))
                {
                    Logger.Log(LogLevel.Information, "ModelProfileSwitcher:IsInProfile", $"Profile-Mapping '{profiles[i]}' matched to '{name}'.");
                    return true;
                }

            return false;
        }
    }
}
