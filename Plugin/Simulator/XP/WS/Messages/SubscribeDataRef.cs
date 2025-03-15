using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class SubscribeDataRefMessage(int id)
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = id;
        public string type { get; set; } = RequestType.SubscribeDataRef;
        public SubscribeParams @params { get; set; } = new();


        public class SubscribeParams
        {
            public List<SubscribeDataRef> datarefs { get; set; } = [];

        }

        public class SubscribeDataRef(long id, int? index = null)
        {
            public long id { get; set; } = id;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string name { get; set; } = null;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? index { get; set; } = index;
        }

#pragma warning restore

        public void AddDataRef(long id, int? index = null)
        {
            if (!@params.datarefs.Where(r => r.id == id).Any())
                @params.datarefs.Add(new(id, index));
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
