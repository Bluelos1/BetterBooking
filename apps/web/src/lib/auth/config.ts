export type AuthConfig = {
  issuer: string;
  clientId: string;
  clientSecret?: string;
  scopes: string;
  cookieSecret: string;
  webBaseUrl?: string;
};

export type AuthConfigResult =
  | { ok: true; config: AuthConfig }
  | { ok: false; error: string };

export function getAuthConfig(): AuthConfigResult {
  const issuer = trim(process.env.BETTERBOOKING_AUTH_ISSUER);
  const clientId = trim(process.env.BETTERBOOKING_AUTH_CLIENT_ID);
  const clientSecret = trim(process.env.BETTERBOOKING_AUTH_CLIENT_SECRET);
  const scopes = trim(process.env.BETTERBOOKING_AUTH_SCOPES) ?? "openid profile email";
  const cookieSecret = trim(process.env.BETTERBOOKING_AUTH_COOKIE_SECRET);
  const webBaseUrl = trim(process.env.BETTERBOOKING_WEB_BASE_URL);

  if (!issuer) {
    return { ok: false, error: "BETTERBOOKING_AUTH_ISSUER is required." };
  }

  if (!clientId) {
    return { ok: false, error: "BETTERBOOKING_AUTH_CLIENT_ID is required." };
  }

  if (!cookieSecret || cookieSecret.length < 32) {
    return { ok: false, error: "BETTERBOOKING_AUTH_COOKIE_SECRET must be at least 32 characters." };
  }

  return {
    ok: true,
    config: {
      issuer: issuer.replace(/\/+$/, ""),
      clientId,
      clientSecret,
      scopes,
      cookieSecret,
      webBaseUrl: webBaseUrl?.replace(/\/+$/, "")
    }
  };
}

function trim(value: string | undefined): string | undefined {
  const trimmed = value?.trim();

  return trimmed ? trimmed : undefined;
}
