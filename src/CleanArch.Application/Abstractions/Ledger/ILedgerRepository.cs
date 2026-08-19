using CleanArch.Domain.Ledger;

namespace CleanArch.Application.Abstractions.Ledger;

public interface ILedgerRepository
{
    Task<LedgerTransaction?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<bool> AccountsExistAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken);

    Task<LedgerTransaction> SaveAsync(
        LedgerTransaction transaction,
        string eventPayload,
        CancellationToken cancellationToken);

    Task<LedgerTransaction?> GetAsync(
        Guid transactionId,
        CancellationToken cancellationToken);
}
