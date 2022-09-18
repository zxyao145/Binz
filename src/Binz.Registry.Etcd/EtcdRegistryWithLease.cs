using Binz.Core;
using dotnet_etcd;
using dotnet_etcd.interfaces;
using Etcdserverpb;
using Google.Protobuf;
using Grpc.Core;
using HashDepot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Binz.Registry.Etcd
{

    public class EtcdRegistryWithLease : EtcdRegistryBase
    {
        private readonly ConcurrentDictionary<string, long> _leaseId = new ConcurrentDictionary<string, long>();


        public EtcdRegistryWithLease(
            IOptions<RegistryConfig> registryConfigOptions,
            ILogger<EtcdRegistry> logger
            ) : base(registryConfigOptions, logger)
        {

        }



        public override async Task RegisterAsync(RegistryInfo registerInfo)
        {
            var registrationInfo = new EtcdRegistryValue()
            {
                ServiceAddress = registerInfo.ServiceIp,
                Port = registerInfo.ServicePort,
                Tags = registerInfo.Tags,
                Version = 1,
                EnvName = registerInfo.EnvName,
            };

            var key = GetRegisKey(registerInfo);
            var val = JsonSerializer.Serialize(registrationInfo);
            long ticks = DateTime.UtcNow.Ticks;
            var leaseId = GenLeaseId(ticks, registerInfo);
            LeaseGrantRequest leaseRequest = new LeaseGrantRequest()
            {
                ID = leaseId,
                TTL = (_registryConfig.HealthCheckIntervalSec + _registryConfig.HealthCheckTimeoutSec) * 1000,
            };
            int excepCount = 0;
        gotoTag:
            try
            {
                await _etcdClient.LeaseGrantAsync(leaseRequest);
            }
            catch (RpcException e)
            {
                if (e.StatusCode == StatusCode.FailedPrecondition && e.Status.Detail == "etcdserver: lease already exists")
                {
                    _logger.LogWarning("etcdserver: lease already exists for {0}", leaseId);
                    excepCount += 1;
                    if (excepCount < 5)
                    {
                        goto gotoTag;
                    }
                    else
                    {
                        _logger.LogError("etcdserver: lease already exists try 5 times");
                        throw e;
                    }
                }
            }
            _leaseId[key] = leaseId;

            PutRequest putRequest = new PutRequest()
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(val),
                Lease = leaseRequest.ID
            };
            await _etcdClient.PutAsync(putRequest);
            _ = Task.Factory.StartNew(async (obj) =>
            {
                while (true)
                {
                    if (!_watchCancellationTokenSource.IsCancellationRequested)
                    {
                        await _etcdClient.LeaseKeepAlive(
                            new LeaseKeepAliveRequest
                            {
                                ID = leaseRequest.ID
                            },
                            rep => _logger.LogDebug($"Lease {rep.ID} keep alive"),
                            _watchCancellationTokenSource.Token
                        );
                    }

                    await Task.Delay(_registryConfig.HealthCheckIntervalSec * 1000);
                }
            }, _watchCancellationTokenSource.Token, TaskCreationOptions.LongRunning);

            _allRegisterInfo.Add(registerInfo);
        }

        public override async Task UnRegisterAsync(RegistryInfo serviceInfo)
        {
            var key = GetRegisKey(serviceInfo);
            if (_leaseId.Remove(key, out var leaseId))
            {
                await _etcdClient.LeaseRevokeAsync(new LeaseRevokeRequest()
                {
                    ID = leaseId
                });
            }

            await _etcdClient.DeleteAsync(key);
        }

        ConcurrentDictionary<string, byte> hasWatchedService = new ConcurrentDictionary<string, byte>();


        private long GenLeaseId(long ticks, RegistryInfo registerInfo)
        {
            var str = ticks
                + $"{registerInfo.ServiceName}{registerInfo.ServiceIp}:{registerInfo.ServicePort}"
                + new Random().Next(1, 99999);
            return MurmurHash3.Hash32(Encoding.UTF8.GetBytes(str), 19960517);
        }
    }

}