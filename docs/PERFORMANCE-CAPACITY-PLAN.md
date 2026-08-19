# Performance & Capacity Engineering Plan

## Purpose

This document defines how to determine the minimum resources required for the API to meet a declared service-level objective (SLO), how to identify the first saturated dependency, and when scaling infrastructure is justified.

The goal is not to find the absolute maximum requests per second (RPS). The goal is to establish a repeatable **safe capacity envelope** for representative workloads and to use evidence before increasing infrastructure cost.

A capacity result is always conditional on the tested application version, workload, dependency topology, data volume, runtime configuration and infrastructure profile.

## Questions this plan must answer

For each critical workload, testing must answer:

1. What sustained RPS can one replica process while meeting the SLO?
2. What resource profile is the minimum that meets the target workload?
3. Where is the knee of the latency curve?
4. What saturates first: application CPU, memory/GC, thread pool, PostgreSQL, connection pool, RabbitMQ, network, external dependency or another component?
5. Does adding CPU/memory materially increase safe capacity?
6. Does horizontal scaling increase capacity approximately linearly?
7. What operating headroom is required before the breaking point?
8. What HPA target is supported by measured behavior rather than convention?
9. What is the safe RPS per pod and the approximate RPS per CPU core?
10. What is the marginal capacity gained by each larger resource profile?

## Definitions

### SLO

A workload-specific target such as:

```text
p95 < 250 ms
p99 < 500 ms
HTTP error rate < 0.1%
timeout rate < 0.1%
```

The example values above are placeholders. A real service must declare its SLO before running capacity tests.

### Breaking point

The lowest load at which the workload consistently violates an SLO or a critical dependency saturates.

### Knee of the curve

The region where a small increase in load causes a disproportionate increase in latency, errors or resource saturation.

### Safe capacity

The highest sustained load accepted for normal operation after applying headroom below the measured breaking point.

Safe capacity must not be equal to the breaking point.

### Capacity profile

A versioned record of the application version, workload, SLO, resource profile, safe RPS, breaking point, bottleneck and relevant observations.

## Test principles

### Use arrival-rate based load

Capacity tests should control request arrival rate (RPS), not only virtual-user count. The load generator must continue attempting the configured arrival rate even when the application becomes slower.

### Test representative workloads independently

Do not treat the API as having one universal capacity number.

At minimum, characterize:

- `POST /api/v1/ledger/transactions` as a transactional write workload;
- `GET /api/v1/ledger/transactions/{id}` as a read workload.

Additional production endpoints should receive their own profiles when their cost characteristics differ materially.

### Isolate the unit under test

The load generator must not compete with the API for the same constrained CPU/memory during formal runs.

### Warm up before measurement

Exclude startup, JIT and cold-cache effects from steady-state measurements unless cold-start behavior is explicitly being tested.

### Change one primary variable at a time

For resource characterization, keep workload and topology stable while changing CPU/memory. For load characterization, keep resources stable while changing RPS.

### Preserve realistic dependencies

A ledger capacity test that replaces PostgreSQL with an in-memory store is not a capacity test of the deployed ledger architecture.

## Required observability

During every run, capture at least:

### Request-level

- attempted RPS;
- completed RPS;
- p50 latency;
- p95 latency;
- p99 latency;
- HTTP error rate;
- timeout rate;
- response status distribution.

### Application runtime

- CPU utilization;
- CPU throttling when containerized;
- working set / memory usage;
- allocation rate;
- GC pause/activity;
- thread-pool queueing/starvation indicators;
- process/container restarts.

### PostgreSQL

- CPU utilization;
- query latency;
- active connections;
- connection-pool utilization/waiting;
- lock waits;
- transaction duration;
- slow queries;
- storage I/O where applicable.

### RabbitMQ / outbox

- publish latency/failures;
- connection/channel failures;
- queue depth;
- outbox pending count;
- age of oldest unprocessed outbox message;
- outbox publish throughput.

### Distributed traces

Use traces to distinguish application processing time from database, messaging and external dependency latency. Do not enable sensitive request/response body capture for performance testing.

## Baseline workload

The first capacity profile should use the ledger write endpoint because it exercises application logic, PostgreSQL transactions and the outbox path.

Example logical request:

```text
POST /api/v1/ledger/transactions
Idempotency-Key: unique-per-operation

Debit  100 BRL -> MERCHANT-SETTLEMENT
Credit 100 BRL -> CUSTOMER-CASH
```

Each logical operation must use a unique idempotency key unless the scenario is explicitly testing duplicate delivery/idempotency.

## Resource matrix

Start with a deliberately small profile and increase resources only after measurement.

Example matrix:

| Profile | CPU request/limit | Memory request/limit |
|---|---:|---:|
| XS | 250m | 256Mi |
| S | 500m | 512Mi |
| M | 1000m | 1Gi |
| L | 2000m | 2Gi |

These values are test coordinates, not recommendations.

In Kubernetes, prefer running each formal capacity pass with a fixed replica count of one and without HPA so the capacity of one replica can be characterized first.

## Phase 1 - Capacity ramp

Purpose: find the knee and breaking point for a fixed resource profile.

For each profile:

1. warm up the application;
2. start at a clearly sustainable arrival rate;
3. hold each load step long enough to reach a steady state;
4. increase RPS in fixed or proportional increments;
5. stop after the SLO is violated consistently or a dependency reaches a hard saturation point;
6. repeat the neighborhood around the knee using smaller increments.

Example progression:

```text
25 -> 50 -> 75 -> 100 -> 125 -> 150 -> 175 -> 200 RPS
```

The exact progression should be configurable.

A single short spike is not sufficient to declare capacity.

## Phase 2 - Resource efficiency

Purpose: determine the minimum resource profile that supports the target workload.

For the declared target RPS, compare resource profiles using the same workload and environment.

Record:

- safe RPS;
- CPU actually consumed;
- memory actually consumed;
- p95/p99;
- error/timeout rate;
- primary saturation signal.

Evaluate marginal efficiency:

```text
capacity gain / resource gain
```

Example interpretation:

```text
250m -> 500m CPU: safe capacity 80 -> 190 RPS
500m -> 1000m CPU: safe capacity 190 -> 240 RPS
```

The second increase doubles CPU for little additional throughput. The 500m profile may therefore be the economic sweet spot even though 1000m has a higher absolute breaking point.

## Phase 3 - Soak test

Purpose: detect degradation that short capacity runs miss.

Run a representative load below the safe-capacity boundary for an extended period, for example 1-4 hours depending on the service risk and test environment.

Look for:

- memory growth;
- worsening GC behavior;
- connection leaks;
- thread-pool degradation;
- increasing database latency;
- lock accumulation;
- outbox backlog growth;
- RabbitMQ instability;
- latency drift.

The service must not be declared production-capable solely from a short ramp test.

## Phase 4 - Spike and recovery

Purpose: validate behavior during abrupt bursts and recovery afterward.

Example:

```text
100 RPS -> 500 RPS -> 100 RPS
```

Measure:

- latency during spike;
- timeout/error behavior;
- whether the API recovers without restart;
- database recovery;
- connection-pool recovery;
- outbox backlog creation and drain time.

A system that meets steady-state SLO but does not recover after a burst needs remediation before simply adding replicas.

## Phase 5 - Horizontal scaling

Only after single-replica capacity is known, repeat tests with multiple replicas.

Example:

```text
1 pod -> 2 pods -> 4 pods
```

Compare observed safe capacity with expected capacity based on the one-pod baseline.

If one pod safely handles 180 RPS, four pods should not be assumed to handle 720 RPS without measurement. Shared bottlenecks such as PostgreSQL, connection limits or downstream systems can make scaling sub-linear.

Record scaling efficiency:

```text
observed multi-pod capacity / theoretical linear capacity
```

## Bottleneck classification

The test report should identify the first credible saturation mechanism.

### CPU-bound application

Typical evidence:

- API CPU approaches saturation;
- latency rises strongly with CPU saturation;
- database remains healthy;
- increasing CPU or replicas materially improves safe RPS.

Action: optimize CPU-heavy paths or scale application compute.

### Memory / GC-bound

Typical evidence:

- allocation rate and GC activity grow sharply;
- latency spikes correlate with GC;
- memory pressure or OOM/restarts appear;
- CPU may not be fully saturated.

Action: reduce allocations/object retention before blindly increasing memory.

### Database-bound

Typical evidence:

- API CPU has headroom;
- database CPU, I/O, locks or query latency saturate;
- connection pool waits increase;
- increasing API CPU does not materially improve throughput.

Action: inspect query plans, indexes, locking, transaction scope, schema and database capacity.

### Connection-pool-bound

Typical evidence:

- requests wait for database connections;
- application and database CPU can both show headroom;
- increasing pool size changes behavior temporarily or shifts the bottleneck.

Action: first understand transaction/query duration and concurrency. Do not increase pool size automatically.

### Messaging/outbox-bound

Typical evidence:

- HTTP write path remains healthy but pending outbox count/age grows;
- publisher throughput is lower than event production rate;
- RabbitMQ publish latency or failures increase.

Action: optimize/potentially scale publishers or broker topology while preserving delivery guarantees.

### External-dependency-bound

Typical evidence:

- traces attribute most latency to a downstream call;
- API CPU has headroom;
- adding local resources has little effect.

Action: address downstream SLO, timeout, caching or architectural coupling rather than local compute.

## Safe capacity decision

The final safe RPS must include explicit headroom below the breaking point.

Do not define a universal percentage. Select headroom based on variability, workload criticality, dependency volatility and failure-domain requirements.

Example:

```text
breaking point: 245 RPS/pod
knee begins:     215 RPS/pod
safe capacity:  180 RPS/pod
```

The safe value becomes the planning input, not the breaking point.

## HPA calibration

Do not pick CPU utilization targets only because a percentage is conventional.

Correlate CPU utilization with SLO behavior during the capacity ramp.

Example observation:

```text
55% CPU -> p95 120 ms
65% CPU -> p95 150 ms
75% CPU -> p95 210 ms
85% CPU -> p95 430 ms
```

If the latency curve deteriorates above ~75%, an HPA target below that region can be justified experimentally.

HPA validation must then include a separate test for scale-up speed, scale-down behavior and burst handling.

## Cost-aware capacity

For each resource profile, record a normalized capacity-efficiency metric such as:

```text
safe RPS / requested CPU core
```

and, when cost data is available:

```text
safe RPS / hourly workload cost
```

The objective is not to maximize utilization at any cost. It is to find the lowest-cost profile that meets the SLO with sufficient headroom and predictable scaling behavior.

## Pass / fail criteria

A capacity run passes only when all declared SLO thresholds are met for the required steady-state interval and no hidden saturation signal invalidates the result.

A run is invalid if, for example:

- the load generator cannot sustain requested arrival rate;
- dependencies use different configurations between compared runs;
- data volume or cache state changes materially without being recorded;
- telemetry is missing for the suspected bottleneck;
- warm-up was included unintentionally;
- a shared test environment introduces uncontrolled competing load.

## Reproducibility requirements

Every formal run must record:

- git commit SHA;
- container image digest when available;
- test script/scenario version;
- runtime/.NET version;
- resource requests and limits;
- replica count;
- HPA state;
- PostgreSQL/RabbitMQ versions and sizing;
- relevant connection-pool settings;
- dataset size/state;
- test duration;
- target RPS;
- environment identifier;
- timestamp.

Capacity numbers without this context must not be treated as durable facts.

## Capacity profile output

Every characterized workload should produce a profile using `docs/templates/CAPACITY-PROFILE.md`.

At minimum it must state:

```text
workload
SLO
resource profile
safe RPS per pod
breaking point
knee of curve
primary bottleneck
p95 / p99 at safe capacity
error rate
timeout rate
CPU and memory at safe capacity
DB indicators
outbox/RabbitMQ indicators
scaling efficiency
recommended HPA signal/target, if validated
```

## Proposed automation sequence

This document defines the methodology only. Implementation should be incremental:

1. add k6 arrival-rate scenarios for ledger write/read;
2. encode SLO thresholds in k6;
3. add capacity, soak and spike scenarios;
4. add Docker/local execution for developer validation;
5. add a Kubernetes test harness that applies one resource profile at a time;
6. collect OpenTelemetry and platform metrics for each run;
7. generate a machine-readable result artifact;
8. generate/update a capacity profile from the result;
9. optionally compare profiles across commits to detect performance regressions.

The automation must not automatically recommend more infrastructure without first reporting the observed bottleneck.

## Current example target

For the ledger sample, the initial characterization should focus on:

```text
POST /api/v1/ledger/transactions
```

with placeholder SLO values until the service owner defines real ones:

```text
p95 < 250 ms
p99 < 500 ms
errors < 0.1%
timeouts < 0.1%
```

The first implementation milestone is complete when the repository can reproducibly answer:

> For this commit and this environment, what is the minimum pod resource profile that sustains the target ledger RPS within the declared SLO, what is the safe RPS per pod, and what component saturates first?
