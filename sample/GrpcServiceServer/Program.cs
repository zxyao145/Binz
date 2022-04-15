using Binz.Consul;
using Binz.Server;
using GrpcServiceServer.Services;


await BinzServerApp.RunAsync<ConsulRegistry>(args,
    typeof(GreeterService),
    configureServices: null,
    configure: app =>
    {
        app.MapGrpcService<GreeterService>();
    }
);
