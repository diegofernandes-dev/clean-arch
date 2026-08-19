using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Abstractions.Weather;
using CleanArch.Application.Common.Errors;
using CleanArch.Domain.Weather;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CleanArch.Application.Weather.GetForecast;

public sealed class GetWeatherForecastHandler(IWeatherService weatherService)
    : IQueryHandler<GetWeatherForecastQuery, IReadOnlyCollection<WeatherForecastResponse>>
{
    public async Task<Either<ApplicationError, IReadOnlyCollection<WeatherForecastResponse>>> Handle(GetWeatherForecastQuery query, CancellationToken cancellationToken)
    {
        if (query.Days is < 1 or > 14)
        {
            return Left<ApplicationError, IReadOnlyCollection<WeatherForecastResponse>>(
                ApplicationError.Validation("weather.days.invalid", "Days must be between 1 and 14."));
        }

        var forecasts = new List<WeatherForecastResponse>(query.Days);
        for (var index = 0; index < query.Days; index++)
        {
            var date = query.From.AddDays(index);
            var temperature = await weatherService.GetTemperatureAsync(date, cancellationToken);
            if (temperature is null)
            {
                return Left<ApplicationError, IReadOnlyCollection<WeatherForecastResponse>>(
                    ApplicationError.NotFound("weather.forecast.not_found", $"Weather forecast for {date:yyyy-MM-dd} was not found."));
            }

            var forecast = new WeatherForecast(date, temperature.Value, GetSummary(temperature.Value));
            forecasts.Add(new WeatherForecastResponse(forecast.Date, forecast.TemperatureC, forecast.TemperatureF, forecast.Summary));
        }

        return Right<ApplicationError, IReadOnlyCollection<WeatherForecastResponse>>(forecasts);
    }

    private static string GetSummary(int temperature) => temperature switch
    {
        <= 0 => "Freezing",
        <= 10 => "Cold",
        <= 20 => "Mild",
        <= 30 => "Warm",
        _ => "Hot"
    };
}
