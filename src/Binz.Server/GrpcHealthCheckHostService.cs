using Grpc.Health.V1;
using Grpc.HealthCheck;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Binz.Server
{
    public class GrpcHealthCheckHostService : IHostedService, IDisposable
    {
        private readonly ILogger<GrpcHealthCheckHostService> _logger;
        private readonly HealthServiceImpl _healthService;
        private readonly HealthCheckService _healthCheckService;
        private Timer? _timer;

        public GrpcHealthCheckHostService(
             ILogger<GrpcHealthCheckHostService> logger,
            HealthServiceImpl healthService, HealthCheckService healthCheckService)
        {
            _logger = logger;
            _healthService = healthService;
            _healthCheckService = healthCheckService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GrpcHealthCheck Service running.");
            _timer = new Timer(async (state) =>
            {
                var health = await _healthCheckService.CheckHealthAsync(cancellationToken);

                _healthService.SetStatus(string.Empty,
                    health.Status == HealthStatus.Healthy
                        ? HealthCheckResponse.Types.ServingStatus.Serving
                        : HealthCheckResponse.Types.ServingStatus.NotServing);

                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GrpcHealthCheck Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }


        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
