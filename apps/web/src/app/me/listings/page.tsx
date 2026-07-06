import Link from "next/link";
import { ActionNotice } from "@/components/ActionNotice";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { EmptyState } from "@/components/EmptyState";
import { PaginationSummary } from "@/components/PaginationSummary";
import { SignInRequired } from "@/components/SignInRequired";
import { StatusPill } from "@/components/StatusPill";
import { getMyListings } from "@/lib/api";
import { getSession } from "@/lib/auth/session";
import { formatDateTime } from "@/lib/format";

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
        <SignInRequired returnTo="/me/listings" />
      </main>
    );
  }

  const isAdmin = session.user.roles.includes("admin") || session.user.roles.includes("host");

  if (!isAdmin) {
    return (
      <main className="page">
        <PageHeading />
        <section className="notice auth" role="status">
          <p className="eyebrow">Admin workspace</p>
          <h2>Switch to property admin to manage listings.</h2>
          <p>Traveler accounts can book stays and manage trips. Property admins create hotels and apartments.</p>
          <Link className="button primary" href="/sign-in?returnTo=/me/listings">Sign in as property admin</Link>
        </section>
      </main>
    );
  }

  const result = await getMyListings({ page, pageSize: 20 }, session.accessToken);

  return (
    <main className="page">
      <PageHeading />
      <ActionNotice status={status} error={error} />

      <section className="form-card listing-form-card" aria-labelledby="create-listing-heading">
        <div>
          <p className="eyebrow">New hotel or apartment</p>
          <h2 id="create-listing-heading">Create a complete listing</h2>
          <p>Add enough detail for guests to understand the stay before it goes public.</p>
        </div>
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
            Price per night
            <input name="nightlyPriceAmount" type="number" min="1" step="0.01" placeholder="180" required />
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
            Hero image URL optional
            <input name="heroImageUrl" type="url" placeholder="https://images.example/stay.jpg" maxLength={2048} />
          </label>
          <label className="wide-field">
            Amenities
            <input name="amenities" placeholder="Wi-Fi, kitchen, workspace, self check-in" maxLength={500} />
          </label>
          <button className="button primary wide-field" type="submit">Create detailed draft</button>
        </form>
      </section>

      {!result.ok && result.error.status === 401 ? (
        <SignInRequired returnTo="/me/listings" />
      ) : !result.ok ? (
        <ApiErrorPanel context="My listings" error={result.error} />
      ) : result.data.items.length === 0 ? (
        <EmptyState title="No host listings yet" body="Create a detailed draft above, review it, then publish when it is ready for guests." />
      ) : (
        <section className="host-listings-section" aria-label="My listings">
          <PaginationSummary
            page={result.data.page}
            pageSize={result.data.pageSize}
            totalCount={result.data.totalCount}
            hasNextPage={result.data.hasNextPage}
          />
          <div className="host-listing-grid">
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
                  <p>{listing.description}</p>
                  <div className="listing-facts">
                    <span>{formatMoney(listing.nightlyPriceAmount)} / night</span>
                    <span>{listing.maxGuests} guests</span>
                    <span>{listing.bedroomCount} bedrooms</span>
                    <span>{listing.bathroomCount} baths</span>
                  </div>
                  {listing.amenities ? <p className="muted">{listing.amenities}</p> : null}
                  <div className="card-footer-row">
                    <span className="muted">Created {formatDateTime(listing.createdAt)}</span>
                    {listing.status === "Published"
                      ? <Link className="text-link" href={`/listings/${listing.id}`}>Open public page</Link>
                      : <span className="muted">Hidden from guests</span>}
                  </div>
                  <div className="action-row">
                    {listing.status === "Draft" ? (
                      <LifecycleForm action={`/api/listings/${listing.id}/publish`} label="Publish" />
                    ) : null}
                    {listing.status === "Published" ? (
                      <LifecycleForm action={`/api/listings/${listing.id}/unpublish`} label="Unpublish" />
                    ) : null}
                    {listing.status !== "Archived" ? (
                      <LifecycleForm action={`/api/listings/${listing.id}/archive`} label="Archive" danger />
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

function LifecycleForm({ action, label, danger = false }: { action: string; label: string; danger?: boolean }) {
  return (
    <form action={action} method="post">
      <input type="hidden" name="returnTo" value="/me/listings" />
      <button className={danger ? "button compact danger" : "button compact secondary"} type="submit">
        {label}
      </button>
    </form>
  );
}

function PageHeading() {
  return (
    <section className="page-heading">
      <p className="eyebrow">Owner workspace</p>
      <h1>Host dashboard</h1>
      <p>Create hotel and apartment listings, publish them when ready, and keep guest-facing details consistent.</p>
    </section>
  );
}

function formatMoney(value: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0
  }).format(value);
}

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function parsePositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? "", 10);

  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}
