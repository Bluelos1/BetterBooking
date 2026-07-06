import { createListing } from "@/lib/api";
import { getFormString } from "@/lib/mutation";
import { getSessionOrSignInRedirect, redirectAfterMutation } from "@/lib/server/mutation-routes";

export const runtime = "nodejs";

export async function POST(request: Request) {
  const formData = await request.formData();
  const returnTo = getFormString(formData, "returnTo") ?? "/me/listings";
  const sessionResult = await getSessionOrSignInRedirect(request, returnTo);

  if (!sessionResult.ok) {
    return sessionResult.response;
  }

  const title = getFormString(formData, "title");

  if (!title) {
    return redirectAfterMutation(request, returnTo, { error: "listing-title-required" });
  }

  const description = getFormString(formData, "description");
  const location = getFormString(formData, "location");
  const nightlyPriceAmount = parseNumber(getFormString(formData, "nightlyPriceAmount"));
  const maxGuests = parseInteger(getFormString(formData, "maxGuests"));
  const bedroomCount = parseInteger(getFormString(formData, "bedroomCount"));
  const bathroomCount = parseInteger(getFormString(formData, "bathroomCount"));

  if (!description || !location || !nightlyPriceAmount || !maxGuests || bedroomCount === undefined || !bathroomCount) {
    return redirectAfterMutation(request, returnTo, { error: "listing-details-required" });
  }

  const result = await createListing({
    title,
    description,
    location,
    nightlyPriceAmount,
    maxGuests,
    bedroomCount,
    bathroomCount,
    heroImageUrl: getFormString(formData, "heroImageUrl"),
    amenities: getFormString(formData, "amenities")
  }, sessionResult.session.accessToken);

  if (!result.ok) {
    return redirectAfterMutation(request, returnTo, { error: "listing-create-failed" });
  }

  return redirectAfterMutation(request, "/me/listings", { status: "listing-created" });
}

function parseNumber(value: string | undefined): number | undefined {
  const parsed = Number.parseFloat(value ?? "");

  return Number.isFinite(parsed) ? parsed : undefined;
}

function parseInteger(value: string | undefined): number | undefined {
  const parsed = Number.parseInt(value ?? "", 10);

  return Number.isInteger(parsed) ? parsed : undefined;
}
