using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkTrafficMonitor
{
    internal class DayReading
    {
        public long Day { get; set; }
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
    }
}
