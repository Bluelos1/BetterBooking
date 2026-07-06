import { cancelReservation } from "@/lib/api";
import { getFormString } from "@/lib/mutation";
import { getSessionOrSignInRedirect, redirectAfterMutation } from "@/lib/server/mutation-routes";

export const runtime = "nodejs";

export async function POST(request: Request, context: { params: Promise<{ reservationId: string }> }) {
  const formData = await request.formData();
  const returnTo = getFormString(formData, "returnTo") ?? "/me/reservations";
  const sessionResult = await getSessionOrSignInRedirect(request, returnTo);

  if (!sessionResult.ok) {
    return sessionResult.response;
  }

  const { reservationId } = await context.params;
  const result = await cancelReservation(reservationId, sessionResult.session.accessToken);

  return redirectAfterMutation(request, returnTo, result.ok
    ? { status: "reservation-cancelled" }
    : { error: "reservation-cancel-failed" });
}
