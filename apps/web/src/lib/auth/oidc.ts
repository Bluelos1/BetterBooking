import { createHash, randomBytes } from "node:crypto";
import type { AuthConfig } from "./config";

export type OidcMetadata = {
  authorization_endpoint: string;
  token_endpoint: string;
};

export type AuthTransaction = {
  state: string;
  codeVerifier: string;
  returnTo: string;
  redirectUri: string;
  createdAt: number;
};

export type TokenResponse = {
  access_token?: string;
  id_token?: string;
  token_type?: string;
  expires_in?: number;
  error?: string;
  error_description?: string;
};

export type SessionUser = {
  name?: string;
  email?: string;
  roles: string[];
};

export type AuthorizationUrlOptions = {
  loginHint?: string;
  screenHint?: "signup";
};

export function createAuthTransaction(returnTo: string, redirectUri: string): AuthTransaction {
  return {
    state: randomBase64Url(32),
    codeVerifier: randomBase64Url(64),
    returnTo: sanitizeReturnTo(returnTo),
    redirectUri,
    createdAt: Date.now()
  };
}

export function buildAuthorizationUrl(
  metadata: OidcMetadata,
  config: AuthConfig,
  transaction: AuthTransaction,
  options: AuthorizationUrlOptions = {}
): string {
  const url = new URL(metadata.authorization_endpoint);
  url.searchParams.set("client_id", config.clientId);
  url.searchParams.set("response_type", "code");
  url.searchParams.set("redirect_uri", transaction.redirectUri);
  url.searchParams.set("scope", config.scopes);
  url.searchParams.set("state", transaction.state);
  url.searchParams.set("code_challenge", createCodeChallenge(transaction.codeVerifier));
  url.searchParams.set("code_challenge_method", "S256");

  if (options.loginHint) {
    url.searchParams.set("login_hint", options.loginHint);
  }

  if (options.screenHint) {
    url.searchParams.set("screen_hint", options.screenHint);
  }

  return url.toString();
}

export async function discoverOidcMetadata(issuer: string): Promise<OidcMetadata> {
  const response = await fetch(`${issuer}/.well-known/openid-configuration`, {
    cache: "no-store",
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw new Error("OIDC metadata discovery failed.");
  }

  const metadata = (await response.json()) as Partial<OidcMetadata>;

  if (!metadata.authorization_endpoint || !metadata.token_endpoint) {
    throw new Error("OIDC metadata is missing required endpoints.");
  }

  return {
    authorization_endpoint: metadata.authorization_endpoint,
    token_endpoint: metadata.token_endpoint
  };
}

export async function exchangeCodeForTokens(
  metadata: OidcMetadata,
  config: AuthConfig,
  transaction: AuthTransaction,
  code: string
): Promise<TokenResponse> {
  const body = new URLSearchParams({
    grant_type: "authorization_code",
    client_id: config.clientId,
    code,
    redirect_uri: transaction.redirectUri,
    code_verifier: transaction.codeVerifier
  });

  if (config.clientSecret) {
    body.set("client_secret", config.clientSecret);
  }

  const response = await fetch(metadata.token_endpoint, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/x-www-form-urlencoded"
    },
    body
  });

  return (await response.json()) as TokenResponse;
}

export function parseIdTokenUser(idToken: string | undefined): SessionUser {
  if (!idToken) {
    return { roles: [] };
  }

  const [, payload] = idToken.split(".");

  if (!payload) {
    return { roles: [] };
  }

  try {
    const claims = JSON.parse(Buffer.from(payload, "base64url").toString("utf8")) as Record<string, unknown>;
    const name = typeof claims.name === "string" ? claims.name : undefined;
    const email = typeof claims.email === "string" ? claims.email : undefined;
    const roles = extractRoles(claims);

    return { name, email, roles };
  } catch {
    return { roles: [] };
  }
}

function extractRoles(claims: Record<string, unknown>): string[] {
  const role = claims.role;
  const roles = claims.roles;

  if (Array.isArray(roles)) {
    return roles.filter((value): value is string => typeof value === "string");
  }

  if (typeof role === "string") {
    return [role];
  }

  if (Array.isArray(role)) {
    return role.filter((value): value is string => typeof value === "string");
  }

  return [];
}

export function sanitizeReturnTo(value: string | undefined): string {
  if (!value || !value.startsWith("/") || value.startsWith("//")) {
    return "/";
  }

  return value;
}

export function getRedirectUri(requestUrl: string, configuredWebBaseUrl?: string): string {
  const origin = configuredWebBaseUrl ?? new URL(requestUrl).origin;

  return `${origin}/api/auth/callback`;
}

function createCodeChallenge(codeVerifier: string): string {
  return createHash("sha256").update(codeVerifier).digest("base64url");
}

function randomBase64Url(size: number): string {
  return randomBytes(size).toString("base64url");
}
