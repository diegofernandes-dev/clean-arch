using CleanArch.Application.Abstractions.Weather;
using CleanArch.Infrastructure.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IWeatherService, WeatherService>();
        return services;
    }
}
