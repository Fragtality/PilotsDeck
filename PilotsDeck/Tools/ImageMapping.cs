using System;
using System.Collections.Generic;
using System.IO;

namespace PilotsDeck
{
    public class ImageMapping
    {
        public string MapString { get; set; }
        public ManagedImageScript MapScript { get; protected set; } = null;
        public string ScriptParam { get; protected set; } = "";
        public ImageManager ImgManager { get; protected set; } = Plugin.ActionController.imgManager;
        public ScriptManager ScriptManager { get; protected set; } = Plugin.ActionController.ipcManager.ScriptManager;
        public ValueManager ValueManager { get; protected set; }
        public StreamDeckType DeckType { get; set; }
        public bool IsStringMap { get { return !IPCTools.rxLuaFile.IsMatch(MapString); } }

        public ImageMapping(string strMap, StreamDeckType deckType, ValueManager valueManager)
        {
            MapString = strMap;
            DeckType = deckType;
            ValueManager = valueManager;
            RegisterMap();
        }

        public static string GetFunctionParam(string address)
        {
            if (address.StartsWith("lua:", StringComparison.InvariantCultureIgnoreCase))
                address = address.Replace("lua:", "", StringComparison.InvariantCultureIgnoreCase);
            string[] parts = address.Split(':');
            if (parts.Length == 2)
                return parts[1];
            else
                return "";
        }

        public void RegisterMap()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MapString))
                    return;
                else if (IsStringMap)
                    ManageImageMap(true);
                else
                {
                    string scriptFile = ScriptManager.GetRealFileName(MapString);
                    if (File.Exists(ScriptManager.ImageScriptFolder + scriptFile))
                    {
                        MapScript = ScriptManager.RegisterImageScript(scriptFile);
                        foreach (var image in MapScript.ImagedIDs)
                            ImgManager.AddImage(image.Value, DeckType);

                        foreach (var value in MapScript.Variables)
                            if (!value.Value)
                                ValueManager.AddDynamicValue(value.Key);

                        ScriptParam = GetFunctionParam(MapString);
                    }
                    else
                        Logger.Log(LogLevel.Warning, "ImageMapping:RegisterMap", $"The Script File '{scriptFile}' does not exist! (MapString: '{MapString}')");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ImageMapping:RegisterMap", $"Exception '{ex.GetType()}' while registering ImageMap '{MapString}': {ex.Message}");
            }
        }

        public void DeregisterMap()
        {
            try
            {
                if (IsStringMap && !string.IsNullOrWhiteSpace(MapString))
                    ManageImageMap(false);
                else if (MapScript != null)
                {
                    foreach (var image in MapScript.ImagedIDs)
                        ImgManager.RemoveImage(image.Value, DeckType);

                    foreach (var value in MapScript.Variables)
                        if (!value.Value)
                            ValueManager.RemoveDynamicValue(value.Key);

                    ScriptManager.DeregisterImageScript(MapScript.FileName);
                    ScriptParam = "";
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ImageMapping:RegisterMap", $"Exception '{ex.GetType()}' while registering ImageMap '{MapString}': {ex.Message}");
            }
        }

        public void UpdateMap(string newMap, StreamDeckType deckType)
        {
            DeregisterMap();
            MapString = newMap;
            DeckType = deckType;
            RegisterMap();
        }

        protected void ManageImageMap(bool addImages)
        {
            if (string.IsNullOrEmpty(MapString))
                return;

            string[] parts = MapString.Split(':');
            foreach (var part in parts)
            {
                string[] mapping = part.Split('=');
                if (mapping.Length == 2 && !string.IsNullOrWhiteSpace(mapping[0]) && !string.IsNullOrEmpty(mapping[1]))
                {
                    if (addImages)
                        ImgManager.AddImage($"Images/{mapping[1]}.png", DeckType);
                    else
                        ImgManager.RemoveImage($"Images/{mapping[1]}.png", DeckType);
                }
            }
        }

        public ManagedImage GetMappedImage(string currentValue, string defaultImage)
        {
            string mappedImage = GetMappedImageString(currentValue);
            if (!string.IsNullOrEmpty(mappedImage))
                return ImgManager.GetImage(mappedImage, DeckType);
            else
                return ImgManager.GetImage(defaultImage, DeckType);
        }

        public string GetMappedImageString(string currentValue, string defaultImage)
        {
            string mappedImage = GetMappedImageString(currentValue);
            if (!string.IsNullOrEmpty(mappedImage))
                return mappedImage;
            else
                return defaultImage;
        }

        public string GetMappedImageString(string currentValue)
        {
            string result = "";

            try
            {
                if (IsStringMap && !string.IsNullOrWhiteSpace(MapString))
                {
                    string mapResult = ModelDisplayText.GetValueMapped(currentValue, MapString);
                    if (mapResult != currentValue)
                        result = $"Images/{mapResult}.png";
                }
                else if (MapScript != null)
                    result =  MapScript.GetImage(currentValue, ScriptParam);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ImageMapping:GetMappedImage", $"Exception '{ex.GetType()}' getting mapped Image for Mapping '{MapString}': {ex.Message}");
            }

            return result;
        }

        public static void RefUpdateHelper(Dictionary<int, ImageMapping> mapRefs, int id, bool settingUseMap, string mapStr, StreamDeckType deckType, ValueManager valueManager)
        {
            if (mapRefs.TryGetValue(id, out ImageMapping map))
            {
                if (settingUseMap)
                {
                    if (map == null)
                    {
                        map = new(mapStr, deckType, valueManager);
                        mapRefs[id] = map;
                    }
                    else if (map.MapString != mapStr)
                        map.UpdateMap(mapStr, deckType);
                }
                else
                {
                    map?.DeregisterMap();
                    mapRefs.Remove(id);
                }
            }
            else if (settingUseMap)
            {
                map = new(mapStr, deckType, valueManager);
                mapRefs.Add(id, map);
            }
        }
    }
}
