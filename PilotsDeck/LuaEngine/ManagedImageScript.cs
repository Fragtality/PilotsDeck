using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PilotsDeck
{
    public class ManagedImageScript(string file) : ManagedScript(file)
    {
        public virtual Dictionary<int, string> ImagedIDs { get; protected set; } = [];

        protected override void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.SimVar = new Func<string, bool>(RegisterVariable);
            _env.SimRead = new Func<string, double>(SimRead);
            _env.SimReadString = new Func<string, string>(SimReadString);
            _env.Log = new Action<string>(WriteLog);
            _env.MapImage = new Func<string, int, bool>(MapImage);
        }

        public override void Stop()
        {
            base.Stop();
            ImagedIDs.Clear();
        }

        protected virtual bool MapImage(string image, int id)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                Logger.Log(LogLevel.Warning, "ManagedImageScript:MapImage", $"Empty Image File Name provided by Script File '{FileName}'");
                return false;
            }

            if (!image.StartsWith("Images/"))
                image = $"Images/{image}";

            if (!image.EndsWith(".png"))
                image = $"{image}.png";

            if (ImagedIDs.Any(kv => kv.Value == image))
            {
                Logger.Log(LogLevel.Warning, "ManagedImageScript:MapImage", $"The Image '{image}' was already mapped by Script File '{FileName}'");
                return false;
            }

            ImagedIDs.Add(id, image);
            Logger.Log(LogLevel.Debug, "ManagedImageScript:MapImage", $"The Image '{image}' is mapped in Script File '{FileName}'");
            return true;
        }

        public string GetImage(string currentValue, string funcParam)
        {
            string image = "";

            try
            {
                if (currentValue != null)
                {
                    LuaResult luaResult = DoChunkWithResult($"return GetMappedImage('{currentValue}', '{funcParam}')");
                    if (luaResult != null && luaResult.Count >= 1 && int.TryParse(luaResult[0].ToString(), CultureInfo.InvariantCulture.NumberFormat, out int id))
                    {
                        if (!ImagedIDs.TryGetValue(id, out image))
                        {
                            image = "";
                            Logger.Log(LogLevel.Warning, "ManagedImageScript:GetImage", $"The returned Image ID '{id}' is not mapped in File '{FileName}'!");
                        }
                    }
                    else
                        Logger.Log(LogLevel.Warning, "ManagedImageScript:GetImage", $"Script '{FileName}' did not return a valid ID for Value '{currentValue}'!");
                }
                else
                    Logger.Log(LogLevel.Warning, "ManagedImageScript:GetImage", $"The current Value is null! ('{FileName}')");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, "ManagedImageScript:GetImage", $"Exception '{ex.GetType()}' getting mapped Image in File '{FileName}': {ex.Message}");
            }

            return image;
        }
    }
}
