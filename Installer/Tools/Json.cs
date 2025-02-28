using System.Text.Json;
using System.Text.Json.Serialization;

namespace Installer.Tools
{
    public static class Json
    {
        public static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
        }
    }
}
