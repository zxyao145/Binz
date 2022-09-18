

namespace Binz.Core
{
    /// <summary>
    /// RPC Server info
    /// </summary>
    public class ServiceInfo
    {
        public ServiceInfo()
        {
            EnvName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? BinzConstants.DefaultEnvName;
        }

        public ServiceInfo(string serviceIp, int servicePort, string? envName = null)
        {
            ServiceIp = serviceIp;
            ServicePort = servicePort;
            if (envName == null)
            {
                envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? BinzConstants.DefaultEnvName;
            }
            EnvName = envName;
        }

        public string ServiceIp { get; set; } = BinzConstants.DefaultServiceIp;

        public int ServicePort { get; set; } = BinzConstants.DefaultPort;

        public string EnvName { get; set; }
    }
}
