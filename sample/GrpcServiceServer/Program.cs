using Binz.Registry.Consul;
using Binz.Registry.Etcd;
using Binz.Server;
using GrpcServiceServer;
using GrpcServiceServer.Services;


//await BinzServerHost.RunAsync<ConsulRegistry>(args,
//    typeof(GreeterService),
//    configureServices: builder =>
//    {
//        builder.Services.AddScoped<HelloService>();
//    },
//    configure: app =>
//    {
//        app.MapGrpcService<GreeterService>();
//        app.MapGrpcService<GreeterService2>();
//    }
//);

//await BinzServerHost.RunAsync<EtcdRegistry>(args,
//    typeof(GreeterService),
//    configureServices: builder =>
//    {
//        builder.Services.AddScoped<HelloService>();
//    },
//    configure: app =>
//    {
//        app.MapGrpcService<GreeterService>();
//        app.MapGrpcService<GreeterService2>();
//    }
//);

await BinzServerHost.RunAsync<EtcdRegistryWithLease>(args,
    typeof(GreeterService),
    configureServices: builder =>
    {
        builder.Services.AddScoped<HelloService>();
    },
    configure: app =>
    {
        app.MapGrpcService<GreeterService>();
        app.MapGrpcService<GreeterService2>();
    }
);