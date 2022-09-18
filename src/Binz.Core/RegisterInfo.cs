using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Binz.Core
{
    /// <summary>
    /// Registration information on ectd or consul
    /// </summary>
    public class RegistryInfo : ServiceInfo
    {
        // key
        public string? ServiceId { get; set; }

        /// <summary>
        /// format: service/{proto namespace}.ServiceName
        /// example: service/Proto.GreeterService.Greater
        /// </summary>
        public string ServiceName { get; set; } = BinzConstants.DefaultServiceName;

        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return $"ServiceName={ServiceName}, EnvName={EnvName}, Tags={string.Join(",", Tags)}";
        }
    }
}
