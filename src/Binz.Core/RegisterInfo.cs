using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    public class RegisterInfo : ServiceInfo
    {
        public string? ServiceId { get; set; }

        public string ServiceName { get; set; } = BinzConstants.DefaultServiceName;

        public string EnvName { get; set; } = BinzConstants.DefaultEnvName;

        public int Weight { get; set; } = BinzConstants.DefaultWeight;

        public int Level { get; set; } = BinzConstants.DefaultLevel;

        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }
}
