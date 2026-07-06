import { NextResponse } from "next/server";
import { getSession } from "@/lib/auth/session";
import { buildMutationRedirectUrl } from "@/lib/mutation";

export async function getSessionOrSignInRedirect(request: Request, returnTo: string) {
  const session = await getSession();

  if (session) {
    return { ok: true as const, session };
  }

  const signInUrl = new URL("/api/auth/sign-in", getRedirectBaseUrl(request));
  signInUrl.searchParams.set("returnTo", returnTo);

  return { ok: false as const, response: NextResponse.redirect(signInUrl, { status: 303 }) };
}

export function redirectAfterMutation(
  request: Request,
  returnTo: string | undefined,
  query: Record<string, string>
) {
  return NextResponse.redirect(buildMutationRedirectUrl(getRedirectBaseUrl(request), returnTo, query), { status: 303 });
}

function getRedirectBaseUrl(request: Request): string {
  return process.env.BETTERBOOKING_WEB_BASE_URL?.trim() || request.url;
}
