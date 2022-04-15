﻿using Binz.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Binz.Server
{
    public class BinzServerApp
    {
        public static async Task
            RunAsync<TRegistry>(string[] args,
                                Type scanAssembly,
                                Action<IServiceCollection, IConfiguration>? configureServices = null,
                                Action<WebApplication?, IWebHostEnvironment>? configure = null)
             where TRegistry : class, IRegistry
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;
            builder.Services.AddGrpc()
                .AddBinzServices<TRegistry>(builder.Configuration);

            configureServices?.Invoke(builder.Services, configuration);

            var app = builder.Build();
            configure?.Invoke(app, app.Environment);

            await app.RegisterBinzServer(scanAssembly);
            await app.RunAsync();
        }



        public static async Task
           RunAsync<TRegistry>(string[] args,
                               Type scanAssembly,
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

            await app.RegisterBinzServer(scanAssembly);
            await app.RunAsync();
        }
    }
}
