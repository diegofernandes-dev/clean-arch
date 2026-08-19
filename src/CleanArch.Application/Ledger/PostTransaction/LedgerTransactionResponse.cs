namespace CleanArch.Application.Ledger.PostTransaction;

public sealed record LedgerEntryResponse(
    Guid AccountId,
    string Direction,
    decimal Amount,
    string Currency);

public sealed record LedgerTransactionResponse(
    Guid Id,
    string Reference,
    string Description,
    string Currency,
    DateTimeOffset PostedAt,
    IReadOnlyCollection<LedgerEntryResponse> Entries);
