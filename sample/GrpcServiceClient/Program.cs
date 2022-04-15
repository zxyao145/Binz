using Binz.Client;
using Proto.GreeterService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Binz.Core;
using Binz.Consul;

var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json");
IConfiguration configuration = builder.Build();


var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging();
serviceCollection.AddSingleton<IConfiguration>(_ => configuration);


// here
serviceCollection.AddBinzClient<ConsulRegistry>(configuration);


var serviceProvider = serviceCollection.BuildServiceProvider();

var binzClient = serviceProvider.GetRequiredService<BinzClient>();

//var channel = await binzClient.CreateGrpcChannelAsync<Greeter.GreeterClient>();
//var client = new Greeter.GreeterClient(channel);

var client = await binzClient.CreateGrpcClient<Greeter.GreeterClient>();


var res = client?.SayHello(new HelloRequest { Name = "asas" });
Console.WriteLine(res?.Message);

Console.WriteLine("Hello, World!");
