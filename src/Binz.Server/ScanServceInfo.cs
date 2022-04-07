using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Server
{
    internal class ScanServceInfo
    {
        public ScanServceInfo(string serviceName)
        {
            ServiceName = serviceName;
        }
        public string ServiceName { get; set; }


    }
}
