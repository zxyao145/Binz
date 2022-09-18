using Binz.Core;
using dotnet_etcd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Binz.Registry.Etcd
{
    public abstract class EtcdRegistryBase : IRegistry, IAsyncDisposable
    {
        protected ConcurrentBag<RegistryInfo> _allRegisterInfo = new ConcurrentBag<RegistryInfo>();


        protected readonly RegistryConfig _registryConfig;
        protected readonly EtcdClient _etcdClient;
        protected readonly ILogger<EtcdRegistry> _logger;
        protected readonly CancellationTokenSource _watchCancellationTokenSource;


        protected EtcdRegistryBase(
            IOptions<RegistryConfig> registryConfigOptions,
            ILogger<EtcdRegistry> logger
            )
        {
            _registryConfig = registryConfigOptions.Value;
            _etcdClient = new EtcdClient(_registryConfig.Address ?? "http://127.0.0.1:2379");
            _logger = logger;
            _watchCancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation(JsonSerializer.Serialize(_registryConfig));
        }

        public abstract Task RegisterAsync(RegistryInfo serviceInfo);
        public abstract Task UnRegisterAsync(RegistryInfo serviceInfo);

        public async Task UnRegisterAllAsync()
        {
            foreach (var registerInfo in _allRegisterInfo)
            {
                await UnRegisterAsync(registerInfo);
            }
        }

        ConcurrentDictionary<string, byte> hasWatchedService = new ConcurrentDictionary<string, byte>();

        public Task Watch(RegistryInfo serviceInfo, Func<List<ServiceInfo>, Task> OnServiceCahnged)
        {
            if (hasWatchedService.ContainsKey(serviceInfo.ServiceName))
            {
            }
            if (!hasWatchedService.TryAdd(serviceInfo.ServiceName, 1))
            {
                return Task.CompletedTask;
            }

            var serviceName = serviceInfo.ServiceName;
            Task.Factory.StartNew(() =>
            {
                _etcdClient.WatchRange(path: serviceInfo.ServiceName, async (events) =>
                {
                    if (events.Length > 0)
                    {
                        List<string> data = events.Select(x => x.Value).ToList();
                        try
                        {
                            _logger.LogInformation("sync service {0} from etcd", serviceName);
                            var newServicesInfos = ParseRegisInfo(data, serviceInfo.ServiceName, serviceInfo.EnvName);
                            _logger.LogInformation("update service {0} servers: {1}", serviceName, JsonSerializer.Serialize(newServicesInfos));
                            await (OnServiceCahnged?.Invoke(newServicesInfos) ?? Task.CompletedTask);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("WatchAsync get service failed: {0}", ex);
                            await Task.Delay(2000);
                        }

                    }
                }, cancellationToken: _watchCancellationTokenSource.Token);
            }, TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }


        private List<ServiceInfo> ParseRegisInfo(
            IEnumerable<string> servicesArr,
            string serviceNameRange,
            string envName
        )
        {
            var registrationInfo = servicesArr
                .Select(x => JsonSerializer.Deserialize<EtcdRegistryValue>(x))
                .Where(x =>
                {
                    return x != null && x.EnvName == envName;
                })
                .ToList();
            if (registrationInfo.Count == 0)
            {
                _logger.LogError("service not be found {0} with envTag{1}", serviceNameRange, envName);
                throw BinzException.ServiceCannotBrFound;
            }

            List<ServiceInfo> serviceInfos = new List<ServiceInfo>();

            foreach (var item in registrationInfo)
            {
                serviceInfos.Add(new ServiceInfo(item!.ServiceAddress!, item.Port));

                _logger.LogInformation("BinzClient get service for {0}: {1}, {2}, {3}"
                    , serviceNameRange, item!.ServiceAddress, item!.Port,
                    string.Join(";", item.Tags ?? new Dictionary<string, string>()));
            }

            var res = serviceInfos
                .OrderBy(e => e.ServiceIp)
                .ThenBy(e => e.ServicePort)
                .ToList();
            return res;
        }




        public async Task<List<ServiceInfo>> GetServiceAsync(RegistryInfo registryInfo)
        {
            var servicesArr = await _etcdClient.GetRangeValAsync(registryInfo.ServiceName + "/");

            if (servicesArr == null || servicesArr.Count == 0)
            {
                _logger.LogError("service register info cannot found:{0}", registryInfo.ServiceName);
                throw BinzException.ServiceCannotBrFound;
            }

            return ParseRegisInfo(servicesArr.Select(x => x.Value), registryInfo.ServiceName, registryInfo.EnvName);
        }




        protected string GetRegisKey(RegistryInfo registerInfo)
        {
            var key = registerInfo.ServiceName + $"/{registerInfo.ServiceIp}:{registerInfo.ServicePort}";
            return key;
        }

        public async ValueTask DisposeAsync()
        {
            _watchCancellationTokenSource.Cancel();
            await UnRegisterAllAsync();
            _etcdClient.Dispose();
        }
    }
}
