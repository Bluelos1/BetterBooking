import Link from "next/link";
import { ActionNotice } from "@/components/ActionNotice";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { EmptyState } from "@/components/EmptyState";
import { PaginationSummary } from "@/components/PaginationSummary";
import { SignInRequired } from "@/components/SignInRequired";
import { StatusPill } from "@/components/StatusPill";
import { getMyReservations } from "@/lib/api";
import { getSession } from "@/lib/auth/session";
import { formatDate, formatDateTime } from "@/lib/format";

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
        <SignInRequired returnTo="/me/reservations" />
      </main>
    );
  }

  const result = await getMyReservations({ page, pageSize: 20 }, session.accessToken);

  return (
    <main className="page">
      <PageHeading />
      <ActionNotice status={status} error={error} />

      {!result.ok && result.error.status === 401 ? (
        <SignInRequired returnTo="/me/reservations" />
      ) : !result.ok ? (
        <ApiErrorPanel context="My reservations" error={result.error} />
      ) : result.data.items.length === 0 ? (
        <EmptyState title="No trips yet" body="Find a published stay, hold dates, then complete the demo payment to confirm your reservation." />
      ) : (
        <section className="table-card" aria-label="My reservations table">
          <PaginationSummary
            page={result.data.page}
            pageSize={result.data.pageSize}
            totalCount={result.data.totalCount}
            hasNextPage={result.data.hasNextPage}
          />
          <div className="responsive-table">
            <table>
              <thead>
                <tr>
                  <th>Listing</th>
                  <th>Dates</th>
                  <th>Status</th>
                  <th>Payment</th>
                  <th>Updated</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {result.data.items.map((reservation) => (
                  <tr key={reservation.id}>
                    <td>
                      <Link className="text-link" href={`/listings/${reservation.listingId}`}>
                        {reservation.listingTitle}
                      </Link>
                    </td>
                    <td>{formatDate(reservation.startDate)} - {formatDate(reservation.endDate)}</td>
                    <td><StatusPill status={reservation.status} /></td>
                    <td><StatusPill status={reservation.paymentStatus} /></td>
                    <td>{formatDateTime(reservation.updatedAt)}</td>
                    <td>
                      <div className="action-row">
                        {reservation.status === "Pending" && reservation.paymentStatus === "Unpaid" ? (
                          <form action={`/api/reservations/${reservation.id}/payment/confirm`} method="post">
                            <input type="hidden" name="returnTo" value="/me/reservations" />
                            <button className="button compact primary" type="submit">Pay demo</button>
                          </form>
                        ) : null}
                        {reservation.status === "Pending" || reservation.status === "Confirmed" ? (
                          <form action={`/api/reservations/${reservation.id}/cancel`} method="post">
                            <input type="hidden" name="returnTo" value="/me/reservations" />
                            <button className="button compact danger" type="submit">Cancel</button>
                          </form>
                        ) : null}
                        {reservation.status !== "Pending" && reservation.status !== "Confirmed" ? <span className="muted">No actions</span> : null}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </main>
  );
}

function PageHeading() {
  return (
    <section className="page-heading">
      <p className="eyebrow">Guest workspace</p>
      <h1>Trips</h1>
      <p>Review held and confirmed stays, complete demo payments, or cancel active reservations.</p>
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
