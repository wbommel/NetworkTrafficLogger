using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vobsoft.net.LiteDBLogger.model
{
    public class Reading
    {
        public int Id { get; set; }

        public int InterfaceId { get; set; }

        public long LogTime { get; set; }

        public long BytesReceived { get; set; }

        public long BytesSent { get; set; }
    }
}
