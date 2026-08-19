namespace CleanArch.Domain.Ledger;

public sealed record LedgerEntry(
    Guid AccountId,
    EntryDirection Direction,
    decimal Amount,
    string Currency);
