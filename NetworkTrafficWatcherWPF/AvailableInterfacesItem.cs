using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vobsoft.Libraries.WPF;

namespace NetworkTrafficWatcherWPF
{
    internal class AvailableInterfacesItem : ViewModelBase
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
