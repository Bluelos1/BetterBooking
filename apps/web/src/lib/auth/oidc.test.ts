import { createServer } from "node:http";
import { exportJWK, generateKeyPair, SignJWT } from "jose";
import { describe, expect, it } from "vitest";
import type { AuthConfig } from "./config";
import { decryptJson, encryptJson } from "./crypto";
import {
  buildAuthorizationUrl,
  createAuthTransaction,
  getRedirectUri,
  sanitizeReturnTo,
  verifyIdToken,
  type OidcMetadata
} from "./oidc";

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

  it("builds an Auth0-compatible authorization request", () => {
    const transaction = createAuthTransaction("/me/listings", "https://web.example.test/api/auth/callback");
    const url = new URL(buildAuthorizationUrl(metadata(), config(), transaction, { screenHint: "signup" }));

    expect(url.searchParams.get("audience")).toBe("https://api.example.test");
    expect(url.searchParams.get("nonce")).toBe(transaction.nonce);
    expect(url.searchParams.get("state")).toBe(transaction.state);
    expect(url.searchParams.get("code_challenge_method")).toBe("S256");
    expect(url.searchParams.get("screen_hint")).toBe("signup");
    expect(url.searchParams.has("login_hint")).toBe(false);
  });

  it("verifies ID token signature, issuer, audience, and nonce", async () => {
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = { ...await exportJWK(publicKey), alg: "RS256", kid: "test-key", use: "sig" };
    const server = createServer((_, response) => {
      response.writeHead(200, { "Content-Type": "application/json" });
      response.end(JSON.stringify({ keys: [jwk] }));
    });

    await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));

    try {
      const address = server.address();
      if (!address || typeof address === "string") throw new Error("Test JWKS server did not start.");

      const transaction = createAuthTransaction("/", "https://web.example.test/api/auth/callback");
      const testMetadata = { ...metadata(), jwks_uri: `http://127.0.0.1:${address.port}/jwks` };
      const idToken = await new SignJWT({ nonce: transaction.nonce, name: "Alex", email: "alex@example.test" })
        .setProtectedHeader({ alg: "RS256", kid: "test-key" })
        .setIssuer(testMetadata.issuer)
        .setAudience(config().clientId)
        .setSubject("auth0|alex")
        .setIssuedAt()
        .setExpirationTime("5m")
        .sign(privateKey);

      await expect(verifyIdToken(idToken, testMetadata, config(), transaction)).resolves.toEqual({
        name: "Alex",
        email: "alex@example.test",
        roles: []
      });
      await expect(verifyIdToken(idToken, testMetadata, config(), { ...transaction, nonce: "wrong" })).rejects.toThrow();

      const tokenWithoutSubject = await new SignJWT({ nonce: transaction.nonce })
        .setProtectedHeader({ alg: "RS256", kid: "test-key" })
        .setIssuer(testMetadata.issuer)
        .setAudience(config().clientId)
        .setIssuedAt()
        .setExpirationTime("5m")
        .sign(privateKey);

      await expect(verifyIdToken(tokenWithoutSubject, testMetadata, config(), transaction)).rejects.toThrow(
        "missing required claims"
      );
    } finally {
      server.close();
    }
  });

  it("encrypts and decrypts json payloads", () => {
    const secret = "test-secret-value-with-enough-length";
    const encrypted = encryptJson({ accessToken: "token" }, secret);

    expect(decryptJson<{ accessToken: string }>(encrypted, secret)).toEqual({ accessToken: "token" });
    expect(decryptJson(encrypted, "different-secret-value-with-enough-length")).toBeNull();
  });
});

function config(): AuthConfig {
  return {
    issuer: "https://issuer.example.test",
    audience: "https://api.example.test",
    clientId: "web-client",
    scopes: "openid profile email",
    cookieSecret: "test-secret-value-with-enough-length"
  };
}

function metadata(): OidcMetadata {
  return {
    issuer: "https://issuer.example.test/",
    authorization_endpoint: "https://issuer.example.test/authorize",
    token_endpoint: "https://issuer.example.test/oauth/token",
    jwks_uri: "https://issuer.example.test/.well-known/jwks.json"
  };
}
