using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Ledger.PostTransaction;

namespace CleanArch.Application.Ledger.GetTransaction;

public sealed record GetLedgerTransactionQuery(Guid TransactionId)
    : IQuery<LedgerTransactionResponse>;
