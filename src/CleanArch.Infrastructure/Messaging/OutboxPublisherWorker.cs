using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;

namespace CleanArch.Infrastructure.Messaging;

internal sealed class OutboxPublisherWorker(
    NpgsqlDataSource dataSource,
    ConnectionFactory connectionFactory,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private const string Exchange = "ledger.events";
    private const string Queue = "ledger.transaction.posted";
    private const string RoutingKey = "ledger.transaction.posted";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPublisherAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Outbox publisher disconnected; retrying");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunPublisherAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(
            "clean-arch-outbox",
            cancellationToken);

        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await channel.ExchangeDeclareAsync(
            Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            Queue,
            Exchange,
            RoutingKey,
            cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var pending = await ClaimPendingAsync(cancellationToken);

            foreach (var message in pending)
            {
                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = message.Id.ToString(),
                    Type = message.EventType
                };

                await channel.BasicPublishAsync(
                    Exchange,
                    RoutingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message.Payload),
                    cancellationToken: cancellationToken);

                await MarkProcessedAsync(message.Id, message.LockId, cancellationToken);

                logger.LogInformation(
                    "Published outbox event {EventType} with MessageId {MessageId}",
                    message.EventType,
                    message.Id);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<OutboxMessage>> ClaimPendingAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var lockId = Guid.NewGuid();

        const string sql = """
            update outbox_messages
            set lock_id = @lock_id,
                locked_until = now() + interval '30 seconds'
            where id in (
                select id
                from outbox_messages
                where processed_at is null
                  and (locked_until is null or locked_until < now())
                order by occurred_at
                for update skip locked
                limit 50
            )
            returning id, event_type, payload::text;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("lock_id", lockId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var messages = new List<OutboxMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessage(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                lockId));
        }

        await reader.DisposeAsync();
        await transaction.CommitAsync(cancellationToken);

        return messages;
    }

    private async Task MarkProcessedAsync(Guid id, Guid lockId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
            update outbox_messages
            set processed_at = now(),
                lock_id = null,
                locked_until = null
            where id = @id
              and lock_id = @lock_id
              and processed_at is null;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("lock_id", lockId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record OutboxMessage(Guid Id, string EventType, string Payload, Guid LockId);
}
