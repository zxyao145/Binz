using Binz.Core;
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Binz.Consul
{
    public class ConsulRegistry : IRegistry
    {
        private readonly ConsulClient _consulClient;
        private readonly RegistryConfig _registryConfig;
        private readonly ILogger<ConsulRegistry> _logger;


        private ConcurrentDictionary<string, ulong> _serviceModifyIndex = new ConcurrentDictionary<string, ulong>();
        private ConcurrentBag<RegisterInfo> _allRegisterInfo = new ConcurrentBag<RegisterInfo>();
        private readonly CancellationTokenSource _watchCancellationTokenSource;

        public ConsulRegistry(IOptions<RegistryConfig> registryConfigOptions, ILogger<ConsulRegistry> logger)
        {
            _registryConfig = registryConfigOptions.Value;
            _consulClient = new ConsulClient(config =>
            {
                config.Address = new Uri(_registryConfig.Address);
            });

            _logger = logger;
            _watchCancellationTokenSource = new CancellationTokenSource();
        }

        private AgentServiceCheck GetHealthCheck(string serviceIp, int servicePort)
        {
            //请求注册的 Consul 地址
            var httpCheck = new AgentServiceCheck()
            {
                // 服务启动多久后注册
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                // 健康检查时间间隔，或者称为心跳间隔
                Interval = TimeSpan.FromSeconds(_registryConfig.HealthCheckIntervalSec),
                Name = "SimpleCheck",
                // 健康检查地址
                GRPC = $"{serviceIp}:{servicePort}", // grpc.health.v1.Health.Check
                GRPCUseTLS = false,
                // 禁用https检查
                TLSSkipVerify = true,
                Timeout = TimeSpan.FromSeconds(_registryConfig.HealthCheckTimeoutSec)
            };
            return httpCheck;
        }

        public async Task RegisterAsync(RegisterInfo registerInfo)
        {
            AgentServiceCheck httpCheck = GetHealthCheck(registerInfo.ServiceIp, registerInfo.ServicePort);

            if (string.IsNullOrWhiteSpace(registerInfo.ServiceId))
            {
                var pureServiceName = registerInfo.ServiceName.Split('.')[^1];
                registerInfo.ServiceId =
                    $"{pureServiceName}-{BinzUtil.GetContainerName()}-{Guid.NewGuid().ToString().Substring(0, 6)}";
            }
            var id = registerInfo.ServiceId;

            // Register service with consul
            var registration = new AgentServiceRegistration()
            {
                Checks = new[] { httpCheck },
                ID = id,
                Name = registerInfo.ServiceName,
                Address = registerInfo.ServiceIp,
                Port = registerInfo.ServicePort,

                //添加 urlprefix-/servicename 格式的 tag 标签，以便 Fabio 识别
                Tags = new[] {
                    $"EnvName:{registerInfo.EnvName}",
                    $"Level:{registerInfo.Level}",
                    $"Weight:{registerInfo.Weight}",
                    $"urlprefix-/{registerInfo.ServiceName}"
                }
            };

            // 服务启动时注册，内部实现其实就是使用 Consul API 进行注册（HttpClient发起）
            await _consulClient.Agent.ServiceRegister(registration);

            _allRegisterInfo.Add(registerInfo);
        }

        public async Task UnRegisterAsync(RegisterInfo serviceInfo)
        {
            await _consulClient.Agent.ServiceDeregister(serviceInfo.ServiceId).ConfigureAwait(false);
        }

        public async Task UnRegisterAllAsync()
        {
            foreach (var registerInfo in _allRegisterInfo)
            {
                await UnRegisterAsync(registerInfo);
            }
        }

        public async Task<List<ServiceInfo>> GetServiceAsync(RegisterInfo serviceInfo)
        {
            var serviceName = serviceInfo.ServiceName;
            var envTag = BinzUtil.GetEnvTag(serviceInfo.EnvName);
            var services = await _consulClient.Catalog.Service(serviceName, envTag);
            if (services.Response.Length == 0)
            {
                throw new Exception($"未发现服务 {serviceName}");
            }

            foreach (var item in services.Response)
            {
                _logger.LogInformation("BinzClient get service for {0}: {1}, {2}, {3}"
                    , serviceName, item.ServiceAddress, item.ServicePort, string.Join(";", item.ServiceTags));
            }
            var lastIndex = services.LastIndex;

            _serviceModifyIndex[serviceName] = lastIndex;

            var res = services.Response
                .Where(e => e.ServiceTags.Contains(envTag))
                .Select(e => new ServiceInfo(e.ServiceAddress, e.ServicePort))
                .OrderBy(e => e.ServiceIp).ThenBy(e => e.ServicePort)
                .ToList();
            return res;
        }

        public async Task Watch(RegisterInfo serviceInfo, Func<List<ServiceInfo>, Task> OnServiceCahnged)
        {
            var serviceName = serviceInfo.ServiceName;
            var lastIndex = _serviceModifyIndex[serviceName];
            var envTag = BinzUtil.GetEnvTag(serviceName);
            while (true)
            {
                if (_watchCancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }
                QueryResult<CatalogService[]> services = new QueryResult<CatalogService[]>();

                try
                {
                    services = await _consulClient.Catalog.Service(serviceName, envTag, new QueryOptions
                    {
                        WaitIndex = lastIndex,
                    }, _watchCancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("WatchAsync get service failed: {0}", ex);
                    await Task.Delay(5000);
                    continue;
                }

                _logger.LogInformation("sync service {0} from consul", serviceName);

                if (services.LastIndex != lastIndex)
                {
                    lastIndex = services.LastIndex;
                    _serviceModifyIndex[serviceName] = lastIndex;


                    var newServicesInfos = services.Response
                         .Select(x =>
                         {
                             return new ServiceInfo(x.ServiceAddress, x.ServicePort);
                         })
                         .ToList();
                    _logger.LogInformation("update service {0} servers: {1}", serviceName, JsonSerializer.Serialize(newServicesInfos));
                    await (OnServiceCahnged?.Invoke(newServicesInfos) ?? Task.CompletedTask);
                }
            }
        }

        public void Dispose()
        {
            _watchCancellationTokenSource?.Cancel();
            _consulClient?.Dispose();
        }

       
    }
}