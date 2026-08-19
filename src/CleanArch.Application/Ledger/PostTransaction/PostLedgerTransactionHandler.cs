using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CleanArch.Application.Abstractions.Ledger;
using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Common.Errors;
using CleanArch.Domain.Ledger;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CleanArch.Application.Ledger.PostTransaction;

public sealed class PostLedgerTransactionHandler(ILedgerRepository repository)
    : ICommandHandler<PostLedgerTransactionCommand, LedgerTransactionResponse>
{
    public async Task<Either<ApplicationError, LedgerTransactionResponse>> Handle(
        PostLedgerTransactionCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return Left<ApplicationError, LedgerTransactionResponse>(
                ApplicationError.Validation(
                    "ledger.idempotency_key.required",
                    "Idempotency-Key is required."));
        }

        if (command.Entries is null || command.Entries.Count < 2)
        {
            return Left<ApplicationError, LedgerTransactionResponse>(
                ApplicationError.Validation(
                    "ledger.entries.minimum",
                    "At least two ledger entries are required."));
        }

        var requestHash = ComputeRequestHash(command);

        var existing = await repository.FindByIdempotencyKeyAsync(
            command.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return Left<ApplicationError, LedgerTransactionResponse>(
                    ApplicationError.Conflict(
                        "ledger.idempotency_key.reused",
                        "Idempotency-Key was already used with a different request."));
            }

            return Right<ApplicationError, LedgerTransactionResponse>(Map(existing));
        }

        var accountIds = command.Entries.Select(x => x.AccountId).Distinct().ToArray();

        if (!await repository.AccountsExistAsync(accountIds, cancellationToken))
        {
            return Left<ApplicationError, LedgerTransactionResponse>(
                ApplicationError.Validation(
                    "ledger.account.invalid",
                    "One or more ledger accounts do not exist."));
        }

        LedgerTransaction transaction;
        try
        {
            transaction = LedgerTransaction.Create(
                command.IdempotencyKey,
                requestHash,
                command.Reference,
                command.Description,
                command.Currency,
                command.Entries.Select(x =>
                    new LedgerEntry(
                        x.AccountId,
                        x.Direction,
                        x.Amount,
                        command.Currency)),
                DateTimeOffset.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return Left<ApplicationError, LedgerTransactionResponse>(
                ApplicationError.Validation(
                    "ledger.transaction.invalid",
                    exception.Message));
        }

        var eventPayload = JsonSerializer.Serialize(new
        {
            eventType = "ledger.transaction.posted",
            transactionId = transaction.Id,
            reference = transaction.Reference,
            currency = transaction.Currency,
            postedAt = transaction.PostedAt
        });

        var persisted = await repository.SaveAsync(
            transaction,
            eventPayload,
            cancellationToken);

        if (!string.Equals(persisted.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return Left<ApplicationError, LedgerTransactionResponse>(
                ApplicationError.Conflict(
                    "ledger.idempotency_key.reused",
                    "Idempotency-Key was already used with a different request."));
        }

        return Right<ApplicationError, LedgerTransactionResponse>(Map(persisted));
    }

    private static string ComputeRequestHash(PostLedgerTransactionCommand command)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            reference = command.Reference?.Trim(),
            description = command.Description?.Trim(),
            currency = command.Currency?.Trim().ToUpperInvariant(),
            entries = command.Entries.Select(x => new
            {
                accountId = x.AccountId,
                direction = x.Direction.ToString(),
                amount = x.Amount
            })
        });

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();
    }

    private static LedgerTransactionResponse Map(LedgerTransaction transaction) =>
        new(
            transaction.Id,
            transaction.Reference,
            transaction.Description,
            transaction.Currency,
            transaction.PostedAt,
            transaction.Entries.Select(x =>
                new LedgerEntryResponse(
                    x.AccountId,
                    x.Direction.ToString(),
                    x.Amount,
                    x.Currency)).ToArray());
}
