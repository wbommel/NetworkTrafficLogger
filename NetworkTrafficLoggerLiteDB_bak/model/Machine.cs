using System.Collections.Generic;

namespace vobsoft.net.LiteDBLogger.model
{
    public class Machine
    {
        public int Id { get; set; }

        public string MachineName { get; set; }

        public Dictionary<string, LocalNetworkInterface> Interfaces = new Dictionary<string, LocalNetworkInterface>();
    }
}
