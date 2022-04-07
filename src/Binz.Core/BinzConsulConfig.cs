using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    public class BinzConsulConfig
    {
        public string Address { get; set; } = "http://localhost:8500/";

        public int HealthCheckIntervalSec { get; set; } = 10;
        public int HealthCheckTimeoutSec { get; set; } = 5;
    }
}
