using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace CleanArch.Infrastructure.Health;

internal sealed class RabbitMqHealthCheck(ConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(
                "clean-arch-health",
                cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }
    }
}
