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

### Add Config

Edit `appsettings.json`, and add `Binz` config.

**For Server:**

```json
{
  "Binz": {
    "Server": {
      "Port": 9527 // Kestrel port 
    },
    "RegistryConfig": {
      // consul
      // "Address": "http://127.0.0.1:18401/",
      // etcd
      "Address": "http://etcd-srv:2379", // registration center address
      "HealthCheckIntervalSec": 5 // health check interval second
    }
  }
}
```

**For Client:**

```json
"Binz": {
    "RegistryConfig": {
      // consul
      // "Address": "http://127.0.0.1:18401/",
      // etcd 
      "Address": "http://etcd-srv:2379", // registration center address
      "HealthCheckIntervalSec": 5 // health check interval second
    }
  }
```

### Init the host

#### Mode 1: Using BinzServerHost

```c#
await BinzServerHost.RunAsync<EtcdRegistryWithLease>(args,
    typeof(GreeterService), // assembly of the service to be registered, If no value is passed, all assemblies will be scanned
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

// the assemblys of the gRPC service to be registered which is in typeof(GreeterService).Assembly
// await app.RegisterBinzServer(typeof(GreeterService)); 
// all assemblies will be scanned to find [BinzService] attribute
+ await app.RegisterBinzServer(); 

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





