using CleanArch.Application.Abstractions.Weather;

namespace CleanArch.Infrastructure.Weather;

internal sealed class WeatherService : IWeatherService
{
    public Task<int?> GetTemperatureAsync(DateOnly date, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int? temperature = Random.Shared.Next(-20, 55);
        return Task.FromResult(temperature);
    }
}
