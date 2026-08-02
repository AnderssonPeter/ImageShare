import { describe, expect, it } from "vitest";

import { downloadUrl, imageUrl, randomFolderUrl, randomRootUrl } from "@lib/api/urls";

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
