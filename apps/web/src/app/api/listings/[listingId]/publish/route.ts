import { publishListing } from "@/lib/api";
import { getFormString } from "@/lib/mutation";
import { getSessionOrSignInRedirect, redirectAfterMutation } from "@/lib/server/mutation-routes";

export const runtime = "nodejs";

export async function POST(request: Request, context: { params: Promise<{ listingId: string }> }) {
  const formData = await request.formData();
  const returnTo = getFormString(formData, "returnTo") ?? "/me/listings";
  const sessionResult = await getSessionOrSignInRedirect(request, returnTo);

  if (!sessionResult.ok) {
    return sessionResult.response;
  }

  const { listingId } = await context.params;
  const result = await publishListing(listingId, sessionResult.session.accessToken);

  return redirectAfterMutation(request, returnTo, result.ok
    ? { status: "listing-published" }
    : { error: "listing-publish-failed" });
}
