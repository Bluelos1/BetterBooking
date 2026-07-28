import Link from "next/link";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { EmptyState } from "@/components/EmptyState";
import { PaginationSummary } from "@/components/PaginationSummary";
import { formatMoney } from "@/lib/format";
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
          <p className="eyebrow">Apartments and hotels</p>
          <h1>Find your next stay.</h1>
          <p>Search properties, check your dates, and book in a few simple steps.</p>
        </div>
        <form className="search-card" action="/" method="get">
          <label htmlFor="q">Search by destination or property</label>
          <div className="search-row">
            <input id="q" name="q" defaultValue={q ?? ""} placeholder="City, neighborhood, or property" maxLength={100} />
            <button className="button primary" type="submit">Search</button>
          </div>
        </form>
      </section>

      {!result.ok ? (
        <ApiErrorPanel context="Listing search" error={result.error} />
      ) : result.data.totalCount === 0 ? (
        <EmptyState
          title={q ? `No stays match “${q}”` : "No stays are available yet"}
          body={q ? "Try another destination or a broader search." : "Check back soon for new places to stay."}
          actionHref={q ? "/" : undefined}
          actionLabel={q ? "Clear search" : undefined}
        />
      ) : (
        <section className="results-section" aria-label="Published listings">
          <div className="section-heading">
            <h2>{q ? `Stays matching “${q}”` : "Places to stay"}</h2>
          </div>
          <PaginationSummary
            page={result.data.page}
            pageSize={result.data.pageSize}
            totalCount={result.data.totalCount}
            hasNextPage={result.data.hasNextPage}
            basePath="/"
            query={{ q }}
          />
          <div className="listing-grid">
            {result.data.items.length === 0 ? (
              <EmptyState
                title="This page is empty"
                body="Return to the previous page to continue browsing."
                actionHref={buildSearchPageHref(Math.max(1, page - 1), q)}
                actionLabel="Previous page"
              />
            ) : null}
            {result.data.items.map((listing) => (
              <article className="listing-card" key={listing.id}>
                <div className="listing-card-media" style={listing.heroImageUrl ? { backgroundImage: `url(${listing.heroImageUrl})` } : undefined} />
                <div className="listing-card-body">
                  <p className="listing-location">{listing.location}</p>
                  <h2><Link href={`/listings/${listing.id}`}>{listing.title}</Link></h2>
                  <p className="listing-description">{listing.description}</p>
                  <div className="listing-facts">
                    <strong>{formatMoney(listing.nightlyPriceAmount)} night</strong>
                    <span>{listing.maxGuests} guests · {listing.bedroomCount} bedrooms</span>
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

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function parsePositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? "", 10);

  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

function buildSearchPageHref(page: number, q?: string): string {
  const params = new URLSearchParams();
  if (q) params.set("q", q);
  if (page > 1) params.set("page", String(page));

  const search = params.toString();
  return search ? `/?${search}` : "/";
}
