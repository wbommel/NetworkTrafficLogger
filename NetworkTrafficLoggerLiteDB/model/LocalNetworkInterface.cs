using System.Collections.Generic;

namespace vobsoft.net.LiteDBLogger.model
{
    public class LocalNetworkInterface
    {
        public int Id { get; set; }

        public int MachineId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string InterfaceId { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public long Speed { get; set; }

        public SortedDictionary<long, Reading> Readings { get; set; } = new SortedDictionary<long, Reading>();
    }
}
