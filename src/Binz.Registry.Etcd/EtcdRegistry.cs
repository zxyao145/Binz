using Binz.Core;
using dotnet_etcd;
using dotnet_etcd.interfaces;
using Etcdserverpb;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Binz.Registry.Etcd
{

    public class EtcdRegistry : EtcdRegistryBase
    {

        public EtcdRegistry(
            IOptions<RegistryConfig> registryConfigOptions,
            ILogger<EtcdRegistry> logger
            ) : base(registryConfigOptions, logger) { }

        public override async Task RegisterAsync(RegistryInfo serviceInfo)
        {
            var registrationInfo = new EtcdRegistryValue()
            {
                ServiceAddress = serviceInfo.ServiceIp,
                Port = serviceInfo.ServicePort,
                Tags = serviceInfo.Tags,
                Version = 1,
                EnvName = serviceInfo.EnvName,
            };

            var key = GetRegisKey(serviceInfo);
            var val = JsonSerializer.Serialize(registrationInfo);

            PutRequest putRequest = new PutRequest()
            {
                Key = ByteString.CopyFromUtf8(key),
                Value = ByteString.CopyFromUtf8(val),
            };
            await _etcdClient.PutAsync(putRequest);

            _allRegisterInfo.Add(serviceInfo);
        }


        public override async Task UnRegisterAsync(RegistryInfo serviceInfo)
        {
            var key = GetRegisKey(serviceInfo);
            await _etcdClient.DeleteAsync(key);
        }
    }

}