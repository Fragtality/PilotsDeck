using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class SubscribeDataRefMessage
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = 0;
        public string type { get; set; } = "dataref_subscribe_values";
        public SubscribeParams @params { get; set; } = new();


        public class SubscribeParams
        {
            public List<SubscribeDataRef> datarefs { get; set; } = [];

        }

        public class SubscribeDataRef
        {
            public int id { get; set; } = 0;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string name { get; set; } = null;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? index { get; set; } = null;

            public SubscribeDataRef(int id, int? index = null)
            {
                this.id = id;
                this.index = index;
            }
        }
#pragma warning restore

        public SubscribeDataRefMessage(int id)
        {
            req_id = id;
        }

        public void AddDataRef(int id, int? index = null)
        {
            @params.datarefs.Add(new(id, index));
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
