using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Client
{
    internal class ServiceClientCacheItem
    {
        public ServiceClientCacheItem(string serviceName)
        {
            ServiceName = serviceName;
        }

        public string ServiceName { get; private set; } 

        public DateTime LastSyncTime { get; set; } = DateTime.Now;

        public int ConnectTimeoutSecond { get; set; } = 5;
    }
}
