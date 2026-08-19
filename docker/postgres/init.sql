create table ledger_accounts (
    id uuid primary key,
    code varchar(64) not null unique,
    name varchar(200) not null,
    account_type varchar(32) not null,
    currency char(3) not null,
    created_at timestamptz not null default now(),
    unique (id, currency),
    constraint ck_ledger_accounts_type
        check (account_type in ('asset', 'liability', 'equity', 'revenue', 'expense'))
);

create table ledger_transactions (
    id uuid primary key,
    idempotency_key varchar(200) not null unique,
    request_hash char(64) not null,
    reference varchar(100) not null,
    description varchar(500) not null,
    currency char(3) not null,
    posted_at timestamptz not null,
    unique (id, currency)
);

create table ledger_entries (
    id uuid primary key,
    transaction_id uuid not null,
    account_id uuid not null,
    direction varchar(6) not null,
    amount numeric(20,4) not null,
    currency char(3) not null,
    constraint ck_ledger_entries_direction check (direction in ('debit', 'credit')),
    constraint ck_ledger_entries_amount check (amount > 0),
    constraint fk_entries_transaction
        foreign key (transaction_id, currency)
        references ledger_transactions (id, currency),
    constraint fk_entries_account
        foreign key (account_id, currency)
        references ledger_accounts (id, currency)
);

create index ix_ledger_entries_transaction on ledger_entries(transaction_id);
create index ix_ledger_entries_account on ledger_entries(account_id);

create table outbox_messages (
    id uuid primary key,
    event_type varchar(200) not null,
    aggregate_id uuid not null,
    payload jsonb not null,
    occurred_at timestamptz not null,
    processed_at timestamptz null,
    lock_id uuid null,
    locked_until timestamptz null
);

create index ix_outbox_unprocessed
    on outbox_messages(occurred_at)
    where processed_at is null;

create or replace function enforce_balanced_ledger_transaction()
returns trigger
language plpgsql
as $$
declare
    debit_total numeric(20,4);
    credit_total numeric(20,4);
begin
    select
        coalesce(sum(amount) filter (where direction = 'debit'), 0),
        coalesce(sum(amount) filter (where direction = 'credit'), 0)
    into debit_total, credit_total
    from ledger_entries
    where transaction_id = new.transaction_id;

    if debit_total <> credit_total then
        raise exception 'Ledger transaction % is unbalanced: debits %, credits %',
            new.transaction_id, debit_total, credit_total
            using errcode = '23514';
    end if;

    return null;
end;
$$;

create constraint trigger trg_ledger_entries_balanced
after insert on ledger_entries
deferrable initially deferred
for each row
execute function enforce_balanced_ledger_transaction();

create or replace function reject_ledger_mutation()
returns trigger
language plpgsql
as $$
begin
    raise exception 'Posted ledger records are immutable';
end;
$$;

create trigger trg_ledger_entries_immutable
before update or delete on ledger_entries
for each row
execute function reject_ledger_mutation();

create trigger trg_ledger_transactions_immutable
before update or delete on ledger_transactions
for each row
execute function reject_ledger_mutation();

insert into ledger_accounts(id, code, name, account_type, currency)
values
    ('11111111-1111-1111-1111-111111111111', 'CUSTOMER-CASH', 'Customer cash', 'asset', 'BRL'),
    ('22222222-2222-2222-2222-222222222222', 'MERCHANT-SETTLEMENT', 'Merchant settlement', 'asset', 'BRL');
