# Architecture Overview

BetterBooking starts as a secure modular monolith.

## Principles

- Keep the system simple until scale or team boundaries justify more complexity.
- Keep business rules in the Domain and Application layers.
- Keep controllers and endpoints thin.
- Keep infrastructure concerns isolated from domain rules.
- Treat the database as the source of truth for reservations and availability.
- Keep application instances stateless for horizontal scaling.

## Backend Layers

- `App.Api`: HTTP API, middleware, endpoint wiring, health checks, API contracts.
- `App.Application`: use cases, DTOs, validation, authorization decisions close to use cases.
- `App.Domain`: core business rules and invariants.
- `App.Infrastructure`: persistence, Azure SDK integrations, caching, messaging, external providers.
- `App.Worker`: asynchronous processing for Service Bus messages and scheduled work.

## Implemented Backend Modules

- Users: maps authenticated external identities to internal user ids.
- Listings: supports owner-owned draft creation and owner-only publishing.
- Reservations: creates pending reservations with database-backed overlap protection.
- Audit: stores security-relevant actions without sensitive payloads.

## Initial Azure Target

- Azure App Service for API and Next.js frontend.
- Separate TEST and PROD resource groups or subscriptions.
- Azure Key Vault for secrets.
- Azure App Configuration for environment-specific non-secret settings.
- Application Insights and Log Analytics for observability.
- Azure Front Door Premium with WAF in front of PROD.
- Deployment slots for zero-downtime PROD releases.

## Observability Baseline

- Structured JSON console logs with correlation id scopes.
- Application Insights is enabled when `ApplicationInsights:ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` is configured.
- `/health/live` reports process liveness.
- `/health/ready` includes database readiness when the application database is configured.
- OpenAPI is exposed only in `Development` and `Test` environments.
- Default request timeout is configured through `RequestTimeouts:DefaultSeconds`.
