import Link from "next/link";
import { notFound } from "next/navigation";
import { ActionNotice } from "@/components/ActionNotice";
import { ApiErrorPanel } from "@/components/ApiErrorPanel";
import { checkListingAvailability, getListing } from "@/lib/api";
import { formatMoney } from "@/lib/format";

type ListingPageProps = {
  params: Promise<{ listingId: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function ListingPage({ params, searchParams }: ListingPageProps) {
  const { listingId } = await params;
  const query = await searchParams;
  const startDate = getSingle(query.startDate);
  const endDate = getSingle(query.endDate);
  const status = getSingle(query.status);
  const error = getSingle(query.error);
  const listingResult = await getListing(listingId);

  if (!listingResult.ok && listingResult.error.status === 404) {
    notFound();
  }

  const availabilityResult = startDate && endDate
    ? await checkListingAvailability(listingId, startDate, endDate)
    : undefined;

  return (
    <main className="page detail-page">
      <Link className="text-link" href="/">Back to search</Link>
      <ActionNotice status={status} error={error} />

      {!listingResult.ok ? (
        <ApiErrorPanel context="Listing detail" error={listingResult.error} />
      ) : (
        <>
          <section className="detail-layout">
            <article className="detail-content">
              <div className="detail-copy">
              <p className="eyebrow">{listingResult.data.location}</p>
              <h1>{listingResult.data.title}</h1>
              </div>
              <div className="detail-media" style={listingResult.data.heroImageUrl ? { backgroundImage: `url(${listingResult.data.heroImageUrl})` } : undefined} />
              <p>{listingResult.data.description}</p>
              <p className="detail-summary">{listingResult.data.maxGuests} guests · {listingResult.data.bedroomCount} bedrooms · {listingResult.data.bathroomCount} baths</p>
              {listingResult.data.amenities ? <div><h2>Amenities</h2><p>{listingResult.data.amenities}</p></div> : null}
            </article>

          <aside className="availability-card" aria-labelledby="availability-heading">
            <div>
              <p className="booking-price"><strong>{formatMoney(listingResult.data.nightlyPriceAmount)}</strong> night</p>
              <h2 id="availability-heading">Choose your dates</h2>
            </div>
            <form className="date-form" method="get">
              <label>
                Start date
                <input name="startDate" type="date" defaultValue={startDate ?? ""} required />
              </label>
              <label>
                End date
                <input name="endDate" type="date" defaultValue={endDate ?? ""} required />
              </label>
              <button className="button primary" type="submit">Check dates</button>
            </form>
            {availabilityResult ? (
              <AvailabilityResult
                result={availabilityResult}
                listingId={listingId}
                startDate={startDate}
                endDate={endDate}
                nightlyPriceAmount={listingResult.data.nightlyPriceAmount}
              />
            ) : null}
          </aside>
          </section>
        </>
      )}
    </main>
  );
}

function AvailabilityResult({
  result,
  listingId,
  startDate,
  endDate,
  nightlyPriceAmount
}: {
  result: Awaited<ReturnType<typeof checkListingAvailability>>;
  listingId: string;
  startDate?: string;
  endDate?: string;
  nightlyPriceAmount: number;
}) {
  if (!result.ok) {
    return <ApiErrorPanel context="Availability check" error={result.error} />;
  }

  const nights = calculateNights(startDate ?? result.data.startDate, endDate ?? result.data.endDate);
  const total = nights * nightlyPriceAmount;

  return (
    <div className={result.data.available ? "availability-result available" : "availability-result unavailable"}>
      <div>
        <p className="eyebrow">{result.data.startDate} to {result.data.endDate}</p>
        <h3>{result.data.available ? "Available" : "Unavailable"}</h3>
        <p>
          {result.data.available
            ? `${nights} night${nights === 1 ? "" : "s"} · ${formatMoney(total)} before fees.`
            : "Try another date range."}
        </p>
      </div>
      {result.data.available ? (
        <form action="/api/reservations/create" method="post" className="reservation-request-form">
          <input type="hidden" name="listingId" value={listingId} />
          <input type="hidden" name="startDate" value={startDate ?? result.data.startDate} />
          <input type="hidden" name="endDate" value={endDate ?? result.data.endDate} />
          <input
            type="hidden"
            name="returnTo"
            value={`/listings/${listingId}?startDate=${encodeURIComponent(startDate ?? result.data.startDate)}&endDate=${encodeURIComponent(endDate ?? result.data.endDate)}`}
          />
          <button className="button primary" type="submit">Hold these dates</button>
        </form>
      ) : null}
    </div>
  );
}

function calculateNights(startDate: string, endDate: string): number {
  const start = Date.parse(`${startDate}T00:00:00Z`);
  const end = Date.parse(`${endDate}T00:00:00Z`);
  const nights = Math.round((end - start) / 86_400_000);

  return Number.isFinite(nights) && nights > 0 ? nights : 0;
}

function getSingle(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}
