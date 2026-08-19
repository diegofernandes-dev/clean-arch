# Capacity Profile

> Copy this template for each formally characterized workload. Capacity values are valid only for the application version and environment recorded below.

## Identity

| Field | Value |
|---|---|
| Service | |
| Workload / endpoint | |
| Git commit SHA | |
| Image digest | |
| Test scenario version | |
| Test date | |
| Environment | |
| .NET/runtime version | |

## SLO

| Signal | Target | Measured at safe capacity |
|---|---:|---:|
| p95 latency | | |
| p99 latency | | |
| HTTP error rate | | |
| timeout rate | | |

## Resource profile

| Setting | Value |
|---|---|
| CPU request | |
| CPU limit | |
| Memory request | |
| Memory limit | |
| Replica count | |
| HPA | disabled / enabled |
| HPA target | |

## Capacity result

| Metric | Result |
|---|---:|
| Safe RPS per pod | |
| Knee begins around | |
| Breaking point | |
| RPS per requested CPU core | |
| Multi-pod scaling efficiency | |

## Resource utilization at safe capacity

| Signal | Result |
|---|---:|
| API CPU | |
| CPU throttling | |
| API memory | |
| Allocation rate | |
| GC observations | |
| Thread-pool observations | |

## PostgreSQL

| Signal | Result |
|---|---:|
| Query latency p95 | |
| CPU | |
| Active connections | |
| Pool waiting / saturation | |
| Lock waits | |
| Slow-query observations | |

## RabbitMQ / outbox

| Signal | Result |
|---|---:|
| Publish throughput | |
| Publish errors | |
| Queue depth | |
| Pending outbox count | |
| Oldest outbox message age | |
| Backlog drain time after spike | |

## Bottleneck assessment

**Primary bottleneck:**

- [ ] application CPU
- [ ] memory / GC
- [ ] thread pool
- [ ] PostgreSQL compute / I/O
- [ ] database locks
- [ ] connection pool
- [ ] RabbitMQ / outbox publisher
- [ ] external dependency
- [ ] network
- [ ] not yet determined

### Evidence

Describe the telemetry that supports the classification.

## Scaling behavior

Record results for 1, 2 and 4 replicas when horizontal scaling is tested.

| Replicas | Expected linear RPS | Observed safe RPS | Efficiency |
|---:|---:|---:|---:|
| 1 | | | 100% |
| 2 | | | |
| 4 | | | |

## Resource-efficiency comparison

| Profile | CPU | Memory | Safe RPS | p95 | p99 | Cost / capacity observation |
|---|---:|---:|---:|---:|---:|---|
| XS | | | | | | |
| S | | | | | | |
| M | | | | | | |
| L | | | | | | |

## Recommended operating envelope

```text
safe RPS/pod:
recommended minimum replicas:
recommended CPU request:
recommended memory request:
recommended HPA signal/target:
required operational headroom:
```

## Optimization recommendation

State what should be attacked **before adding infrastructure**. Examples: query/index, transaction duration, allocation hot path, pool contention, downstream latency, outbox throughput, CPU optimization, or horizontal scaling.

## Test validity

- [ ] load generator sustained requested arrival rate
- [ ] warm-up excluded from measurement
- [ ] dependency versions/sizing recorded
- [ ] dataset/cache state recorded
- [ ] telemetry available for suspected bottleneck
- [ ] no uncontrolled competing load identified
- [ ] capacity was sustained for the required interval
- [ ] safe capacity includes headroom below breaking point

## Notes

Add anomalies, compromises, environmental constraints and links to dashboards/test artifacts here.
