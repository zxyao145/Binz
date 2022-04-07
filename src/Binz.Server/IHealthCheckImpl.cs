using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Binz.Server
{
    public class IHealthCheckImpl : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
