# Binz（丙子）

The gRPC fast development framework with a registry is based on ASPNET Core. And the registry supports the use of `etcd` and `consul`.

# Usage

## Server

### Import package

```bash
Install-Package Binz.Server

# select a registry
# Install-Package Binz.Registry.Consul
Install-Package Binz.Registry.Etcd
```

### Add Attribute

add `[BinzService]` on gRPC service:

```c#
[BinzService]
public class GreeterService : Greeter.GreeterBase
{
    // something ...
}
```

### Init the host

#### Mode 1: Using BinzServerHost

```c#
await BinzServerHost.RunAsync<EtcdRegistryWithLease>(args,
    typeof(GreeterService), // assembly of the service to be registered
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
```

#### Mode 2: Custom

```c#
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddGrpc()
+    .AddBinzServices<TRegistry>(builder.Configuration);


var app = builder.Build();

// the assemblys of the gRPC service to be registered
+ await app.RegisterBinzServer(typeof(GreeterService), typeof(AnotherAssemblyService)); 

await app.RunAsync();
```

## Client

### Import package

```bash
Install-Package Binz.Server

# select a registry
# Install-Package Binz.Registry.Consul
Install-Package Binz.Registry.Etcd
```

### Coding

```c#
serviceCollection.AddBinzClient<EtcdRegistryWithLease>(configuration);


var binzClient = serviceProvider.GetRequiredService<BinzClient>();

var client = await binzClient.CreateGrpcClient<Greeter.GreeterClient>();
var res = client?.SayHello(new HelloRequest { Name = "Greeter" });
```





