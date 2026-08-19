using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CleanArch.Api.Observability;

internal static class ObservabilityExtensions
{
    internal static IServiceCollection AddFinancialObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "clean-arch-financial-api";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        var openTelemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName));

        openTelemetry.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
            });

            if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
            {
                tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
            }
        });

        openTelemetry.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation();

            if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
            {
                metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
            }
        });

        return services;
    }
}
