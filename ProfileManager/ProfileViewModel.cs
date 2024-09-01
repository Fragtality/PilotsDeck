using ProfileManager.json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProfileManager
{
    public class ProfileViewModel
    {
        public ProfileManifest Manifest { get; protected set; }
        public bool HasMapping { get { return Mapping != null; } }
        public ProfileMapping Mapping { get; protected set; }
        public bool IsChanged { get { return Manifest.IsChanged || Manifest.DeleteFlag || (HasMapping && (Mapping.IsChanged || Mapping.DeleteFlag)); } }
        public bool IsMappedProfile { get { return HasMapping && IsPreparedForSwitching && !ProfileNever; } }
        public string ProfileName { get { return Manifest.ProfileName; } }
        public string DeviceInfo { get { return $"@ {Manifest.Device.DeckName} ({Manifest.Device.Hash})"; } }
        public string DeckName { get { return Manifest.Device.DeckName; } }
        public string ProfileDirectory { get { return Manifest.ProfileDirectory; } }
        public bool IsPreparedForSwitching { get { return Manifest.IsPreparedForSwitching; } }
        public bool ProfileNever { get {  return !HasMapping || (HasMapping && Mapping.IsProfileNever); } }
        public bool ProfileDefault { get { return HasMapping && Mapping.DefaultProfile; }  }
        public int DefaultSimulator { get { return (HasMapping ? (int)Mapping.DefaultSimulator : (int)SimulatorType.UNKNOWN) + 1; } }
        public bool ProfileAircraft { get { return HasMapping && Mapping.AircraftProfile; } }
        public ObservableCollection<string> AircraftCollection { get { return GetAircraftCollection(); } }

        public ProfileViewModel(ProfileManifest manifest)
        {
            Manifest = manifest;
            Mapping = manifest.ProfileMapping;

            UpdateModel();
        }

        public override string ToString()
        {
            return $"Name {Manifest.Device.DeckName} | DeckID {Manifest.Device.Hash} | ProfileName {ProfileName} | PreconfiguredName {Manifest.PreconfiguredName} | HasMapping {HasMapping} ";
        }

        protected ObservableCollection<string> GetAircraftCollection()
        {
            if (HasMapping)
                return new(Mapping.AircraftStrings);
            else
                return [];
        }

        public void SetProfileNever(bool value)
        {
            if (value && HasMapping)
            {
                bool oldValue = ProfileNever;
                Mapping.DefaultProfile = false;
                Mapping.AircraftProfile = false;
                UpdateModel(oldValue != ProfileNever);
            }
        }

        public void SetProfileDefault(bool value)
        {
            if (HasMapping)
            {
                bool oldValue = Mapping.DefaultProfile;
                Mapping.DefaultProfile = value;
                Mapping.AircraftProfile = !value;
                UpdateModel(oldValue != Mapping.DefaultProfile);
            }
            else
            {
                PrepareProfileForSwitching();
                Mapping.DefaultProfile = value;
                Mapping.AircraftProfile = !value;
                UpdateModel();
            }
        }

        public void SetProfileAircraft(bool value)
        {
            if (HasMapping)
            {
                bool oldValue = Mapping.AircraftProfile;
                Mapping.DefaultProfile = !value;
                Mapping.AircraftProfile = value;
                UpdateModel(oldValue != Mapping.AircraftProfile);
            }
            else
            {
                PrepareProfileForSwitching();
                Mapping.DefaultProfile = !value;
                Mapping.AircraftProfile = value;
                UpdateModel();
            }
        }

        public void SetDefaultSimulator(int value)
        {
            SetDefaultSimulator((SimulatorType)(value - 1));
        }

        public void SetDefaultSimulator(SimulatorType value)
        {
            if (!HasMapping)
                return;

            var oldSetting = Mapping.DefaultSimulator;
            Mapping.DefaultSimulator = value;
            UpdateModel(oldSetting != Mapping.DefaultSimulator);
        }

        public void CopyAircraftList(List<string> aircraftList)
        {
            if (!HasMapping)
                return;

            Mapping.AircraftStrings = new(aircraftList);
            Mapping.IsChanged = true;
            UpdateModel();
        }

        public List<string> GetNewList()
        {
            List<string> list = [];
            
            if (HasMapping)
                foreach(var aircraft in Mapping.AircraftStrings)
                    list.Add(aircraft);

            return list;
        }

        public void UpdateModel(bool setChangedMapping = false)
        {
            bool changedBefore = IsChanged;
            if (HasMapping)
            {
                if (setChangedMapping)
                {
                    Mapping.IsChanged = setChangedMapping;
                    Logger.Log(LogLevel.Debug, $"setChangedMapping was true");
                }

                if (Mapping.DefaultSimulator < SimulatorType.UNKNOWN || Mapping.DefaultSimulator > SimulatorType.XP)
                {
                    Logger.Log(LogLevel.Warning, $"Unknown SimulatorType - corrected to Unknown! @ {this}");
                    Mapping.DefaultSimulator = SimulatorType.UNKNOWN;
                    Mapping.IsChanged = true;
                }

                if (Mapping.DefaultProfile && Mapping.AircraftProfile)
                    Logger.Log(LogLevel.Error, $"Illegal ProfileState - both true! (Count: {Mapping.AircraftStrings.Count}) @ {this}");
            }
            if (changedBefore || IsChanged)
                Logger.Log(LogLevel.Debug, $"ViewModel Updated {changedBefore} -> {IsChanged} @ {this}");
        }

        public void PrepareProfileForSwitching()
        {
            Mapping = new();
            if (Manifest.ProfileController.ProfileMappings.Where(m => m.ProfileUUID == Manifest.ProfileDirectoryCleaned).Any())
            {
                Mapping = Manifest.ProfileController.ProfileMappings.Where(m => m.ProfileUUID == Manifest.ProfileDirectoryCleaned).First();
                Logger.Log(LogLevel.Information, $"Reused existing Mapping  @ {Manifest}");
            }
            else
            {
                Manifest.ProfileController.ProfileMappings.Add(Mapping);
                Logger.Log(LogLevel.Information, $"Added new Mapping  @ {Manifest}");
            }

            Mapping.SetCheckManifest(Manifest);
            Mapping.IsChanged = true;

            Manifest.InstalledByPluginUUID = Parameters.PLUGIN_UUID;
            Logger.Log(LogLevel.Information, $"Corrected UUID  @ {Manifest}");
            Manifest.IsChanged = true;

            
        }

        public void SetProfileName(string name)
        {
            Manifest.ProfileName = name;
            Manifest.IsChanged = true;
            Logger.Log(LogLevel.Information, $"Set new ProfileName @ {Manifest}");

            if (HasMapping)
                Mapping.SetCheckManifest(Manifest);
        }

        public void AircraftAdd(string Aircraft)
        {
            if (!HasMapping)
            {
                Logger.Log(LogLevel.Error, $"Calling while Mapping is NOT set @ {Mapping}");
                return;
            }

            Mapping.AircraftStrings.Add(Aircraft);
            Mapping.IsChanged = true;
            Logger.Log(LogLevel.Information, $"Added new Aircraft @ {Mapping}");
        }

        public void AircraftRemove(string Aircraft)
        {
            if (!HasMapping)
            {
                Logger.Log(LogLevel.Error, $"Calling while Mapping is NOT set @ {Mapping}");
                return;
            }

            Mapping.AircraftStrings.Remove(Aircraft);
            Mapping.IsChanged = true;
            Logger.Log(LogLevel.Information, $"Removed Aircraft @ {Mapping}");
        }

        public void ToggleDeleteFlag()
        {
            Manifest.DeleteFlag = !Manifest.DeleteFlag;
            Logger.Log(LogLevel.Debug, $"Toggled Delete Flag -> {Manifest.DeleteFlag} @ {Manifest}");

            if (HasMapping)
            {
                Mapping.DeleteFlag = Manifest.DeleteFlag;
                Logger.Log(LogLevel.Debug, $"Toggled Delete Flag -> {Mapping.DeleteFlag} @ {Mapping}");
            }
        }
    }
}
