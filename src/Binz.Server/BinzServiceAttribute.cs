using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Server
{
    [AttributeUsage(
        AttributeTargets.Class,
        AllowMultiple = true,
        Inherited = false)
        ]
    public class BinzServiceAttribute : Attribute
    {
        public string ServiceName { get; }

        public BinzServiceAttribute()
        {
            ServiceName = string.Empty;
        }

        public BinzServiceAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }


    }
}
