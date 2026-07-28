import { NextResponse } from "next/server";
import { getAuthConfig } from "@/lib/auth/config";
import { authCookieOptions, authSessionCookieName, authTransactionCookieName } from "@/lib/auth/cookies";
import { decryptJson, encryptJson } from "@/lib/auth/crypto";
import {
  discoverOidcMetadata,
  exchangeCodeForTokens,
  verifyIdToken,
  type AuthTransaction
} from "@/lib/auth/oidc";
import type { AuthSession } from "@/lib/auth/session";

export const runtime = "nodejs";

export async function GET(request: Request) {
  const configResult = getAuthConfig();

  if (!configResult.ok) {
    return new Response(configResult.error, { status: 500 });
  }

  const url = new URL(request.url);
  const code = url.searchParams.get("code");
  const state = url.searchParams.get("state");
  const providerError = url.searchParams.get("error_description") ?? url.searchParams.get("error");
  const transactionCookie = request.headers
    .get("cookie")
    ?.split(";")
    .map((part) => part.trim())
    .find((part) => part.startsWith(`${authTransactionCookieName}=`))
    ?.slice(authTransactionCookieName.length + 1);
  const transaction = transactionCookie
    ? decryptJson<AuthTransaction>(decodeURIComponent(transactionCookie), configResult.config.cookieSecret)
    : null;

  if (!state || !transaction || transaction.state !== state || Date.now() - transaction.createdAt > 10 * 60 * 1000) {
    return new Response("OIDC callback state is invalid or expired.", { status: 400 });
  }

  if (providerError) {
    const response = new Response(providerError, { status: 400 });
    response.headers.append("Set-Cookie", `${authTransactionCookieName}=; Path=/; Max-Age=0; HttpOnly; SameSite=Lax`);

    return response;
  }

  if (!code) {
    return new Response("OIDC callback is missing an authorization code.", { status: 400 });
  }

  try {
    const metadata = await discoverOidcMetadata(configResult.config.issuer);
    const tokens = await exchangeCodeForTokens(metadata, configResult.config, transaction, code);

    if (!tokens.access_token) {
      return new Response(tokens.error_description ?? tokens.error ?? "OIDC token exchange failed.", { status: 502 });
    }

    const expiresInSeconds = tokens.expires_in ?? 3600;
    const user = await verifyIdToken(tokens.id_token, metadata, configResult.config, transaction);
    const session: AuthSession = {
      accessToken: tokens.access_token,
      idToken: tokens.id_token,
      expiresAt: Date.now() + expiresInSeconds * 1000,
      user
    };
    const response = NextResponse.redirect(new URL(transaction.returnTo, configResult.config.webBaseUrl ?? request.url));

    response.cookies.set(authSessionCookieName, encryptJson(session, configResult.config.cookieSecret), {
      ...authCookieOptions,
      maxAge: expiresInSeconds
    });
    response.cookies.delete(authTransactionCookieName);

    return response;
  } catch {
    return new Response("OIDC callback could not be completed.", { status: 502 });
  }
}
