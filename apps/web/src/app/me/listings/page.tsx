import Link from "next/link";
import { ActionNotice } from "@/components/ActionNotice";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { EmptyState } from "@/components/EmptyState";
import { PaginationSummary } from "@/components/PaginationSummary";
import { SignInRequired } from "@/components/SignInRequired";
import { StatusPill } from "@/components/StatusPill";
import { getMyListings } from "@/lib/api";
import { getSession } from "@/lib/auth/session";
import { formatDateTime, formatMoney } from "@/lib/format";

type MyListingsPageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function MyListingsPage({ searchParams }: MyListingsPageProps) {
  const params = await searchParams;
  const page = parsePositiveInt(getSingle(params.page), 1);
  const status = getSingle(params.status);
  const error = getSingle(params.error);
  const session = await getSession();

  if (!session) {
    return (
      <main className="page">
        <PageHeading />
        <SignInRequired returnTo="/me/listings" title="Sign in to manage your listings" />
      </main>
    );
  }

  const result = await getMyListings({ page, pageSize: 20 }, session.accessToken);
  const openCreateForm = Boolean(error) || (result.ok && result.data.totalCount === 0);
  const currentReturnTo = page > 1 ? `/me/listings?page=${page}` : "/me/listings";

  return (
    <main className="page">
      <PageHeading />
      <ActionNotice status={status} error={error} />

      <details className="form-card listing-form-card" open={openCreateForm}>
        <summary id="create-listing-heading">Add a listing</summary>
        <form className="listing-create-form" action="/api/listings/create" method="post">
          <input type="hidden" name="returnTo" value="/me/listings" />
          <label>
            Listing title
            <input name="title" placeholder="Old Town serviced apartment" maxLength={200} required />
          </label>
          <label>
            Location
            <input name="location" placeholder="Krakow, Old Town" maxLength={160} required />
          </label>
          <label className="wide-field">
            Description
            <textarea
              name="description"
              placeholder="Describe the rooms, neighborhood, check-in experience, and what makes this stay useful."
              maxLength={2000}
              required
            />
          </label>
          <label>
            Price per night (USD)
            <input name="nightlyPriceAmount" type="number" min="1" max="100000" step="0.01" placeholder="180" required />
          </label>
          <label>
            Max guests
            <input name="maxGuests" type="number" min="1" max="50" defaultValue="2" required />
          </label>
          <label>
            Bedrooms
            <input name="bedroomCount" type="number" min="0" max="50" defaultValue="1" required />
          </label>
          <label>
            Bathrooms
            <input name="bathroomCount" type="number" min="1" max="50" defaultValue="1" required />
          </label>
          <label className="wide-field">
            Hero image URL (optional)
            <input name="heroImageUrl" type="url" placeholder="https://images.example/stay.jpg" maxLength={2048} />
          </label>
          <label className="wide-field">
            Amenities
            <input name="amenities" placeholder="Wi-Fi, kitchen, workspace, self check-in" maxLength={500} />
          </label>
          <button className="button primary wide-field" type="submit">Save draft</button>
        </form>
      </details>

      {!result.ok && result.error.status === 401 ? (
        <SignInRequired returnTo="/me/listings" />
      ) : !result.ok ? (
        <ApiErrorPanel context="My listings" error={result.error} />
      ) : result.data.totalCount === 0 ? (
        <EmptyState title="No listings yet" body="Add your first apartment or hotel, then publish it when the details are ready." />
      ) : (
        <section className="host-listings-section" aria-label="My listings">
          <PaginationSummary
            page={result.data.page}
            pageSize={result.data.pageSize}
            totalCount={result.data.totalCount}
            hasNextPage={result.data.hasNextPage}
            basePath="/me/listings"
          />
          <div className="host-listing-grid">
            {result.data.items.length === 0 ? (
              <EmptyState
                title="This page is empty"
                body="Return to the previous page to continue managing your listings."
                actionHref={`/me/listings?page=${Math.max(1, page - 1)}`}
                actionLabel="Previous page"
              />
            ) : null}
            {result.data.items.map((listing) => (
              <article className="host-listing-card" key={listing.id}>
                <div className="listing-thumb" style={listing.heroImageUrl ? { backgroundImage: `url(${listing.heroImageUrl})` } : undefined} />
                <div className="host-listing-body">
                  <div className="card-title-row">
                    <div>
                      <p className="eyebrow">{listing.location}</p>
                      <h2>{listing.title}</h2>
                    </div>
                    <StatusPill status={listing.status} />
                  </div>
                  <p className="listing-description">{listing.description}</p>
                  <div className="listing-facts">
                    <strong>{formatMoney(listing.nightlyPriceAmount)} night</strong>
                    <span>{listing.maxGuests} guests · {listing.bedroomCount} bedrooms · {listing.bathroomCount} baths</span>
                  </div>
                  {listing.amenities ? <p className="muted">{listing.amenities}</p> : null}
                  <div className="card-footer-row">
                    <span className="muted">Created {formatDateTime(listing.createdAt)}</span>
                    {listing.status === "Published"
                      ? <Link className="text-link" href={`/listings/${listing.id}`}>Open public page</Link>
                      : null}
                  </div>
                  <div className="action-row">
                    {listing.status === "Draft" ? (
                      <LifecycleForm action={`/api/listings/${listing.id}/publish`} label="Publish" returnTo={currentReturnTo} />
                    ) : null}
                    {listing.status === "Published" ? (
                      <LifecycleForm action={`/api/listings/${listing.id}/unpublish`} label="Unpublish" returnTo={currentReturnTo} />
                    ) : null}
                    {listing.status !== "Archived" ? (
                      <LifecycleForm action={`/api/listings/${listing.id}/archive`} label="Archive" returnTo={currentReturnTo} danger />
                    ) : null}
                    {listing.status === "Archived" ? <span className="muted">No actions</span> : null}
                  </div>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}
    </main>
  );
}

function LifecycleForm({ action, label, returnTo, danger = false }: { action: string; label: string; returnTo: string; danger?: boolean }) {
  return (
    <form action={action} method="post">
      <input type="hidden" name="returnTo" value={returnTo} />
      <button className={danger ? "button compact danger" : "button compact secondary"} type="submit">
        {label}
      </button>
    </form>
  );
}

function PageHeading() {
  return (
    <section className="page-heading">
      <h1>Your listings</h1>
      <p>Create, publish, and manage your apartments or hotels.</p>
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
