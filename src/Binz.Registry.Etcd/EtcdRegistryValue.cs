using Binz.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Registry.Etcd
{
    public class EtcdRegistryValue
    {
        public string? ServiceAddress { get; set; }

        public string EnvName { get; set; } = BinzUtil.GetEnvName();

        public int Port { get; set; }

        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        public int Version { get; set; } = 1;
    }
}
