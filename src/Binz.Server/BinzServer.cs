﻿using Binz.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Server
{
    public class BinzServer
    {
        private readonly IRegistry _registry;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BinzServer> _logger;

        public BinzServer(IRegistry registry, IConfiguration configuration, ILogger<BinzServer> logger)
        {
            this._registry = registry;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有 Assembly 内所有的 binz 服务名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static List<string> ScanGrpcService()
        {
            var binzSvcs = new List<string>();
            var assemblys = ReflectionHelper.GetAllReferencedAssemblies();
            foreach (var asm in assemblys)
            {
                binzSvcs.AddRange(ScanGrpcService(asm));
            }

            return binzSvcs;
        }

        /// <summary>
        /// 获取 Assembly 内所有的 binz 服务名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static List<string> ScanGrpcService(Assembly asm)
        {
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
                    if (attribute is BinzServiceAttribute binzServiceAttribute)
                    {
                        var name = binzServiceAttribute.ServiceName;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = BinzUtil.GetServerServiceName(item);
                        }
                        binzSvcs.Add(name);
                        continue;
                    }
                }
            }

            return binzSvcs;
        }


        public async Task InitAsync(
            IHostApplicationLifetime lifetime,
            params Type[] scanTypes)
        {
            int port = _configuration
                            .GetSection("Binz:Server:Port")
                            ?.Get<int>()
                            ??
                            BinzConstants.DefaultPort;
            var localIp = GetIp();

            _logger.LogInformation($"binzserver bind {localIp}:{port}");

            var env = BinzUtil.GetEnvName();
            var binzSvcNames = new List<string>();
            if (scanTypes != null && scanTypes.Length > 0)
            {
                HashSet<string> names = new HashSet<string>();
                foreach (var scanType in scanTypes)
                {
                    var key = scanType.FullName;
                    if (!string.IsNullOrWhiteSpace(key) && !names.Contains(key))
                    {
                        names.Add(key);
                        binzSvcNames.AddRange(
                            ScanGrpcService(scanType.Assembly)
                            );
                    }
                }
            }
            else
            {
                binzSvcNames = ScanGrpcService();
            }

            foreach (var svcName in binzSvcNames)
            {
                var registerInfo = new RegistryInfo()
                {
                    ServiceName = svcName,
                    ServiceIp = localIp,
                    ServicePort = port,
                    EnvName = env,
                };

                await _registry.RegisterAsync(registerInfo).ConfigureAwait(false);
                _logger.LogInformation("regist service:{0}", registerInfo);
            }

            //lifetime.ApplicationStopping.Register(async () =>
            //{
            //    await _registry.DisposeAsync();
            //});
        }

        private static string GetIp()
        {
            var localIp = BinzUtil.GetLocalIp()[0];
            if (localIp == null)
            {
                throw new Exception("Binz cannot get ip address!");
            }
            return localIp;
        } 

    }
}
