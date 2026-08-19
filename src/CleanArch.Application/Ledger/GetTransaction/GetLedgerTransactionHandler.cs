using CleanArch.Application.Abstractions.Ledger;
using CleanArch.Application.Abstractions.Messaging;
using CleanArch.Application.Common.Errors;
using CleanArch.Application.Ledger.PostTransaction;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CleanArch.Application.Ledger.GetTransaction;

public sealed class GetLedgerTransactionHandler(ILedgerRepository repository)
    : IQueryHandler<GetLedgerTransactionQuery, LedgerTransactionResponse>
{
    public async Task<Either<ApplicationError, LedgerTransactionResponse>> Handle(
        GetLedgerTransactionQuery query,
        CancellationToken cancellationToken)
    {
        var transaction = await repository.GetAsync(query.TransactionId, cancellationToken);

        if (transaction is null)
        {
            return Left<ApplicationError, LedgerTransactionResponse>(
                ApplicationError.NotFound(
                    "ledger.transaction.not_found",
                    "Ledger transaction was not found."));
        }

        return Right<ApplicationError, LedgerTransactionResponse>(
            new LedgerTransactionResponse(
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
                        x.Currency)).ToArray()));
    }
}
