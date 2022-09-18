using Binz.Core;
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
    public class BinzClient : IAsyncDisposable
    {
        private readonly ILogger<BinzClient> _logger;
        private readonly IRegistry _registry;
        private readonly ConcurrentDictionary<string, GrpcChannel?> _grpcChannelDict;
        private readonly ConcurrentDictionary<string, ServiceClientCacheItem> _serviceInfoCacheDict;

        public BinzClient(ILogger<BinzClient> logger, IConfiguration configuration, IRegistry registry)
        {
            _logger = logger;
            this._registry = registry;

            _grpcChannelDict = new ConcurrentDictionary<string, GrpcChannel?>();
            _serviceInfoCacheDict = new ConcurrentDictionary<string, ServiceClientCacheItem>();


        }

        public async Task<T?> CreateGrpcClient<T>(bool warmUp = true, int connectTimeoutSecond = 5)
        {
            var channel = await CreateGrpcChannelAsync<T>(warmUp, connectTimeoutSecond);
            var client = (T?) Activator.CreateInstance(typeof(T), channel);
            return client;
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
            var registerService = new RegistryInfo
            {
                ServiceName = serviceName,
            };

            var services = await _registry.GetServiceAsync(registerService);
            var serviceCacheInfo = new ServiceClientCacheItem(serviceName)
            {
                ConnectTimeoutSecond = connectTimeoutSecond,
            };

            _serviceInfoCacheDict.AddOrUpdate(serviceName, serviceCacheInfo, (key, val) => serviceCacheInfo);
            _ = WatchAsync(serviceName).ConfigureAwait(false);
            return await CreateGrpcChannelInternalAsync(serviceName, services, connectTimeoutSecond);
        }

        private Task<GrpcChannel> CreateGrpcChannelInternalAsync(string serviceName, List<ServiceInfo> services, int connectTimeoutSecond = 5)
        {
            var balancerAddresses = services
                    .Select(x => new BalancerAddress(x.ServiceIp, x.ServicePort));
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
        private Task WatchAsync(string serviceName)
        {
            var registerService = new RegistryInfo
            {
                ServiceName = serviceName,
            };
            var task = _registry.Watch(registerService, (async (services) =>
            {
                if (services.Count == 0)
                {
                    _grpcChannelDict[serviceName] = null;
                }
                else
                {
                    var serviceCacheInfos = _serviceInfoCacheDict[serviceName];
                    serviceCacheInfos.LastSyncTime = DateTime.Now;

                    var grpcChannel = await CreateGrpcChannelInternalAsync(serviceName, services, serviceCacheInfos.ConnectTimeoutSecond);
                    await grpcChannel.ConnectAsync();
                    _grpcChannelDict.AddOrUpdate(serviceName, (GrpcChannel)grpcChannel, (key, value) =>
                    {
                        return grpcChannel;
                    });
                }
            }));
            task.ConfigureAwait(false);
            return task;
        }


        public async ValueTask DisposeAsync()
        {
            await (_registry?.DisposeAsync() ?? ValueTask.CompletedTask);
            foreach (var item in _grpcChannelDict.Values)
            {
                item?.Dispose();
            }
        }
    }
}
