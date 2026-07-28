import { cookies } from "next/headers";
import { getAuthConfig } from "./config";
import { authSessionCookieName } from "./cookies";
import { decryptJson } from "./crypto";
import type { SessionUser } from "./oidc";

export type AuthSession = {
  accessToken: string;
  idToken?: string;
  expiresAt: number;
  user: SessionUser;
};

export async function getSession(): Promise<AuthSession | null> {
  const configResult = getAuthConfig();

  if (!configResult.ok) {
    return null;
  }

  const cookieStore = await cookies();
  const cookie = cookieStore.get(authSessionCookieName);

  if (!cookie?.value) {
    return null;
  }

  const session = decryptJson<AuthSession>(cookie.value, configResult.config.cookieSecret);

  if (!session || session.expiresAt <= Date.now()) {
    return null;
  }

  session.user.roles = Array.isArray(session.user.roles) ? session.user.roles : [];

  return session;
}
