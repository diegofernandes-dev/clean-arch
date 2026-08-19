namespace CleanArch.Application.Weather.GetForecast;

public sealed record WeatherForecastResponse(DateOnly Date, int TemperatureC, int TemperatureF, string Summary);
