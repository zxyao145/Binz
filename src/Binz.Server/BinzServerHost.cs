using Binz.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Binz.Server
{
    public class BinzServerHost
    {
        public static async Task
            RunAsync<TRegistry>(string[] args,
                                Type? scanAssembly = null,
                                Action<IServiceCollection, IConfiguration>? configureServices = null,
                                Action<WebApplication, IWebHostEnvironment>? configure = null)
             where TRegistry : class, IRegistry
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            builder.Services.AddGrpc()
                .AddBinzServices<TRegistry>(builder.Configuration);

            configureServices?.Invoke(builder.Services, configuration);

            var app = builder.Build();
            configure?.Invoke(app, app.Environment);

            if(scanAssembly != null)
            {
                await app.RegisterBinzServer(scanAssembly);
            }
            else
            {
                await app.RegisterBinzServer();
            }
            await app.RunAsync();
        }



        public static async Task
           RunAsync<TRegistry>(string[] args,
                               Type? scanAssembly = null,
                               Action<WebApplicationBuilder>? configureServices = null,
                               Action<WebApplication>? configure = null)
            where TRegistry : class, IRegistry
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            builder.Services.AddGrpc()
                .AddBinzServices<TRegistry>(builder.Configuration);

            configureServices?.Invoke(builder);

            var app = builder.Build();
            configure?.Invoke(app);
            if(scanAssembly !=null)
            {
                await app.RegisterBinzServer(scanAssembly);
            }
            else
            {
                await app.RegisterBinzServer();
            }
            await app.RunAsync();
        }
    }
}
