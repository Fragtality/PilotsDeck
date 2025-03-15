using System.Collections.Generic;
using System.Text.Json;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class CommandRef
    {
#pragma warning disable IDE1006
        public long id { get; set; } = 0;
        public string name { get; set; } = "";
        public string description { get; set; } = "";
#pragma warning restore

        public static CommandRef GetCommand(string json)
        {
            return JsonSerializer.Deserialize<CommandRef>(json);
        }
    }

    public class CommandRefList
    {
#pragma warning disable IDE1006
        public List<CommandRef> data { get; set; } = [];
#pragma warning restore

        public static CommandRefList GetCommandList(string json)
        {
            return JsonSerializer.Deserialize<CommandRefList>(json);
        }
    }
}
