using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Binz.Core
{
    public class BinzUtil
    {
        public static List<string> GetLocalIp()
        {
            var addressList = Dns.GetHostAddresses(Dns.GetHostName());
            var ips = addressList
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(x))
                .Select(x => x.ToString())
                .OrderBy(e=>e)
                .ToList();
            return ips;
        }

        public static List<string> GetLocalIp2()
        {
            var addressList = NetworkInterface.GetAllNetworkInterfaces()
                .Select(p => p.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses);
            var ips = addressList
                .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(x.Address))
                .Select(p => p.Address.ToString())
                .OrderBy(e => e)
                .ToList();
            return ips;
        }


        public static string GetClientServiceName<T>()
        {
            var type = typeof(T);
            var nsName = type.Namespace;
            //var className = baseType.Name;

            var svcName = $"service/{nsName ?? "DefaultNs"}";
            return svcName;
        }

        public static string GetServerServiceName(Type type)
        {
            var baseType = type.BaseType;
            if(baseType == null)
            {
                baseType = type;
            }
            var nsName = baseType.Namespace;
            //var className = baseType.Name;
            var svcName = $"service/{nsName ?? "DefaultNs"}";
            return svcName;
        }

        public static string GetEnvTag()
        {
            var envName = GetEnvName();
            return $"env:{envName}";
        }


        /// <summary>
        /// 获取当前环境名
        /// </summary>
        /// <returns></returns>
        public static string GetEnvName()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env == null)
            {
                env = Environment.GetEnvironmentVariable("env");
            }

            if (env == null)
            {
                env = "Development";
            }

            return env;
        }
    }
}
