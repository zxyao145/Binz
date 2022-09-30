using Binz.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Proto.GreeterService;

namespace BinzClientInWebApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly BinzClient _binzClient;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, BinzClient binzClient)
        {
            _logger = logger;
            _binzClient = binzClient;
        }


        // /WeatherForecast/GetWeatherForecast
        [HttpGet]
        public IEnumerable<WeatherForecast> GetWeatherForecast()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        // /WeatherForecast/GetGreeter
        [HttpGet]
        public async Task<IEnumerable<string>> GetGreeter()
        {
            try
            {
                var client = await _binzClient.CreateGrpcClient<Greeter.GreeterClient>();
                var res = client?.SayHello(new HelloRequest { Name = "Greeter" });
                _logger.LogInformation(res?.Message);

                var client2 = await _binzClient.CreateGrpcClient<Greeter2.Greeter2Client>();
                var res2 = client2?.SayHello(new HelloRequest { Name = "Greeter2 " });
                _logger.LogInformation(res2?.Message);

                return new List<string> { res?.Message ?? "", res2?.Message ?? "" };
            }
            catch (Exception e)
            {
                return new List<string> { e.Message };
            }
          
        }
    }
}