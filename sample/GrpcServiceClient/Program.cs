using Binz.Client;
using Proto.GreeterService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Binz.Core;
using Binz.Registry.Consul;
using Binz.Registry.Etcd;
using DnsClient.Internal;

var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json");
IConfiguration configuration = builder.Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging();
serviceCollection.AddSingleton<IConfiguration>(_ => configuration);


// here
// serviceCollection.AddBinzClient<ConsulRegistry>(configuration);
serviceCollection.AddBinzClient<EtcdRegistryWithLease>(configuration);
var serviceProvider = serviceCollection.BuildServiceProvider();


var binzClient = serviceProvider.GetRequiredService<BinzClient>();
//var channel = await binzClient.CreateGrpcChannelAsync<Greeter.GreeterClient>();
//var client = new Greeter.GreeterClient(channel);

var logger = serviceProvider.GetRequiredService<ILogger>();

var client = await binzClient.CreateGrpcClient<Greeter.GreeterClient>();
var res = client?.SayHello(new HelloRequest { Name = "Greeter" });
logger.LogInformation(res?.Message);

var client2 = await binzClient.CreateGrpcClient<Greeter2.Greeter2Client>();
var res2 = client2?.SayHello(new HelloRequest { Name = "Greeter2 " });
logger.LogInformation(res2?.Message);
