﻿using Binz.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Server
{
    public class BinzServer
    {
        private readonly IRegistry _registry;
        private readonly IConfiguration _configuration;

        public BinzServer(IRegistry registry, IConfiguration configuration)
        {
            this._registry = registry;
            _configuration = configuration;
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
                    if (attribute is BinzServiceAttribute binzServiceAttribute)
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

        public async Task InitAsync(IHostApplicationLifetime lifetime,
            Type scanAssembly)
        {
            int port = _configuration
                            .GetSection("Binz:Server:Port")
                            ?.Get<int>()
                            ??
                            BinzConstants.DefaultPort;
            var localIp = GetIp();

            Console.WriteLine($"binzserver bind {localIp}:{port}");

            var env = BinzUtil.GetEnvName();
            var binzSvcNames = ScanGrpcService(scanAssembly);
            var serviceIds = new List<string>(binzSvcNames.Count);
            foreach (var svcName in binzSvcNames)
            {
                var registerInfo = new RegisterInfo()
                {
                    ServiceName = svcName,
                    ServiceIp = localIp,
                    ServicePort = port,
                    EnvName = env,
                };

                await _registry.RegisterAsync(registerInfo).ConfigureAwait(false);
            }

            lifetime.ApplicationStopping.Register(async () =>
            {
                await _registry.UnRegisterAllAsync();
            });
        }

        private static string GetIp()
        {
            var localIp = BinzUtil.GetLocalIp()
                                  .Where(e => !e.StartsWith("172") && !e.StartsWith("169"))
                                  .FirstOrDefault();
            if (localIp == null)
            {
                throw new Exception("Binz cannot get ip address!");
            }
            return localIp;
        }

    }
}
