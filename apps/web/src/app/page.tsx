import Link from "next/link";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { EmptyState } from "@/components/EmptyState";
import { PaginationSummary } from "@/components/PaginationSummary";
import { formatDate } from "@/lib/format";
import { searchListings } from "@/lib/api";

type HomePageProps = {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function HomePage({ searchParams }: HomePageProps) {
  const params = await searchParams;
  const q = getSingle(params.q);
  const page = parsePositiveInt(getSingle(params.page), 1);
  const result = await searchListings({ q, page, pageSize: 12 });

  return (
    <main className="page">
      <section className="hero">
        <div>
          <p className="eyebrow">Booking marketplace</p>
          <h1>Find apartments and hotels with real availability checks.</h1>
          <p>
            Search published stays, compare practical details, check exact dates, and reserve with a
            payment-ready flow backed by PostgreSQL overlap protection.
          </p>
        </div>
        <form className="search-card" action="/" method="get">
          <label htmlFor="q">Where do you want to stay?</label>
          <div className="search-row">
            <input id="q" name="q" defaultValue={q ?? ""} placeholder="Krakow, apartment, workspace..." />
            <button className="button primary" type="submit">Search</button>
          </div>
        </form>
      </section>

      {!result.ok ? (
        <ApiErrorPanel context="Listing search" error={result.error} />
      ) : result.data.items.length === 0 ? (
        <EmptyState title="No published listings found" body="Try a different search term or publish a listing from the owner workspace." />
      ) : (
        <section className="results-section" aria-label="Published listings">
          <PaginationSummary
            page={result.data.page}
            pageSize={result.data.pageSize}
            totalCount={result.data.totalCount}
            hasNextPage={result.data.hasNextPage}
          />
          <div className="listing-grid">
            {result.data.items.map((listing) => (
              <article className="listing-card" key={listing.id}>
                <div className="listing-card-media" style={listing.heroImageUrl ? { backgroundImage: `url(${listing.heroImageUrl})` } : undefined} />
                <div className="listing-card-body">
                  <p className="eyebrow">{listing.location}</p>
                  <h2>{listing.title}</h2>
                  <p>{listing.description}</p>
                  <div className="listing-facts">
                    <span>{formatMoney(listing.nightlyPriceAmount)} / night</span>
                    <span>{listing.maxGuests} guests</span>
                    <span>{listing.bedroomCount} bedrooms</span>
                  </div>
                  <div className="card-footer-row">
                    <span className="muted">Published {formatDate(listing.createdAt)}</span>
                    <Link className="text-link" href={`/listings/${listing.id}`}>View stay</Link>
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
