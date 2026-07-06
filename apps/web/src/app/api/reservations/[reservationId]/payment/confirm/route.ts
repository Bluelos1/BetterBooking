import { confirmReservationPayment } from "@/lib/api";
import { getFormString } from "@/lib/mutation";
import { getSessionOrSignInRedirect, redirectAfterMutation } from "@/lib/server/mutation-routes";

export const runtime = "nodejs";

type ConfirmPaymentRouteContext = {
  params: Promise<{ reservationId: string }>;
};

export async function POST(request: Request, context: ConfirmPaymentRouteContext) {
  const { reservationId } = await context.params;
  const formData = await request.formData();
  const returnTo = getFormString(formData, "returnTo") ?? "/me/reservations";
  const sessionResult = await getSessionOrSignInRedirect(request, returnTo);

  if (!sessionResult.ok) {
    return sessionResult.response;
  }

  const result = await confirmReservationPayment(reservationId, sessionResult.session.accessToken);

  if (!result.ok) {
    return redirectAfterMutation(request, returnTo, { error: "payment-confirm-failed" });
  }

  return redirectAfterMutation(request, "/me/reservations", { status: "payment-confirmed" });
}
