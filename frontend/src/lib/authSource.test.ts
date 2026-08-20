import { describe, expect, it } from "vitest";
import { getStoredAuthSource, storeAuthSource } from "@lib/authSource";

/** Clear the `authSource` cookie (jsdom ignores `document.cookie = ""`). */
function clearAuthSourceCookie(): void {
  document.cookie = "authSource=; path=/; max-age=0";
}

describe("storeAuthSource / getStoredAuthSource", () => {
  it("round-trips an AuthenticationSource through the cookie", () => {
    expect.assertions(1);
    clearAuthSourceCookie();
    storeAuthSource("OpenIdConnect");
    expect(getStoredAuthSource()).toBe("OpenIdConnect");
  }, 1000);

  it("overwrites a previously stored source", () => {
    expect.assertions(1);
    clearAuthSourceCookie();
    storeAuthSource("OpenIdConnect");
    storeAuthSource("Jwt");
    expect(getStoredAuthSource()).toBe("Jwt");
  }, 1000);

  it("returns undefined when no cookie is set", () => {
    expect.assertions(1);
    clearAuthSourceCookie();
    expect(getStoredAuthSource()).toBeUndefined();
  }, 1000);

  it("returns undefined for a tampered, invalid value", () => {
    expect.assertions(1);
    clearAuthSourceCookie();
    document.cookie = "authSource=NotARealSource";
    expect(getStoredAuthSource()).toBeUndefined();
  }, 1000);
});
