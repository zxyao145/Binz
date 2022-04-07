using Binz.Core;
using Consul;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Binz.Client
{
    public class BinzClient : IDisposable
    {
        private readonly ConsulClient _consulClient;
        private readonly ILogger<BinzClient> _logger;
        private readonly ConcurrentDictionary<string, GrpcChannel?> _grpcChannelDict;
        private readonly ConcurrentDictionary<string, ServiceClientCacheItem> _serviceInfoCacheDict;
        private readonly CancellationTokenSource _watchCancellationTokenSource;

        public BinzClient(ILogger<BinzClient> logger, IConfiguration configuration)
        {
            _logger = logger;
            var binzConsulConfig = new BinzConsulConfig();
            var section = configuration.GetSection("Binz:ConsulConfig");
            if (section != null)
            {
                section.Bind(binzConsulConfig);
            }

            var consulClient = new ConsulClient(x =>
                      {
                          x.Address = new Uri(binzConsulConfig.Address);
                          x.Datacenter = string.Empty;
                      });
            _consulClient = consulClient;

            _grpcChannelDict = new ConcurrentDictionary<string, GrpcChannel?>();
            _serviceInfoCacheDict = new ConcurrentDictionary<string, ServiceClientCacheItem>();


            _watchCancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 第一次创建客户端的时候，获取server信息
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<(List<ServiceInfo> ServiceInfos, ulong LastIndex)>
            GetServicesAsync(string serviceName)
        {
            var envTag = BinzUtil.GetEnvTag();
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

            var res = services.Response
                .Where(e => e.ServiceTags.Contains(envTag))
                .Select(e => new ServiceInfo(e.ServiceAddress, e.ServicePort))
                .OrderBy(e => e.Address).ThenBy(e => e.Port)
                .ToList();
            return (res, lastIndex);
        }

        public async Task<GrpcChannel?> CreateGrpcChannelAsync<T>(bool warmUp = true, int connectTimeoutSecond = 5)
        {
            var serviceName = BinzUtil.GetClientServiceName<T>();

            if (_grpcChannelDict.ContainsKey(serviceName))
            {
                return _grpcChannelDict[serviceName];
            }
            var channel = await CreateGrpcChannelInternalAsync<T>(connectTimeoutSecond);

            if (warmUp)
            {
                await channel.ConnectAsync();
            }

            _grpcChannelDict[serviceName] = channel;
            return channel;
        }

        private async Task<GrpcChannel> CreateGrpcChannelInternalAsync<T>(int connectTimeoutSecond = 5)
        {
            var serviceName = BinzUtil.GetClientServiceName<T>();
            var (services, lastIndex) = await GetServicesAsync(serviceName);
            var serviceCacheInfo = new ServiceClientCacheItem(serviceName)
            {
                ConnectTimeoutSecond = connectTimeoutSecond,
                LastIndex = lastIndex
            };

            _serviceInfoCacheDict.AddOrUpdate(serviceName, serviceCacheInfo, (key, val) => serviceCacheInfo);
            _ = WatchAsync(serviceName).ConfigureAwait(false);
            return await CreateGrpcChannelInternalAsync(serviceName, services, connectTimeoutSecond);
        }


        private Task<GrpcChannel> CreateGrpcChannelInternalAsync(string serviceName, List<ServiceInfo> services, int connectTimeoutSecond = 5)
        {
            var balancerAddresses = services
                    .Select(x => new BalancerAddress(x.Address, x.Port));
            var factory = new StaticResolverFactory(addr => balancerAddresses);
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ResolverFactory>(factory);

            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = TimeSpan.FromSeconds(connectTimeoutSecond),
            };

            var channel = GrpcChannel.ForAddress(
                "static:///" + serviceName,
                new GrpcChannelOptions
                {
                    Credentials = ChannelCredentials.Insecure, // http
                    HttpHandler = handler,
                    ServiceProvider = serviceCollection.BuildServiceProvider(),
                    ServiceConfig = new ServiceConfig
                    {
                        LoadBalancingConfigs = { new RoundRobinConfig() }
                    },
                });

            return Task.FromResult(channel);
        }

        /// <summary>
        /// 监控服务变化
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        private async Task WatchAsync(string serviceName)
        {
            var lastIndex = _serviceInfoCacheDict[serviceName].LastIndex;
            var envTag = BinzUtil.GetEnvName();
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

                if(services.LastIndex != lastIndex)
                {
                    lastIndex = services.LastIndex;

                    if (services.Response.Length == 0)
                    {
                        _grpcChannelDict[serviceName] = null;
                    }
                    else
                    {
                        var serviceCacheInfos = _serviceInfoCacheDict[serviceName];
                        serviceCacheInfos.LastSyncTime = DateTime.Now;
                        serviceCacheInfos.LastIndex = lastIndex;

                        var newServicesInfos = services.Response
                            .Select(x =>
                            {
                                return new ServiceInfo(x.ServiceAddress, x.ServicePort);
                            })
                            .ToList();

                        _logger.LogInformation("update service {0} servers: {1}", serviceName, JsonSerializer.Serialize(newServicesInfos));

                        var grpcChannel = await CreateGrpcChannelInternalAsync(serviceName, newServicesInfos, serviceCacheInfos.ConnectTimeoutSecond);
                        await grpcChannel.ConnectAsync();
                        _grpcChannelDict.AddOrUpdate(serviceName, grpcChannel, (key, value) =>
                        {
                            return grpcChannel;
                        });
                    }
                }
            }
        }


        public void Dispose()
        {
            _watchCancellationTokenSource.Cancel();
            foreach (var item in _grpcChannelDict.Values)
            {
                item?.Dispose();
            }
            _consulClient?.Dispose();
        }
    }
}
