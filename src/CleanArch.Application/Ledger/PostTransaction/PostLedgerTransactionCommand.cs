using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Domain.Ledger;

namespace CleanArch.Application.Ledger.PostTransaction;

public sealed record LedgerEntryInput(
    Guid AccountId,
    EntryDirection Direction,
    decimal Amount);

public sealed record PostLedgerTransactionCommand(
    string IdempotencyKey,
    string Reference,
    string Description,
    string Currency,
    IReadOnlyCollection<LedgerEntryInput> Entries)
    : ICommand<LedgerTransactionResponse>;
