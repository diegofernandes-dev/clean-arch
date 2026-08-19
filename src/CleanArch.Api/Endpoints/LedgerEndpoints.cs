using Asp.Versioning;
using CleanArch.Api.Extensions;
using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Ledger.GetTransaction;
using CleanArch.Application.Ledger.PostTransaction;
using CleanArch.Domain.Ledger;

namespace CleanArch.Api.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(
        this IEndpointRouteBuilder app,
        ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/ledger")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1.0)
            .WithTags("Ledger");

        group.MapPost("/transactions", PostTransaction)
            .Produces<LedgerTransactionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/transactions/{transactionId:guid}", GetTransaction)
            .Produces<LedgerTransactionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> PostTransaction(
        HttpContext httpContext,
        PostLedgerTransactionRequest request,
        ICommandHandler<PostLedgerTransactionCommand, LedgerTransactionResponse> handler,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();

        var command = new PostLedgerTransactionCommand(
            idempotencyKey,
            request.Reference,
            request.Description,
            request.Currency,
            (request.Entries ?? []).Select(x =>
                new LedgerEntryInput(
                    x.AccountId,
                    x.Direction,
                    x.Amount)).ToArray());

        var result = await handler.Handle(command, cancellationToken);

        return result.Match<IResult>(
            Right: response => Results.Created(
                $"/api/v1/ledger/transactions/{response.Id}",
                response),
            Left: error => error.ToProblem());
    }

    private static Task<IResult> GetTransaction(
        Guid transactionId,
        IQueryHandler<GetLedgerTransactionQuery, LedgerTransactionResponse> handler,
        CancellationToken cancellationToken) =>
        handler.Handle(
                new GetLedgerTransactionQuery(transactionId),
                cancellationToken)
            .ToResult();

    private sealed record PostLedgerTransactionRequest(
        string Reference,
        string Description,
        string Currency,
        IReadOnlyCollection<LedgerEntryRequest>? Entries);

    private sealed record LedgerEntryRequest(
        Guid AccountId,
        EntryDirection Direction,
        decimal Amount);
}
