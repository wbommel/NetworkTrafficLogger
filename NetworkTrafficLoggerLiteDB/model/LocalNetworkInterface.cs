using LiteDB;

namespace vobsoft.net.LiteDBLogger.model
{
    public class LocalNetworkInterface
    {
        public int Id { get; set; }

        public int MachineId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string InterfaceGUID { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public long Speed { get; set; }

        public LiteCollection<Reading> Readings { get; set; }
    }
}
