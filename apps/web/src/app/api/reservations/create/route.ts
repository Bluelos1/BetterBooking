import { createReservation } from "@/lib/api";
import { getFormString } from "@/lib/mutation";
import { getSessionOrSignInRedirect, redirectAfterMutation } from "@/lib/server/mutation-routes";

export const runtime = "nodejs";

export async function POST(request: Request) {
  const formData = await request.formData();
  const listingId = getFormString(formData, "listingId");
  const startDate = getFormString(formData, "startDate");
  const endDate = getFormString(formData, "endDate");
  const returnTo = getFormString(formData, "returnTo") ?? (listingId ? `/listings/${listingId}` : "/");
  const sessionResult = await getSessionOrSignInRedirect(request, returnTo);

  if (!sessionResult.ok) {
    return sessionResult.response;
  }

  if (!listingId || !startDate || !endDate) {
    return redirectAfterMutation(request, returnTo, { error: "reservation-dates-required" });
  }

  const result = await createReservation({ listingId, startDate, endDate }, sessionResult.session.accessToken);

  if (!result.ok) {
    return redirectAfterMutation(request, returnTo, { error: "reservation-create-failed" });
  }

  return redirectAfterMutation(request, "/me/reservations", { status: "reservation-created" });
}
