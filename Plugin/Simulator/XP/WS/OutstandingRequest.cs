namespace PilotsDeck.Simulator.XP.WS
{
    public class OutstandingRequest
    {
        public int ID { get; set; } = 0;
        public string Type { get; set; } = "";
        public object RequestData { get; set; } = new();

        public static OutstandingRequest Create(int id, object data, string type)
        {
            return new()
            {
                ID = id,
                Type = type,
                RequestData = data,
            };
        }
    }
}
