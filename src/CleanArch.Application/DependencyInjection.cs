using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Weather.GetForecast;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetWeatherForecastQuery, IReadOnlyCollection<WeatherForecastResponse>>, GetWeatherForecastHandler>();
        return services;
    }
}
