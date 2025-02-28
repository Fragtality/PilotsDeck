using CFIT.AppLogger;
using Neo.IronLua;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PilotsDeck.Resources.Scripts
{
    public class ManagedImageScript(string file, Serilog.Core.Logger log) : ManagedScript(file, log)
    {
        public virtual Dictionary<int, string> ImagedIDs { get; protected set; } = [];

        protected override void CreateEnvironment()
        {
            LuaEnv = LuaEngine.CreateEnvironment<LuaGlobal>();
            dynamic _env = LuaEnv;
            _env.GetAircraft = App.SimController.AircraftString;
            _env.SimVar = new Func<string, bool>(RegisterVariable);
            _env.SimRead = new Func<string, dynamic>(SimRead);
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
                Logger.Warning($"Empty Image File Name provided by Script File '{FileName}'");
                return false;
            }

            if (!image.StartsWith("Images/"))
                image = $"Images/{image}";

            if (!image.EndsWith(".png"))
                image = $"{image}.png";

            if (ImagedIDs.Any(kv => kv.Value == image))
            {
                Logger.Warning($"The Image '{image}' was already mapped by Script File '{FileName}'");
                return false;
            }

            ImagedIDs.Add(id, image);
            Logger.Debug($"The Image '{image}' is mapped in Script File '{FileName}'");
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
                            Logger.Warning($"The returned Image ID '{id}' is not mapped in File '{FileName}'!");
                        }
                    }
                    else
                        Logger.Warning($"Script '{FileName}' did not return a valid ID for Value '{currentValue}'!");
                }
                else
                    Logger.Warning($"The current Value is null! ('{FileName}')");
            }
            catch (LuaRuntimeException ex)
            {
                Log.Fatal(ScriptManager.FormatLogMessage(FileName, $"{ex.GetType()}: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return image;
        }
    }
}
