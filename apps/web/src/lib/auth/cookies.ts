export const authSessionCookieName = "bb_session";
export const authTransactionCookieName = "bb_auth_tx";

export const authCookieOptions = {
  httpOnly: true,
  sameSite: "lax" as const,
  secure: shouldUseSecureCookies(),
  path: "/"
};

function shouldUseSecureCookies(): boolean {
  const webBaseUrl = process.env.BETTERBOOKING_WEB_BASE_URL?.trim();

  if (webBaseUrl) {
    return webBaseUrl.startsWith("https://");
  }

  return process.env.NODE_ENV === "production";
}
