# API Conventions

## HTTP

- Use `/api/v1` for versioned application endpoints.
- Use DTOs for request and response bodies.
- Use ProblemDetails for error responses.
- Use pagination for list endpoints.
- Use cancellation tokens in async handlers and services.
- Do not expose internal entity shapes directly.

## Security

- Validate all request input on the backend.
- Enforce authorization on every protected endpoint.
- Use explicit CORS origins only.
- Do not log sensitive request or response payloads.
- Include `X-Correlation-Id` in responses and logs.
- Map authenticated external identities to internal user ids before protected user actions.

## Health

- `/health/live`: process liveness.
- `/health/ready`: dependency readiness. Database, cache, storage, and messaging checks will be added as those dependencies are introduced.
- Database readiness is included when `ConnectionStrings:ApplicationDatabase` is configured.
- OpenAPI is available at `/openapi/v1.json` only in `Development` and `Test`.

## Reservation API Safety

- Reservation creation is protected by authentication and the `authenticated-user` policy.
- The request body must not contain `GuestUserId`; the backend derives the internal user id from authenticated claims.
- The frontend may display apparent availability, but the backend and database must make the final decision.
- Reservation creation must reject listings that are not published.
- Reservation creation must run in a transaction and rely on a database-enforced overlap constraint to prevent double booking.
- Reservation creation is rate limited.
- `GET /api/v1/me/reservations` returns only reservations for the authenticated internal user.
- `POST /api/v1/reservations/{reservationId}/cancel` is allowed only for the reservation guest.
- `POST /api/v1/reservations/{reservationId}/payment/confirm` is a local/demo payment confirmation endpoint. It marks a pending unpaid reservation as confirmed and paid, but does not process real money.
- Cancelled reservations are excluded from availability overlap checks.
- Reservation cancellation is audit logged.

## Listing API Safety

- Public listing reads return published listings only.
- `GET /api/v1/listings` supports `q`, `page`, and `pageSize`; `pageSize` is capped at 50.
- `GET /api/v1/listings/{listingId}` returns `404` for missing, draft, suspended, or archived listings.
- `GET /api/v1/listings/{listingId}/availability` reports apparent availability for a date range, but reservation creation remains the source-of-truth write path.
- Listing creation and owner actions are protected by authentication and the `property-admin` policy.
- The request body must not contain `OwnerUserId`; the backend derives the owner from the authenticated internal user id.
- `GET /api/v1/me/listings` returns only listings owned by the authenticated internal user, including non-public statuses.
- Publishing a listing requires backend owner verification.
- `POST /api/v1/listings/{listingId}/unpublish` requires backend owner verification and moves a published listing back to `Draft`.
- `POST /api/v1/listings/{listingId}/archive` requires backend owner verification and removes the listing from public visibility permanently.
- Archived listings cannot be published or unpublished.
- Listing create and publish actions are audit logged.
- Listing archive and unpublish actions are audit logged.
