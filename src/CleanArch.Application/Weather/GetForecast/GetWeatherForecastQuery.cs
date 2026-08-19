using CleanArch.Application.Abstractions.Messaging;

namespace CleanArch.Application.Weather.GetForecast;

public sealed record GetWeatherForecastQuery(DateOnly From, int Days) : IQuery<IReadOnlyCollection<WeatherForecastResponse>>;
