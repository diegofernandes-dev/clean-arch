using Asp.Versioning;
using CleanArch.Api.Extensions;
using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Weather.GetForecast;

namespace CleanArch.Api.Endpoints;

public static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/weather")
            .WithTags("Weather")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1.0);

        group.MapGet("/", GetWeatherForecast)
            .WithName("GetWeatherForecast")
            .WithSummary("Returns a generated weather forecast")
            .WithDescription("Classic WeatherForecast sample implemented with Clean Architecture, CQRS and LanguageExt.")
            .Produces<IReadOnlyCollection<WeatherForecastResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> GetWeatherForecast(
        DateOnly? from,
        int? days,
        IQueryHandler<GetWeatherForecastQuery, IReadOnlyCollection<WeatherForecastResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWeatherForecastQuery(from ?? DateOnly.FromDateTime(DateTime.UtcNow), days ?? 5);
        var result = await handler.Handle(query, cancellationToken);
        return result.ToHttpResult();
    }
}
