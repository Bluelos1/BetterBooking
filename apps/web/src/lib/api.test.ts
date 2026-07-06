import { describe, expect, it, vi } from "vitest";
import { buildApiUrl, normalizeApiBaseUrl } from "./api";

describe("api url helpers", () => {
  it("uses the safe local backend default", () => {
    expect(normalizeApiBaseUrl(undefined)).toBe("http://localhost:5245");
  });

  it("trims trailing slashes", () => {
    expect(normalizeApiBaseUrl("https://api.example.test///")).toBe("https://api.example.test");
  });

  it("builds urls with query parameters", () => {
    vi.stubEnv("BETTERBOOKING_API_BASE_URL", "https://api.example.test/");

    expect(buildApiUrl("/api/v1/listings", { q: "lake cabin", page: 2 })).toBe(
      "https://api.example.test/api/v1/listings?q=lake+cabin&page=2"
    );

    vi.unstubAllEnvs();
  });
});
