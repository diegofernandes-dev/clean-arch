namespace CleanArch.Application.Abstractions.Weather;

public interface IWeatherService
{
    Task<int?> GetTemperatureAsync(DateOnly date, CancellationToken cancellationToken);
}
