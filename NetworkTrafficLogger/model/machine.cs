using System.Collections.Generic;

namespace vobsoft.net.model
{
    public class Machine
    {
        public string MachineName { get; set; }

        public Dictionary<string, LocalNetworkInterface> Interfaces = new Dictionary<string, LocalNetworkInterface>();
    }
}
