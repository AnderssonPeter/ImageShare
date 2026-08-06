import {
  buildShareUrl,
  downloadUrl,
  imageUrl,
  randomFolderUrl,
  randomRootUrl,
  trimReturnUrl,
} from "@lib/api/urls";
import { describe, expect, it } from "vitest";

describe("imageUrl builder", () => {
  it("builds a thumbnail URL with the thumbnail param", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(imageUrl("photos/2024/pic", true)).toBe(
      "/api/content/image/photos%2F2024%2Fpic?thumbnail=true",
    );
  }, 1000);

  it("builds a full-res URL without the thumbnail param", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(imageUrl("photos/2024/pic", false)).toBe("/api/content/image/photos%2F2024%2Fpic");
  }, 1000);
});

describe("randomFolderUrl builder", () => {
  it("builds a recursive thumbnail cover URL", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(randomFolderUrl("photos/2024", true, true)).toBe(
      "/api/content/random/photos%2F2024?thumbnail=true&recursive=true",
    );
  }, 1000);

  it("builds a plain random URL with no params", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(randomFolderUrl("public", false, false)).toBe("/api/content/random/public");
  }, 1000);
});

describe("randomRootUrl builder", () => {
  it("builds a full-res root URL with no params", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(randomRootUrl(false)).toBe("/api/content/random");
  }, 1000);

  it("builds a thumbnail root URL with the thumbnail param", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(randomRootUrl(true)).toBe("/api/content/random?thumbnail=true");
  }, 1000);
});

describe("downloadUrl builder", () => {
  it("builds a zip URL with repeated format params", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(downloadUrl("photos/2024", ["avif", "webp"])).toBe(
      "/api/content/download/photos%2F2024?format=avif&format=webp",
    );
  }, 1000);

  it("builds a zip URL with no format params", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(downloadUrl("public", [])).toBe("/api/content/download/public");
  }, 1000);
});

describe("buildShareUrl builder", () => {
  it("builds an absolute sign-in URL from the token and origin", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(buildShareUrl("eyJ.token.sig", "https://imageshare.example")).toBe(
      "https://imageshare.example/api/authentication/login/jwt/eyJ.token.sig",
    );
  }, 1000);

  it("appends the encoded return URL when provided", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(
      buildShareUrl("eyJ.token.sig", "https://imageshare.example", "/browse/photos?image=cat.jpg"),
    ).toBe(
      "https://imageshare.example/api/authentication/login/jwt/eyJ.token.sig?returnUrl=%2Fbrowse%2Fphotos%3Fimage%3Dcat.jpg",
    );
  }, 1000);

  it("omits the return URL query when it is empty", () => {
    expect.assertions(1);
    // Arrange + Act + Assert
    expect(buildShareUrl("tok", "https://host", "")).toBe(
      "https://host/api/authentication/login/jwt/tok",
    );
  }, 1000);
});

describe("return URL normalization", () => {
  it("returns an empty string for blank input", () => {
    expect.assertions(1);
    expect(trimReturnUrl("   ")).toBe("");
  }, 1000);

  it("trims protocol and domain from a full URL", () => {
    expect.assertions(1);
    expect(trimReturnUrl("https://imageshare.example/browse/photos?image=cat.jpg#top")).toBe(
      "/browse/photos?image=cat.jpg#top",
    );
  }, 1000);

  it("trims the host from a protocol-relative URL", () => {
    expect.assertions(1);
    expect(trimReturnUrl("//imageshare.example/browse/x")).toBe("/browse/x");
  }, 1000);

  it("keeps an absolute path as-is", () => {
    expect.assertions(1);
    expect(trimReturnUrl("/browse/photos")).toBe("/browse/photos");
  }, 1000);

  it("adds a leading slash to a bare path", () => {
    expect.assertions(1);
    expect(trimReturnUrl("browse/photos")).toBe("/browse/photos");
  }, 1000);
});
