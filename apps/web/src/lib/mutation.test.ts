import { describe, expect, it } from "vitest";
import { buildMutationRedirectUrl, getFormString } from "./mutation";

describe("mutation helpers", () => {
  it("builds safe same-origin redirect urls", () => {
    const url = buildMutationRedirectUrl("http://localhost:3000/api/listings/create", "/me/listings", {
      status: "listing-created"
    });

    expect(url.toString()).toBe("http://localhost:3000/me/listings?status=listing-created");
  });

  it("rejects absolute return urls", () => {
    const url = buildMutationRedirectUrl("http://localhost:3000/api/listings/create", "https://evil.example", {
      error: "bad-return"
    });

    expect(url.toString()).toBe("http://localhost:3000/?error=bad-return");
  });

  it("reads trimmed form strings", () => {
    const formData = new FormData();
    formData.set("title", "  City apartment  ");

    expect(getFormString(formData, "title")).toBe("City apartment");
  });
});
