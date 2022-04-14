using Binz.Consul;
using Binz.Core;
using Binz.Server;
using GrpcServiceServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc()
    .AddBinzServices<ConsulRegistry>(builder.Configuration);


var app = builder.Build();

app.MapGrpcService<GreeterService>();
await app.RegisterBinzServer(typeof(GreeterService));
app.Run();
