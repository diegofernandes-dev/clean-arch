# Financial Clean Architecture API

A .NET 10 reference API that starts with the classic WeatherForecast example and evolves it toward a financial-service baseline.

## Architecture

```text
API
  -> Application (CQRS + LanguageExt Either)
      -> Domain
      -> Infrastructure ports
          -> PostgreSQL
          -> RabbitMQ
```

The sample intentionally avoids MediatR and framework-style abstractions. Expected failures use `Either<ApplicationError,T>`; unexpected failures flow through `IExceptionHandler` and Problem Details.

## Financial ledger

The ledger implements double-entry journal transactions:

- at least two entries;
- positive amounts only;
- one currency per transaction;
- total debits must equal total credits;
- database constraints also enforce account/transaction currency and balanced postings;
- every POST requires `Idempotency-Key`;
- reusing a key with a different request returns `409 Conflict`.

The ledger write and its integration event are persisted atomically in PostgreSQL. A transactional outbox worker later publishes `ledger.transaction.posted` to RabbitMQ using publisher confirms. Outbox claiming uses `FOR UPDATE SKIP LOCKED`, so multiple API replicas can safely run workers. Delivery is intentionally **at least once**; consumers must be idempotent.

## Local stack

```bash
docker compose up --build
```

Services:

- API: `http://localhost:8080`
- Scalar: `http://localhost:8080/scalar/v1`
- PostgreSQL: `localhost:5432`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ management: `http://localhost:15672` (`ledger` / `ledger`)

Readiness checks PostgreSQL and RabbitMQ. Liveness only checks the process.

## Seed accounts

The Docker database initializes two BRL accounts:

| Account | ID |
|---|---|
| CUSTOMER-CASH | `11111111-1111-1111-1111-111111111111` |
| MERCHANT-SETTLEMENT | `22222222-2222-2222-2222-222222222222` |

## Post a ledger transaction

```bash
curl -i http://localhost:8080/api/v1/ledger/transactions \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: payment-0001' \
  -d '{
    "reference": "payment-0001",
    "description": "Merchant settlement",
    "currency": "BRL",
    "entries": [
      {
        "accountId": "22222222-2222-2222-2222-222222222222",
        "direction": "Debit",
        "amount": 100.00
      },
      {
        "accountId": "11111111-1111-1111-1111-111111111111",
        "direction": "Credit",
        "amount": 100.00
      }
    ]
  }'
```

Query it with:

```bash
curl http://localhost:8080/api/v1/ledger/transactions/{transactionId}
```

## SLA-oriented behavior

A global ASP.NET Core request-timeout policy defaults to 2 seconds and propagates cancellation through endpoint, handler and PostgreSQL calls. The value is configurable with `RequestTimeouts__DefaultMilliseconds`.

Request/response bodies are deliberately not logged. Serilog request logging records structured request metadata and trace IDs, while the outbox worker logs only event type and message ID—not event payloads.

## Health

```text
GET /health/live
GET /health/ready
```

## What is intentionally not included yet

Authentication/authorization, OpenTelemetry exporters, data-classification/redaction packages, distributed caching, rate limiting, external secret stores and a business consumer are intentionally left out until explicitly selected for the template.
