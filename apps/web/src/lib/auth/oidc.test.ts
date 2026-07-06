import { describe, expect, it } from "vitest";
import { decryptJson, encryptJson } from "./crypto";
import { getRedirectUri, sanitizeReturnTo } from "./oidc";

describe("auth helpers", () => {
  it("sanitizes return paths", () => {
    expect(sanitizeReturnTo("/me/listings")).toBe("/me/listings");
    expect(sanitizeReturnTo("https://evil.example")).toBe("/");
    expect(sanitizeReturnTo("//evil.example/path")).toBe("/");
  });

  it("uses configured redirect origin when present", () => {
    expect(getRedirectUri("http://localhost:3000/current", "https://web.example.test")).toBe(
      "https://web.example.test/api/auth/callback"
    );
  });

  it("encrypts and decrypts json payloads", () => {
    const secret = "test-secret-value-with-enough-length";
    const encrypted = encryptJson({ accessToken: "token" }, secret);

    expect(decryptJson<{ accessToken: string }>(encrypted, secret)).toEqual({ accessToken: "token" });
    expect(decryptJson(encrypted, "different-secret-value-with-enough-length")).toBeNull();
  });
});
