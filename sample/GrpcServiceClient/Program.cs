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


serviceCollection.Configure<RegistryConfig>(
              configuration
              .GetSection("Binz:RegistryConfig")
          );
serviceCollection.AddSingleton<IRegistry, ConsulRegistry>();
serviceCollection.AddSingleton<BinzClient>();


serviceCollection.AddLogging();


serviceCollection.AddScoped<IConfiguration>(_ => configuration);


var serviceProvider = serviceCollection.BuildServiceProvider();

var binzClient = serviceProvider.GetRequiredService<BinzClient>();

var channel = await binzClient.CreateGrpcChannelAsync<Greeter.GreeterClient>();
var client = new Greeter.GreeterClient(channel);
var res = client.SayHello(new HelloRequest { Name = "asas" });
Console.WriteLine(res?.Message);

Console.WriteLine("Hello, World!");
