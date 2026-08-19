using System.Diagnostics;
using System.Text.Json.Serialization;
using Asp.Versioning;
using CleanArch.Api.Endpoints;
using CleanArch.Api.ExceptionHandling;
using CleanArch.Api.Observability;
using CleanArch.Application;
using CleanArch.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Telemetry;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting CleanArch API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.EnableRedaction(options =>
    {
        options.ApplyDiscriminator = true;
    });

    // Classified values are erased by default. A reversible/correlatable redactor is
    // intentionally not configured for this financial baseline.
    builder.Services.AddRedaction();

    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddFinancialObservability(builder.Configuration);

    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] =
                Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddRequestTimeouts(options =>
    {
        options.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromMilliseconds(
                builder.Configuration.GetValue("RequestTimeouts:DefaultMilliseconds", 2000)),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
        };
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1.0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    builder.Services.AddOpenApi("v1");

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseRequestTimeouts();

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set(
                "TraceId",
                Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
            diagnosticContext.Set("SpanId", Activity.Current?.SpanId.ToString());
        };
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithTitle("Financial Clean Architecture API"));
    }

    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1.0))
        .ReportApiVersions()
        .Build();

    app.MapWeatherEndpoints(versionSet);
    app.MapLedgerEndpoints(versionSet);

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
