using Binz.Core;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Binz.Server
{
    public static class Ext
    {
        public static IGrpcServerBuilder
            AddBinzServices<TRegistry>(this IGrpcServerBuilder grpcServerBuilder, IConfiguration configuration)
                where TRegistry : class, IRegistry
        {
            var services = grpcServerBuilder.Services;

            services.Configure<RegistryConfig>(
                configuration
                .GetSection("Binz:RegistryConfig")
            );

            services.AddSingleton<IRegistry, TRegistry>();
            services.AddSingleton<BinzServer>();


            // services.AddHostedService<GrpcHealthCheckHostService>();
            services
                .AddGrpcHealthChecks()
                .AddCheck<IHealthCheckImpl>("SimpleCheck");
                //.AddCheck("Simple", () => HealthCheckResult.Healthy())

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
        public static async Task RegisterBinzServer(this WebApplication app, params Type[] scanAssemblys)
        {
            app.MapGrpcHealthChecksService();
#if DEBUG
            app.MapGrpcReflectionService();
#endif
            var server = app.Services.GetRequiredService<BinzServer>();
            await server.InitAsync(app.Lifetime, scanAssemblys);
        }
    }
}
