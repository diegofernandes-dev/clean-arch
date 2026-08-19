using CleanArch.Domain.Ledger;
using Xunit;

namespace CleanArch.Application.Tests.Ledger;

public sealed class LedgerTransactionTests
{
    [Fact]
    public void Create_WhenDebitsEqualCredits_CreatesTransaction()
    {
        var transaction = LedgerTransaction.Create(
            "idem-1",
            "hash",
            "payment-1",
            "Payment",
            "BRL",
            [
                new LedgerEntry(Guid.NewGuid(), EntryDirection.Debit, 100m, "BRL"),
                new LedgerEntry(Guid.NewGuid(), EntryDirection.Credit, 100m, "BRL")
            ],
            DateTimeOffset.UtcNow);

        Assert.Equal(2, transaction.Entries.Count);
    }

    [Fact]
    public void Create_WhenTransactionIsUnbalanced_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LedgerTransaction.Create(
                "idem-2",
                "hash",
                "payment-2",
                "Payment",
                "BRL",
                [
                    new LedgerEntry(Guid.NewGuid(), EntryDirection.Debit, 100m, "BRL"),
                    new LedgerEntry(Guid.NewGuid(), EntryDirection.Credit, 90m, "BRL")
                ],
                DateTimeOffset.UtcNow));

        Assert.Contains("debits must equal", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
