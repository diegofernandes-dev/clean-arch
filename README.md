# Clean Architecture WeatherForecast (.NET 10)

A deliberately small example showing how to combine Clean Architecture, CQRS, Minimal APIs, LanguageExt, RFC 7807-style Problem Details, centralized exception handling, API versioning, health checks, Scalar, and structured Serilog logging without turning the classic `WeatherForecast` sample into a framework.

## Architecture

```text
HTTP
  |
  v
CleanArch.Api
  |  Minimal API / versioning / ProblemDetails / exception boundary
  v
CleanArch.Application
  |  CQRS contracts + use cases + typed expected errors
  v
CleanArch.Domain

CleanArch.Infrastructure -> CleanArch.Application
  implements application ports
```

Dependencies point inward:

- `Domain` has no dependency on ASP.NET Core, LanguageExt, logging, or infrastructure.
- `Application` owns use cases and ports. Expected failures are returned as `Either<ApplicationError, T>`.
- `Infrastructure` implements application ports.
- `Api` translates HTTP to application requests and application results back to HTTP.

## Error model

Expected failures use typed values:

```text
Validation / NotFound / Conflict / Forbidden
                  |
                  v
     Either<ApplicationError, T>
                  |
                  v
             ProblemDetails
```

Unexpected failures use exceptions:

```text
Exception -> IExceptionHandler -> 500 ProblemDetails
```

This keeps exceptions out of normal application control flow.

## CQRS

The sample intentionally does **not** use MediatR. CQRS is represented by small interfaces:

- `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>`
- `ICommand<TResponse>` / `ICommandHandler<TCommand, TResponse>`

The endpoint injects the appropriate handler directly. A mediator can be added later if pipeline behaviors become a real requirement.

## Run

Requires the .NET 10 SDK.

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project src/CleanArch.Api
```

## Endpoints

### WeatherForecast v1

```http
GET /api/v1/weather?days=5
```

Optional query parameters:

- `from`: start date (`yyyy-MM-dd`)
- `days`: number of days, from 1 to 14

### Health checks

```text
GET /health/live
GET /health/ready
```

### OpenAPI and Scalar

In `Development`:

```text
/openapi/v1.json
/scalar/v1
```

## Structured logging

Serilog is configured from `appsettings.json`, enriches request completion events, and outputs JSON to stdout.

## Intentional omissions

The sample intentionally does not add MediatR, FluentValidation, EF Core, repositories, resilience policies, authentication, authorization, Docker/Kubernetes manifests, architecture-test libraries, or observability exporters. Those should be introduced only when the example has a requirement that justifies them.
