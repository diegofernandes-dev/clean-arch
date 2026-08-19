using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Ledger.GetTransaction;
using CleanArch.Application.Ledger.PostTransaction;
using CleanArch.Application.Weather.GetForecast;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<GetWeatherForecastQuery, IReadOnlyCollection<WeatherForecastResponse>>,
            GetWeatherForecastHandler>();

        services.AddScoped<
            ICommandHandler<PostLedgerTransactionCommand, LedgerTransactionResponse>,
            PostLedgerTransactionHandler>();

        services.AddScoped<
            IQueryHandler<GetLedgerTransactionQuery, LedgerTransactionResponse>,
            GetLedgerTransactionHandler>();

        return services;
    }
}
