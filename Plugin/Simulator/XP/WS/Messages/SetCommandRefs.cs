using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PilotsDeck.Simulator.XP.WS.Messages
{
    public class SetCommandRefMessage(int id)
    {
#pragma warning disable IDE1006
        public int req_id { get; set; } = id;
        public string type { get; set; } = RequestType.SetCommandActive;
        public SetCommandParams @params { get; set; } = new();

        public class SetCommandParams
        {
            public List<SetCommandRef> commands { get; set; } = [];
        }

        public class SetCommandRef(long id, bool isActive, double? duration = null)
        {
            public long id { get; set; } = id;
            public bool is_active { get; set; } = isActive;
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public double? duration { get; set; } = duration;
        }

        public void AddCommandRef(long id, bool isActive, double? duration = null)
        {
            if (!@params.commands.Where(r => r.id == id).Any())
                @params.commands.Add(new(id, isActive, duration));
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class ActivateCommandRefMessage(float duration = 0)
    {
        public virtual float duration { get; set; } = duration;
#pragma warning restore
    }
}
