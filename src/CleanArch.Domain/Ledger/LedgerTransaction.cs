namespace CleanArch.Domain.Ledger;

public sealed class LedgerTransaction
{
    private LedgerTransaction(
        Guid id,
        string idempotencyKey,
        string requestHash,
        string reference,
        string description,
        string currency,
        DateTimeOffset postedAt,
        IReadOnlyCollection<LedgerEntry> entries)
    {
        Id = id;
        IdempotencyKey = idempotencyKey;
        RequestHash = requestHash;
        Reference = reference;
        Description = description;
        Currency = currency;
        PostedAt = postedAt;
        Entries = entries;
    }

    public Guid Id { get; }
    public string IdempotencyKey { get; }
    public string RequestHash { get; }
    public string Reference { get; }
    public string Description { get; }
    public string Currency { get; }
    public DateTimeOffset PostedAt { get; }
    public IReadOnlyCollection<LedgerEntry> Entries { get; }

    public static LedgerTransaction Create(
        string idempotencyKey,
        string requestHash,
        string reference,
        string description,
        string currency,
        IEnumerable<LedgerEntry> entries,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var materialized = entries.ToArray();

        if (materialized.Length < 2)
            throw new ArgumentException("A ledger transaction requires at least two entries.", nameof(entries));

        if (materialized.Any(entry => entry.Amount <= 0))
            throw new ArgumentException("Ledger entry amounts must be greater than zero.", nameof(entries));

        if (materialized.Any(entry => !string.Equals(entry.Currency, currency, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("All entries must use the transaction currency.", nameof(entries));

        var debits = materialized.Where(x => x.Direction == EntryDirection.Debit).Sum(x => x.Amount);
        var credits = materialized.Where(x => x.Direction == EntryDirection.Credit).Sum(x => x.Amount);

        if (debits != credits)
            throw new ArgumentException("Total debits must equal total credits.", nameof(entries));

        return new LedgerTransaction(
            Guid.NewGuid(),
            idempotencyKey.Trim(),
            requestHash,
            reference.Trim(),
            description?.Trim() ?? string.Empty,
            currency.Trim().ToUpperInvariant(),
            now,
            materialized);
    }

    public static LedgerTransaction Rehydrate(
        Guid id,
        string idempotencyKey,
        string requestHash,
        string reference,
        string description,
        string currency,
        DateTimeOffset postedAt,
        IReadOnlyCollection<LedgerEntry> entries) =>
        new(id, idempotencyKey, requestHash, reference, description, currency, postedAt, entries);
}
