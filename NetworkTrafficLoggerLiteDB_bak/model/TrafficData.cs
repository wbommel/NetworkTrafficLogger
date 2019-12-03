using System.Collections.Generic;

namespace vobsoft.net.LiteDBLogger.model
{
    public class TrafficData
    {
        public int Id { get; set; }

        public Dictionary<string, Machine> Machines = new Dictionary<string, Machine>();
    }
}
