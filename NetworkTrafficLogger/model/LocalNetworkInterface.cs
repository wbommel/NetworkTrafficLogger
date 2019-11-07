using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vobsoft.net.model
{
    public class LocalNetworkInterface
    {
        public int id { get; set; }

        public int MachineId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string InterfaceId { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public int Speed { get; set; }
    }
}
