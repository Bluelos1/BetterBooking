import Link from "next/link";
import { ActionNotice } from "@/components/ActionNotice";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { EmptyState } from "@/components/EmptyState";
import { PaginationSummary } from "@/components/PaginationSummary";
import { SignInRequired } from "@/components/SignInRequired";
import { StatusPill } from "@/components/StatusPill";
import { getMyReservations } from "@/lib/api";
import { getSession } from "@/lib/auth/session";
import { formatDateOnly, formatDateTime } from "@/lib/format";

type MyReservationsPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function MyReservationsPage({ searchParams }: MyReservationsPageProps) {
  const params = await searchParams;
  const page = parsePositiveInt(getSingle(params.page), 1);
  const status = getSingle(params.status);
  const error = getSingle(params.error);
  const session = await getSession();

  if (!session) {
    return (
      <main className="page">
        <PageHeading />
        <SignInRequired returnTo="/me/reservations" title="Sign in to see your trips" />
      </main>
    );
  }

  const result = await getMyReservations({ page, pageSize: 20 }, session.accessToken);
  const currentReturnTo = page > 1 ? `/me/reservations?page=${page}` : "/me/reservations";

  return (
    <main className="page">
      <PageHeading />
      <ActionNotice status={status} error={error} />

      {!result.ok && result.error.status === 401 ? (
        <SignInRequired returnTo="/me/reservations" />
      ) : !result.ok ? (
        <ApiErrorPanel context="My reservations" error={result.error} />
      ) : result.data.totalCount === 0 ? (
        <EmptyState title="No trips yet" body="Find a stay and choose your dates to get started." actionHref="/" actionLabel="Explore stays" />
      ) : (
        <section className="table-card" aria-label="My reservations">
          <PaginationSummary
            page={result.data.page}
            pageSize={result.data.pageSize}
            totalCount={result.data.totalCount}
            hasNextPage={result.data.hasNextPage}
            basePath="/me/reservations"
          />
          <div className="trip-list">
            {result.data.items.length === 0 ? (
              <EmptyState
                title="This page is empty"
                body="Return to the previous page to continue reviewing your trips."
                actionHref={`/me/reservations?page=${Math.max(1, page - 1)}`}
                actionLabel="Previous page"
              />
            ) : null}
            {result.data.items.map((reservation) => (
              <article className="trip-card" key={reservation.id}>
                <div className="trip-main">
                  <Link className="text-link" href={`/listings/${reservation.listingId}`}>{reservation.listingTitle}</Link>
                  <strong>{formatDateOnly(reservation.startDate)} - {formatDateOnly(reservation.endDate)}</strong>
                  <span className="muted">Updated {formatDateTime(reservation.updatedAt)}</span>
                </div>
                <div className="trip-statuses" aria-label="Reservation status">
                  <StatusPill status={reservation.status} />
                  <StatusPill status={reservation.paymentStatus} />
                </div>
                <div className="action-row">
                  {reservation.status === "Pending" && reservation.paymentStatus === "Unpaid" ? (
                    <form action={`/api/reservations/${reservation.id}/payment/confirm`} method="post">
                      <input type="hidden" name="returnTo" value={currentReturnTo} />
                      <button className="button compact primary" type="submit">Complete demo payment</button>
                    </form>
                  ) : null}
                  {reservation.status === "Pending" || reservation.status === "Confirmed" ? (
                    <form action={`/api/reservations/${reservation.id}/cancel`} method="post">
                      <input type="hidden" name="returnTo" value={currentReturnTo} />
                      <button className="button compact danger" type="submit">Cancel</button>
                    </form>
                  ) : null}
                  {reservation.status !== "Pending" && reservation.status !== "Confirmed" ? <span className="muted">No actions</span> : null}
                </div>
              </article>
            ))}
          </div>
        </section>
      )}
    </main>
  );
}

function PageHeading() {
  return (
    <section className="page-heading">
      <h1>Trips</h1>
      <p>Review upcoming stays, complete payment, or cancel a reservation.</p>
    </section>
  );
}

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function parsePositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? "", 10);

  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}
