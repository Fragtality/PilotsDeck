using CFIT.AppTools;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public enum XpApiValueType
    {
        UNKNOWN = 0,
        FLOAT = 1,
        DOUBLE = 2,
        INT = 3,
        INT_ARRAY = 4,
        FLOAT_ARRAY = 5,
        DATA = 6
    }

    public class DataRef
    {
        [JsonIgnore]
        public static Dictionary<string, XpApiValueType> TypeMapping { get; } = new()
        {
            { "float", XpApiValueType.FLOAT },
            { "double", XpApiValueType.DOUBLE },
            { "int", XpApiValueType.INT },
            { "int_array", XpApiValueType.INT_ARRAY },
            { "float_array", XpApiValueType.FLOAT_ARRAY },
            { "data", XpApiValueType.DATA },
        };

#pragma warning disable IDE1006
        public long id { get; set; } = 0;
        public bool is_writable { get; set; } = false;
        public string name { get; set; } = "";
        public string value_type { get; set; } = "";
#pragma warning restore

        [JsonIgnore]
        public XpApiValueType ValueTypeApi => (TypeMapping.TryGetValue(value_type, out var type) ? type : XpApiValueType.UNKNOWN);

        public static DataRef GetDataRef(string json)
        {
            return JsonSerializer.Deserialize<DataRef>(json);
        }
    }

    public class DataRefList
    {
#pragma warning disable IDE1006
        public List<DataRef> data { get; set; } = [];
#pragma warning restore

        public static DataRefList GetDataRefList(string json)
        {
            return JsonSerializer.Deserialize<DataRefList>(json);
        }
    }

    public class DataRefValue
    {
#pragma warning disable IDE1006
        public object data { get; set; }
#pragma warning restore

        public static DataRefValue CreateNumber(double value)
        {
            return new() { data = value };
        }

        public static DataRefValue CreateString(string value)
        {
            return new() { data = value.Base64Encode() };
        }
    }
}
