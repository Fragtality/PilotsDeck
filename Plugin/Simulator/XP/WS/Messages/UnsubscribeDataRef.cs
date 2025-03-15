using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class UnsubscribeDataRefMessage(int id)
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = id;
        public string type { get; set; } = RequestType.UnsubscribeDataRef;
        public UnsubscribeParams @params { get; set; } = new();


        public class UnsubscribeParams
        {
            public List<UnsubscribeDataRef> datarefs { get; set; } = [];

        }

        public class UnsubscribeDataRef(long id, int? index = null)
        {
            public long id { get; set; } = id;
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

    public class UnsubscribeAllRefMessage(int id)
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = id;
        public string type { get; set; } = RequestType.UnsubscribeDataRef;
        public UnsubscribeAllParams @params { get; set; } = new();

        public class UnsubscribeAllParams
        {
            public string datarefs { get; set; } = "all";

        }
#pragma warning restore

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
