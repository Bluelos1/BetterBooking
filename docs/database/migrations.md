# Database Migration Rules

## Rules

- Use EF Core migrations when persistence is introduced.
- Generate SQL scripts for production review.
- Do not run unreviewed destructive migrations automatically in PROD.
- Use transactions where supported.
- Prefer backward-compatible schema changes.
- Separate destructive contract steps from the code release that stops using old schema.

## Reservation Consistency

- The database is the source of truth for availability.
- Redis may cache availability/search data but must not decide final availability.
- Reservation creation must use a transaction.
- Double booking must be prevented with database constraints or equivalent database-enforced logic.
- Payment confirmation must be idempotent and must not confirm invalid reservation state.

## Initial PostgreSQL Strategy

- Use `btree_gist` and an exclusion constraint to prevent overlapping active reservations per listing.
- Treat `Pending` and `Confirmed` reservations as active for overlap checks.
- Use half-open date ranges: check-in date is inclusive, check-out date is exclusive.
- Keep `Cancelled` and `Expired` reservations outside the overlap constraint.
- Keep the reviewed SQL shape in `infra/database/postgresql/001_initial_booking_schema.sql` aligned with EF Core migrations.
- Store external identity mappings in `users` with a unique `(external_provider, external_subject)` constraint.
- Store audit events in `audit_events` without sensitive payloads.
- Enforce listing ownership and reservation guest relationships with database foreign keys.

## Local Migration Commands

- Restore EF tooling: `dotnet tool restore`.
- Add migration: `dotnet tool run dotnet-ef migrations add <Name> --project apps/backend/src/App.Infrastructure/App.Infrastructure.csproj --startup-project apps/backend/src/App.Infrastructure/App.Infrastructure.csproj --context ApplicationDbContext --output-dir Persistence/Migrations`.
- Generate review script: `dotnet tool run dotnet-ef migrations script --idempotent --project apps/backend/src/App.Infrastructure/App.Infrastructure.csproj --startup-project apps/backend/src/App.Infrastructure/App.Infrastructure.csproj --context ApplicationDbContext --output app-db-idempotent.sql`.
- Do not apply generated scripts to PROD until reviewed.

## PostgreSQL Integration Tests

- PostgreSQL-backed tests use Testcontainers and run automatically in CI.
- To run them locally, start Docker and run tests with `BETTERBOOKING_RUN_POSTGRES_TESTS=true`.
- Without that variable, local test runs skip container-backed tests to avoid failing when Docker Desktop is stopped.
