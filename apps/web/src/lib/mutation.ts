import { sanitizeReturnTo } from "./auth/oidc";

export function buildMutationRedirectUrl(
  requestUrl: string,
  returnTo: string | undefined,
  query: Record<string, string>
): URL {
  const url = new URL(sanitizeReturnTo(returnTo), requestUrl);

  for (const [key, value] of Object.entries(query)) {
    url.searchParams.set(key, value);
  }

  return url;
}

export function getFormString(formData: FormData, name: string): string | undefined {
  const value = formData.get(name);

  return typeof value === "string" ? value.trim() : undefined;
}
