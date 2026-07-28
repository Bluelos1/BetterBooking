import { createHash, randomBytes } from "node:crypto";
import { createRemoteJWKSet, jwtVerify, type JWTPayload } from "jose";
import type { AuthConfig } from "./config";

export type OidcMetadata = {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
  jwks_uri: string;
  end_session_endpoint?: string;
};

export type AuthTransaction = {
  state: string;
  nonce: string;
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
  screenHint?: "signup";
};

export function createAuthTransaction(returnTo: string, redirectUri: string): AuthTransaction {
  return {
    state: randomBase64Url(32),
    nonce: randomBase64Url(32),
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
  url.searchParams.set("audience", config.audience);
  url.searchParams.set("state", transaction.state);
  url.searchParams.set("nonce", transaction.nonce);
  url.searchParams.set("code_challenge", createCodeChallenge(transaction.codeVerifier));
  url.searchParams.set("code_challenge_method", "S256");

  if (options.screenHint) {
    url.searchParams.set("screen_hint", options.screenHint);
  }

  return url.toString();
}

export async function discoverOidcMetadata(issuer: string): Promise<OidcMetadata> {
  const response = await fetch(`${issuer}/.well-known/openid-configuration`, {
    cache: "no-store",
    signal: AbortSignal.timeout(5_000),
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw new Error("OIDC metadata discovery failed.");
  }

  const metadata = (await response.json()) as Partial<OidcMetadata>;

  if (!metadata.issuer || !metadata.authorization_endpoint || !metadata.token_endpoint || !metadata.jwks_uri) {
    throw new Error("OIDC metadata is missing required endpoints.");
  }

  return {
    issuer: metadata.issuer,
    authorization_endpoint: metadata.authorization_endpoint,
    token_endpoint: metadata.token_endpoint,
    jwks_uri: metadata.jwks_uri,
    end_session_endpoint: metadata.end_session_endpoint
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

export async function verifyIdToken(
  idToken: string | undefined,
  metadata: OidcMetadata,
  config: AuthConfig,
  transaction: AuthTransaction
): Promise<SessionUser> {
  if (!idToken) {
    throw new Error("OIDC token response is missing an ID token.");
  }

  const jwks = createRemoteJWKSet(new URL(metadata.jwks_uri), { timeoutDuration: 5_000 });
  const { payload } = await jwtVerify(idToken, jwks, {
    algorithms: ["RS256"],
    issuer: metadata.issuer,
    audience: config.clientId,
    maxTokenAge: "10m"
  });

  if (typeof payload.sub !== "string" || typeof payload.iat !== "number" || typeof payload.exp !== "number") {
    throw new Error("OIDC ID token is missing required claims.");
  }

  if (Array.isArray(payload.aud) && payload.aud.length > 1 && payload.azp !== config.clientId) {
    throw new Error("OIDC ID token authorized party is invalid.");
  }

  if (payload.nonce !== transaction.nonce) {
    throw new Error("OIDC ID token nonce is invalid.");
  }

  return toSessionUser(payload);
}

export function buildLogoutUrl(
  metadata: OidcMetadata,
  config: AuthConfig,
  returnUrl: string,
  idTokenHint?: string
): string | null {
  if (!metadata.end_session_endpoint) {
    return null;
  }

  const url = new URL(metadata.end_session_endpoint);
  url.searchParams.set("client_id", config.clientId);
  url.searchParams.set("post_logout_redirect_uri", returnUrl);
  url.searchParams.set("returnTo", returnUrl);

  if (idTokenHint) {
    url.searchParams.set("id_token_hint", idTokenHint);
  }

  return url.toString();
}

function toSessionUser(claims: JWTPayload): SessionUser {
  const name = typeof claims.name === "string" ? claims.name : undefined;
  const email = typeof claims.email === "string" ? claims.email : undefined;

  return { name, email, roles: extractRoles(claims) };
}

function extractRoles(claims: JWTPayload): string[] {
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
