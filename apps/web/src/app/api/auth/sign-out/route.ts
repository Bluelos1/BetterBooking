import { NextResponse } from "next/server";
import { getAuthConfig } from "@/lib/auth/config";
import { authSessionCookieName, authTransactionCookieName } from "@/lib/auth/cookies";
import { buildLogoutUrl, discoverOidcMetadata } from "@/lib/auth/oidc";
import { getSession } from "@/lib/auth/session";

export const runtime = "nodejs";

export async function GET() {
  return new Response(null, { status: 405, headers: { Allow: "POST" } });
}

export async function POST(request: Request) {
  const expectedOrigin = new URL(process.env.BETTERBOOKING_WEB_BASE_URL?.trim() || request.url).origin;

  if (request.headers.get("origin") !== expectedOrigin) {
    return new Response("Invalid sign-out origin.", { status: 403 });
  }

  return signOut(request);
}

async function signOut(request: Request) {
  const configResult = getAuthConfig();
  const returnUrl = new URL("/", process.env.BETTERBOOKING_WEB_BASE_URL?.trim() || request.url).toString();
  const session = await getSession();
  let destination = returnUrl;

  if (configResult.ok) {
    try {
      const metadata = await discoverOidcMetadata(configResult.config.issuer);
      destination = buildLogoutUrl(metadata, configResult.config, returnUrl, session?.idToken) ?? returnUrl;
    } catch {
      destination = returnUrl;
    }
  }

  const response = NextResponse.redirect(destination, { status: 303 });
  response.cookies.delete(authSessionCookieName);
  response.cookies.delete(authTransactionCookieName);

  return response;
}
