using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.AccessControl;

namespace Binz.Core
{
    public class BinzUtil
    {
        public static List<string> GetLocalIp()
        {
            var addressList = Dns.GetHostAddresses(Dns.GetHostName());
            var ips = addressList
                .Where(x =>
                    x.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(x)
                )
                .Select(x => x.ToString())
                .Where(x => 
                    !x.EndsWith(".1") 
                    && !x.EndsWith(".0") 
                    && !x.EndsWith(".255")
                )
                .OrderBy(e => e)
                .ToList();
            Console.WriteLine("ips:" + String.Join(",", ips));
            return ips;
        }

        public static string GetClientServiceName<T>()
        {
            var type = typeof(T);
            var svcName = $"service/{type.FullName?.Split("+")[0] ?? "DefaultNs"}";
            return svcName;
        }

        public static string GetServerServiceName(Type type)
        {
            var baseType = type.BaseType;
            if(baseType == null)
            {
                baseType = type;
            }
            //var className = baseType.Name;
            var svcName = $"service/{baseType.FullName?.Split("+")[0] ?? "DefaultNs"}";
            return svcName;
        }

        public static string GetEnvTag()
        {
            var envName = GetEnvName();
            return GetEnvTag(envName);
        }

        public static readonly string EnvTagKey = "EnvName";

        public static string GetEnvTag(string envName)
        {
            return $"{EnvTagKey}:{envName}";
        }

        private static string? _curEnvName = null;

        /// <summary>
        /// 获取当前环境名
        /// </summary>
        /// <returns></returns>
        public static string GetEnvName()
        {
            if (_curEnvName == null)
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (env == null)
                {
                    env = Environment.GetEnvironmentVariable("env");
                }
                if (env == null)
                {
                    env = EnvName.Development;
                }
                _curEnvName = env;
            }

            return _curEnvName;
        }


        /// <summary>
        /// 获取当前 容器 or 机器 名
        /// </summary>
        /// <returns></returns>
        public static string GetContainerName()
        {
            var containerName = Environment.GetEnvironmentVariable("HOSTNAME");
            if (containerName == null)
            {
                containerName = Environment.GetEnvironmentVariable("containerName");
            }
            if (containerName == null)
            {
                containerName = Dns.GetHostName();
                //containerName = Environment.MachineName;
            }
            if (containerName == null)
            {
                containerName = DateTime.Now.ToString("YYYYMMddHHmmss");
            }
            return containerName;
        }
    }
}
