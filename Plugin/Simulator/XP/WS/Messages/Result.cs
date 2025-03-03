using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class Result
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = 0;
        public string type { get; set; } = "";
        public bool success { get; set; } = false;
        public object data { get; set; } = null;
#pragma warning restore

        public static Result GetResult(string json)
        {
            return JsonSerializer.Deserialize<Result>(json);
        }
    }

    public class UpdateMessage
    {
#pragma warning disable IDE1006
        public string type { get; set; } = "";
        public bool success { get; set; } = false;
        public Dictionary<string, object> data { get; set; } = null;
#pragma warning restore

        public static UpdateMessage GetUpdateMessage(string json)
        {
            return JsonSerializer.Deserialize<UpdateMessage>(json);
        }
    }
}
