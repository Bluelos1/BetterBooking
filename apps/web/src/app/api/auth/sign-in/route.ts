import { NextResponse } from "next/server";
import { getAuthConfig } from "@/lib/auth/config";
import { authCookieOptions, authTransactionCookieName } from "@/lib/auth/cookies";
import { encryptJson } from "@/lib/auth/crypto";
import { buildAuthorizationUrl, createAuthTransaction, discoverOidcMetadata, getRedirectUri } from "@/lib/auth/oidc";

export const runtime = "nodejs";

export async function GET(request: Request) {
  const configResult = getAuthConfig();

  if (!configResult.ok) {
    return new Response(configResult.error, { status: 500 });
  }

  try {
    const url = new URL(request.url);
    const screenHint = getScreenHint(url.searchParams.get("screen"));
    const metadata = await discoverOidcMetadata(configResult.config.issuer);
    const transaction = createAuthTransaction(
      url.searchParams.get("returnTo") ?? "/",
      getRedirectUri(request.url, configResult.config.webBaseUrl)
    );
    const response = NextResponse.redirect(buildAuthorizationUrl(metadata, configResult.config, transaction, {
      screenHint
    }));

    response.cookies.set(authTransactionCookieName, encryptJson(transaction, configResult.config.cookieSecret), {
      ...authCookieOptions,
      maxAge: 10 * 60
    });

    return response;
  } catch {
    return new Response("OIDC sign-in could not be started.", { status: 502 });
  }
}

function getScreenHint(value: string | null): "signup" | undefined {
  return value === "signup" ? "signup" : undefined;
}
