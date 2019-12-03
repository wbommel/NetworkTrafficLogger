using LiteDB;

namespace vobsoft.net.LiteDBLogger.model
{
    public class Machine
    {
        public int Id { get; set; }

        public string MachineName { get; set; }

        public LiteCollection<LocalNetworkInterface> Interfaces;
    }
}
