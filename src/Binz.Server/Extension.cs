using Binz.Core;
using Consul;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Binz.Server
{
    public static class Extension
    {
        public static IGrpcServerBuilder AddBinzHealthCheck(this IGrpcServerBuilder grpcServerBuilder)
        {
            var services = grpcServerBuilder.Services;
            // services.AddHostedService<GrpcHealthCheckHostService>();
            services
                .AddGrpcHealthChecks()
                .AddCheck<IHealthCheckImpl>("SimpleCheck");
            //.AddCheck("Sample", () => HealthCheckResult.Healthy())

#if DEBUG
            services.AddGrpcReflection();
#endif
            return grpcServerBuilder;
        }


        /// <summary>
        /// 服务注册
        /// </summary>
        /// <param name="app"></param>
        /// <param name="scanAssembly"></param>
        /// <returns></returns>
        public static async Task RegisterBinzServer(this WebApplication app, Type scanAssembly)
        {
            app.MapGrpcHealthChecksService();
            app.MapGrpcReflectionService();

            var configuration = app.Configuration;
            var binzConfig = new BinzConfig();
            configuration.GetSection("Binz").Bind(binzConfig);

            var (localIp, port) = GetIpAndPort(binzConfig);

            Console.WriteLine($"binzserver bind {localIp}:{port}");

            var consulClient = new ConsulClient(
                x => x.Address = new Uri(binzConfig.ConsulConfig.Address)
            );
            AgentServiceCheck httpCheck = GetHealthCheck(binzConfig, localIp, port);

            var env = BinzUtil.GetEnvName();
            string containerName = GetContainerName();

            var binzSvcNames = ScanGrpcService(scanAssembly);
            var serviceIds = new List<string>(binzSvcNames.Count);
            foreach(var svcName in binzSvcNames)
            {
                var id = containerName + Guid.NewGuid().ToString().Substring(0, 6);
                serviceIds.Add(id);

                 // Register service with consul
                var registration = new AgentServiceRegistration()
                {
                    Checks = new[] { httpCheck },
                    ID = id,
                    Name = svcName,
                    Address = localIp,
                    Port = port,

                    //添加 urlprefix-/servicename 格式的 tag 标签，以便 Fabio 识别
                    Tags = new[] { $"env:{env}", $"level:{binzConfig.Level}", $"urlprefix-/{svcName}" }
                };

                // 服务启动时注册，内部实现其实就是使用 Consul API 进行注册（HttpClient发起）
                await consulClient.Agent.ServiceRegister(registration);
            }

            app.Lifetime.ApplicationStopping.Register(async () =>
            {
                //服务停止时取消注册
                foreach (var id in serviceIds)
                {
                    await consulClient.Agent.ServiceDeregister(id);
                }
            });
        }

        /// <summary>
        /// 获取 Assembly 内所有的 binz 服务
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static List<string> ScanGrpcService(Type type)
        {
            var asm = type.Assembly;
            if (asm == null)
            {
                return new List<string>();
            }

            var types = asm.GetExportedTypes();
            var binzSvcs = new List<string>();
            foreach (var item in types)
            {
                var attributes = Attribute.GetCustomAttributes(item, true);
                foreach (var attribute in attributes)
                {
                    if(attribute is BinzServiceAttribute binzServiceAttribute)
                    {
                        var name = binzServiceAttribute.ServiceName;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = BinzUtil.GetServerServiceName(item);
                        }
                        binzSvcs.Add(name);
                    }
                }
            }   

            return binzSvcs;
        }

        /// <summary>
        /// 获取当前 容器 or 机器 名
        /// </summary>
        /// <returns></returns>
        private static string GetContainerName()
        {
            var containerName = Environment.GetEnvironmentVariable("containerName");
            if (containerName == null)
            {
                containerName = Environment.MachineName;
            }
            if (containerName == null)
            {
                containerName = DateTime.Now.ToString("YYYYMMddHHmmss");
            }
            return containerName;
        }


        private static (string Ip, int port) GetIpAndPort(BinzConfig binzConfig)
        {
            var localIp = BinzUtil.GetLocalIp()
                                  .Where(e => !e.StartsWith("172") && !e.StartsWith("169"))
                                  .FirstOrDefault();
            if (localIp == null)
            {
                throw new Exception("Binz cannot get ip address!");
            }
            var port = binzConfig.Port;
            return (localIp, port);
        }

        private static AgentServiceCheck GetHealthCheck(BinzConfig binzConfig, string localIp, int port)
        {

            //请求注册的 Consul 地址
            var httpCheck = new AgentServiceCheck()
            {
                // 服务启动多久后注册
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                // 健康检查时间间隔，或者称为心跳间隔
                Interval = TimeSpan.FromSeconds(binzConfig.ConsulConfig.HealthCheckIntervalSec),
                Name = "SimpleCheck",
                // 健康检查地址
                GRPC = $"{localIp}:{port}", // grpc.health.v1.Health.Check
                GRPCUseTLS = false,
                // 禁用https检查
                TLSSkipVerify = true,
                Timeout = TimeSpan.FromSeconds(binzConfig.ConsulConfig.HealthCheckTimeoutSec)
            };
            return httpCheck;
        }
    }
}
