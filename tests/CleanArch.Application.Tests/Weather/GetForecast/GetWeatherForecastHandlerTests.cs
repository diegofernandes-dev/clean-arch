using CleanArch.Application.Abstractions.Weather;
using CleanArch.Application.Common.Errors;
using CleanArch.Application.Weather.GetForecast;
using Xunit;

namespace CleanArch.Application.Tests.Weather.GetForecast;

public sealed class GetWeatherForecastHandlerTests
{
    [Fact]
    public async Task Handle_WhenDaysIsOutsideRange_ReturnsValidationError()
    {
        var handler = new GetWeatherForecastHandler(new FakeWeatherService(20));
        var query = new GetWeatherForecastQuery(new DateOnly(2026, 8, 19), 15);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsLeft);
        var error = result.Match<ApplicationError?>(Right: _ => null, Left: left => left);
        Assert.NotNull(error);
        Assert.Equal("weather.days.invalid", error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_ReturnsRequestedForecastCount()
    {
        var handler = new GetWeatherForecastHandler(new FakeWeatherService(20));
        var query = new GetWeatherForecastQuery(new DateOnly(2026, 8, 19), 5);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsRight);
        var forecasts = result.Match<IReadOnlyCollection<WeatherForecastResponse>?>(Right: right => right, Left: _ => null);
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);
        Assert.All(forecasts, forecast => Assert.Equal("Mild", forecast.Summary));
    }

    private sealed class FakeWeatherService(int temperature) : IWeatherService
    {
        public Task<int?> GetTemperatureAsync(DateOnly date, CancellationToken cancellationToken) =>
            Task.FromResult<int?>(temperature);
    }
}
