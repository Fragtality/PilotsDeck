using CFIT.AppLogger;
using PilotsDeck.Actions.Simple;
using PilotsDeck.Resources.Scripts;
using PilotsDeck.Tools;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace PilotsDeck.Resources.Images
{
    public class ImageMapping
    {
        public string MapString { get; set; }
        public ManagedImageScript MapScript { get; protected set; } = null;
        public string ScriptParam { get; protected set; } = "";
        public static VariableManager VariableManager { get { return App.PluginController.VariableManager; } }
        public static ImageManager ImageManager { get { return App.PluginController.ImageManager; } }
        public static ScriptManager ScriptManager { get { return App.PluginController.ScriptManager; } }
        protected RessourceStore RessourceStore { get; set; }
        public bool IsStringMap { get { return !TypeMatching.rxLuaFile.IsMatch(MapString); } }

        public ImageMapping(string strMap, RessourceStore ressourceStore)
        {
            MapString = strMap;
            RessourceStore = ressourceStore;
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
                            ImageManager.RegisterImage(image.Value);

                        foreach (var value in MapScript.Variables)
                            if (!value.Value)
                                RessourceStore.AddDynamicVariable(value.Key);

                        ScriptParam = GetFunctionParam(MapString);
                    }
                    else
                        Logger.Warning($"The Script File '{scriptFile}' does not exist! (MapString: '{MapString}')");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
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
                        ImageManager.DeregisterImage(image.Value);

                    foreach (var value in MapScript.Variables)
                        if (!value.Value)
                            RessourceStore.RemoveDynamicVariable(value.Key);

                    ScriptManager.DeregisterImageScript(MapScript.FileName);
                    ScriptParam = "";
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public void UpdateMap(string newMap)
        {
            Logger.Debug($"Update Map - new '{newMap}' | old '{MapString}'");
            DeregisterMap();
            MapString = newMap;
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
                if (mapping.Length == 2 && !string.IsNullOrWhiteSpace(mapping[0]) && !string.IsNullOrWhiteSpace(mapping[1]))
                {

                    if (!mapping[1].StartsWith("Images/"))
                        mapping[1] = $"Images/{mapping[1]}";

                    if (!mapping[1].EndsWith(".png"))
                        mapping[1] = $"{mapping[1]}.png";

                    if (addImages)
                        ImageManager.RegisterImage(mapping[1]);
                    else
                        ImageManager.DeregisterImage(mapping[1]);
                }
            }
        }

        public ManagedImage GetMappedImage(string currentValue, string defaultImage)
        {
            Logger.Debug($"Mapping Value '{currentValue}' in Map '{MapString}'");
            string mappedImage = GetMappedImageString(currentValue);
            Logger.Debug($"Returned Image: {mappedImage}");
            if (!string.IsNullOrEmpty(mappedImage))
                return ImageManager.GetImage(mappedImage);
            else if (!string.IsNullOrEmpty(defaultImage))
                return ImageManager.GetImage(defaultImage);
            else
                return null;
        }

        protected string GetMappedImageString(string currentValue)
        {
            string result = "";

            try
            {
                if (IsStringMap && !string.IsNullOrWhiteSpace(MapString))
                    result = ValueState.GetValueMapped(currentValue, MapString);
                else if (MapScript != null)
                    result =  MapScript.GetImage(currentValue, ScriptParam);

                result = ToolsImage.FormatImagePath(result);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public static void RefUpdateHelper(ConcurrentDictionary<ImageID, ImageMapping> mapRefs, ImageID id, bool settingUseMap, string mapStr, RessourceStore ressourceStore)
        {
            if (mapRefs.TryGetValue(id, out ImageMapping map))
            {
                if (settingUseMap)
                {
                    if (map == null)
                    {
                        map = new(mapStr, ressourceStore);
                        mapRefs[id] = map;
                    }
                    else if (map.MapString != mapStr)
                        map.UpdateMap(mapStr);
                }
                else
                {
                    map?.DeregisterMap();
                    mapRefs.TryRemove(id, out _);
                }
            }
            else if (settingUseMap)
            {
                map = new(mapStr, ressourceStore);
                mapRefs.TryAdd(id, map);
            }
        }
    }
}
