using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class Command
    {
#pragma warning disable IDE1006
        public int id { get; set; } = 0;
        public string name { get; set; } = "";
        public string description { get; set; } = "";
#pragma warning restore

        public static Command GetCommand(string json)
        {
            return JsonSerializer.Deserialize<Command>(json);
        }
    }

    public class CommandList
    {
#pragma warning disable IDE1006
        public List<Command> data { get; set; } = [];
#pragma warning restore

        public static CommandList GetCommandList(string json)
        {
            return JsonSerializer.Deserialize<CommandList>(json);
        }
    }
}
