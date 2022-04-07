using Binz.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Server
{
    public class BinzConfig
    {
        public int Port { get; set; } = 9527;

        public int Level { get; set; } = 10;

        public BinzConsulConfig ConsulConfig { get; set; } = new BinzConsulConfig();
    }
}
