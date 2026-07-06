# Basic Incident Runbooks

## API 5xx Spike

- Check Application Insights failures and dependency failures.
- Filter logs by `CorrelationId`.
- Check recent deployments and slot swap history.
- If caused by deployment, swap back or redeploy the previous artifact.

## High Latency

- Check p95 and p99 latency.
- Check database duration and failed dependency calls.
- Check Redis and Service Bus health if enabled.
- Scale out stateless App Service instances if the bottleneck is application CPU or request queueing.

## Readiness Failure

- Check `/health/ready` output to identify the failed dependency.
- If `application-database` is unhealthy, check database connectivity, firewall/private endpoint rules, and recent migrations.
- Do not swap a production staging slot if readiness is unhealthy.

## Failed Reservations

- Check reservation creation errors and database constraint violations.
- Check payment state transitions if payment is involved.
- Verify no background worker backlog is affecting expiration or confirmation processing.

## Suspected Secret Exposure

- Revoke and rotate the affected secret immediately.
- Review git history and CI logs.
- Confirm TEST and PROD secrets are separate.
- Add or tighten secret scanning rules.
