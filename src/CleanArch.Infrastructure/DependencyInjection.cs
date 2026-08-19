using CleanArch.Application.Abstractions.Ledger;
using CleanArch.Application.Abstractions.Weather;
using CleanArch.Infrastructure.Health;
using CleanArch.Infrastructure.Ledger;
using CleanArch.Infrastructure.Messaging;
using CleanArch.Infrastructure.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RabbitMQ.Client;

namespace CleanArch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IWeatherService, WeatherService>();

        var connectionString = configuration.GetConnectionString("Ledger")
            ?? throw new InvalidOperationException("ConnectionStrings:Ledger is required.");

        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddScoped<ILedgerRepository, PostgresLedgerRepository>();

        services.AddSingleton(_ => new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "localhost",
            Port = configuration.GetValue("RabbitMq:Port", 5672),
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true
        });

        services.AddHostedService<OutboxPublisherWorker>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }
}
