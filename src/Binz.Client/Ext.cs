using Binz.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binz.Client
{
    public static class Ext
    {
        public static void AddBinzClient<TConsulRegistry>
            (this IServiceCollection serviceCollection, IConfiguration configuration)
            where TConsulRegistry : class, IRegistry
        {

            serviceCollection.Configure<RegistryConfig>(
                          configuration
                          .GetSection("Binz:RegistryConfig")
                      );
            serviceCollection.AddSingleton<IRegistry, TConsulRegistry>();
            serviceCollection.AddSingleton<BinzClient>();
        }
    }
}
