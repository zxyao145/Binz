using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Core
{
    public class BinzException : Exception
    {
        public BinzException(string msg): base(msg)
        {
        }

        public BinzException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        public static BinzException ServiceCannotBrFound = new BinzException("service cannot be found");
    }
}
