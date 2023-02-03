using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImportProfiles
{
    public class Profile
    {
        public string Name { get; set; } = string.Empty;
        public int DeviceType { get; set; } = 0;
        public bool ReadOnly { get; set; } = false;
        public bool DontAutoSwitchWhenInstalled { get; set; } = true;
    }


    public class Importer
    {
        protected readonly static string dir = Directory.GetCurrentDirectory();
        protected readonly static string manifest = dir + @"\manifest.json";
        protected readonly static string savedProfileFile = dir + @"\Profiles\savedProfiles.txt";
        private static List<Profile> profilesManifest = new();
        private readonly static Dictionary<string, int> savedProfiles = new();

        public static void Main()
        {
            if (File.Exists(manifest) && Directory.Exists(dir + @"\Profiles"))
            {
                //Load
                var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(manifest));
                profilesManifest = JsonConvert.DeserializeObject<List<Profile>>(jsonObj["Profiles"].ToString());

                if (File.Exists(savedProfileFile))
                {
                    string[] lines = File.ReadAllLines(savedProfileFile);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(':');
                        savedProfiles.Add(parts[0], int.Parse(parts[1]));
                    }
                }

                ImportProfiles();

                //Write
                jsonObj["Profiles"] = profilesManifest;
                string strJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(manifest, strJson);
                
                List<string> save = new();
                foreach (var prof in savedProfiles)
                    save.Add($"{prof.Key}:{prof.Value}");
                File.WriteAllLines(savedProfileFile, save.ToArray());
            }


        }

        public static void ImportProfiles()
        {
            string[] result = Directory.GetFiles(dir + @"\Profiles", "*.streamDeckProfile");
            var fileList = new List<string>();
            result.ToList().ForEach(f => fileList.Add("Profiles/" + Path.GetFileNameWithoutExtension(f)));
            var removeList = new List<string>();
            profilesManifest.ForEach(p => removeList.Add(p.Name));

            foreach (var file in fileList)
            {
                string name = file;
                var entry = profilesManifest.Where(p => p.Name == name);
                if (!entry.Any())
                {
                    int type = -1;
                    if (savedProfiles.TryGetValue(file, out type))
                    {
                        AddProfile(file, type, false, type != 9);
                    }
                    else
                    {
                        type = AskDeviceType(file);
                        if (type != -1)
                        {
                            AddProfile(file, type, true, type != 9);
                        }
                    }
                }
                else
                {
                    removeList.Remove(name);
                }
            }

            Console.Clear();
            foreach (var remove in removeList)
            {
                if (profilesManifest.RemoveAll(p => p.Name == remove) > 0)
                    Console.WriteLine($"Profile \"{remove}\" removed from manifest.");

            }
        }

        public static void AddProfile(string name, int type, bool addSaved, bool addManifest)
        {
            if (addManifest)
            {
                var prof = new Profile()
                {
                    Name = name,
                    DeviceType = type,
                    ReadOnly = false,
                    DontAutoSwitchWhenInstalled = true
                };

                profilesManifest.Add(prof);
                Console.WriteLine($"Profile \"{name}\" added to manifest.");
            }

            if (addSaved)
            {
                savedProfiles.Add(name, type);
                Console.WriteLine($"Profile \"{name}\" added to saved Profiles.");
            }
        }

        public static int AskDeviceType(string file)
        {
            int result = -1;

            while (result == -1)
            {
                Console.Clear();
                Console.WriteLine($"StreamDeck Type for Profile \"{file}\":");
                Console.WriteLine("[0] = StreamDeck (15 Key)");
                //Console.WriteLine("[1] = StreamDeck Mini");
                Console.WriteLine("[2] = StreamDeck XL");
                //Console.WriteLine("[3] = StreamDeck Mobile");
                Console.WriteLine("[7] = StreamDeck Plus");
                Console.WriteLine("[9] = Ignore Profile");
                Console.Write(">> ");
                
                if (int.TryParse(Console.ReadKey().KeyChar.ToString(), out int input) && (input == 0 || input == 2 || input == 7 || input == 9))
                {
                    return input;
                }  
            }

            return result;
        }
    }
}