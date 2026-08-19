using CleanArch.Application.Abstractions.Ledger;
using CleanArch.Domain.Ledger;
using Npgsql;

namespace CleanArch.Infrastructure.Ledger;

internal sealed class PostgresLedgerRepository(NpgsqlDataSource dataSource) : ILedgerRepository
{
    public async Task<LedgerTransaction?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
            select id
            from ledger_transactions
            where idempotency_key = @idempotency_key;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("idempotency_key", idempotencyKey);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is Guid id ? await GetAsync(id, cancellationToken) : null;
    }

    public async Task<bool> AccountsExistAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
            select count(*)
            from ledger_accounts
            where id = any(@ids);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ids", accountIds.ToArray());

        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        return count == accountIds.Count;
    }

    public async Task<LedgerTransaction> SaveAsync(
        LedgerTransaction transaction,
        string eventPayload,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertTransaction = """
                insert into ledger_transactions
                    (id, idempotency_key, request_hash, reference, description, currency, posted_at)
                values
                    (@id, @idempotency_key, @request_hash, @reference, @description, @currency, @posted_at);
                """;

            await using (var command = new NpgsqlCommand(insertTransaction, connection, dbTransaction))
            {
                command.Parameters.AddWithValue("id", transaction.Id);
                command.Parameters.AddWithValue("idempotency_key", transaction.IdempotencyKey);
                command.Parameters.AddWithValue("request_hash", transaction.RequestHash);
                command.Parameters.AddWithValue("reference", transaction.Reference);
                command.Parameters.AddWithValue("description", transaction.Description);
                command.Parameters.AddWithValue("currency", transaction.Currency);
                command.Parameters.AddWithValue("posted_at", transaction.PostedAt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertEntry = """
                insert into ledger_entries
                    (id, transaction_id, account_id, direction, amount, currency)
                values
                    (@id, @transaction_id, @account_id, @direction, @amount, @currency);
                """;

            foreach (var entry in transaction.Entries)
            {
                await using var command = new NpgsqlCommand(insertEntry, connection, dbTransaction);
                command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("transaction_id", transaction.Id);
                command.Parameters.AddWithValue("account_id", entry.AccountId);
                command.Parameters.AddWithValue("direction", entry.Direction.ToString().ToLowerInvariant());
                command.Parameters.AddWithValue("amount", entry.Amount);
                command.Parameters.AddWithValue("currency", entry.Currency);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertOutbox = """
                insert into outbox_messages
                    (id, event_type, aggregate_id, payload, occurred_at)
                values
                    (@id, @event_type, @aggregate_id, cast(@payload as jsonb), @occurred_at);
                """;

            await using (var command = new NpgsqlCommand(insertOutbox, connection, dbTransaction))
            {
                command.Parameters.AddWithValue("id", Guid.NewGuid());
                command.Parameters.AddWithValue("event_type", "ledger.transaction.posted");
                command.Parameters.AddWithValue("aggregate_id", transaction.Id);
                command.Parameters.AddWithValue("payload", eventPayload);
                command.Parameters.AddWithValue("occurred_at", transaction.PostedAt);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await dbTransaction.CommitAsync(cancellationToken);
            return transaction;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await dbTransaction.RollbackAsync(cancellationToken);

            return await FindByIdempotencyKeyAsync(transaction.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("Idempotency conflict occurred but the persisted transaction could not be loaded.");
        }
    }

    public async Task<LedgerTransaction?> GetAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string transactionSql = """
            select idempotency_key, request_hash, reference, description, currency, posted_at
            from ledger_transactions
            where id = @id;
            """;

        await using var transactionCommand = new NpgsqlCommand(transactionSql, connection);
        transactionCommand.Parameters.AddWithValue("id", transactionId);

        string idempotencyKey;
        string requestHash;
        string reference;
        string description;
        string currency;
        DateTimeOffset postedAt;

        await using (var reader = await transactionCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            idempotencyKey = reader.GetString(0);
            requestHash = reader.GetString(1);
            reference = reader.GetString(2);
            description = reader.GetString(3);
            currency = reader.GetString(4);
            postedAt = reader.GetFieldValue<DateTimeOffset>(5);
        }

        const string entriesSql = """
            select account_id, direction, amount, currency
            from ledger_entries
            where transaction_id = @transaction_id
            order by id;
            """;

        await using var entriesCommand = new NpgsqlCommand(entriesSql, connection);
        entriesCommand.Parameters.AddWithValue("transaction_id", transactionId);

        await using var entriesReader = await entriesCommand.ExecuteReaderAsync(cancellationToken);
        var entries = new List<LedgerEntry>();

        while (await entriesReader.ReadAsync(cancellationToken))
        {
            entries.Add(new LedgerEntry(
                entriesReader.GetGuid(0),
                Enum.Parse<EntryDirection>(entriesReader.GetString(1), true),
                entriesReader.GetDecimal(2),
                entriesReader.GetString(3)));
        }

        return LedgerTransaction.Rehydrate(
            transactionId,
            idempotencyKey,
            requestHash,
            reference,
            description,
            currency,
            postedAt,
            entries);
    }
}
