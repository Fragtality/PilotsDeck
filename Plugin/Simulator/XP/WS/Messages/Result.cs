using System.Collections.Generic;
using System.Text.Json;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public static class ResultType
    {
        public const string Request = "result";
        public const string UpdateDataRef = "dataref_update_values";
    }

    public static class RequestType
    {
        public const string SubscribeDataRef = "dataref_subscribe_values";
        public const string UnsubscribeDataRef = "dataref_unsubscribe_values";
        public const string SetCommandActive = "command_set_is_active";
    }

    public class ResultXP
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = 0;
        public string type { get; set; } = "";
        public bool success { get; set; } = false;
        public object data { get; set; } = null;
        public string error_code { get; set; } = "";
        public string error_message { get; set; } = "";
#pragma warning restore

        public static ResultXP GetResult(string json)
        {
            return JsonSerializer.Deserialize<ResultXP>(json);
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
