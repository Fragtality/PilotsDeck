namespace PilotsDeck
{
    public class ModelProfileSwitcher
    {
        public bool EnableSwitching { get; set; } = false;
        public bool ProfilesInstalled { get; set; } = false;
        public bool UseAlphaDefault { get; set; } = true;
        public string ProfileX_Name { get; set; }
        public string ProfileY_Name { get; set; }
        public string ProfileZ_Name { get; set; }

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
