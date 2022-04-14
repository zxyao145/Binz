using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    public class ServiceInfo
    {
        public ServiceInfo()
        {

        }

        public ServiceInfo(string serviceIp, int servicePort)
        {
            ServiceIp = serviceIp;
            ServicePort = servicePort;
        }

        public string ServiceIp { get; set; } = BinzConstants.DefaultServiceIp;

        public int ServicePort { get; set; } = BinzConstants.DefaultPort;

    }
}
