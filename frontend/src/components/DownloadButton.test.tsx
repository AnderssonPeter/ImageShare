import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import DownloadButton from "@components/DownloadButton";
import { type downloadUrl } from "@lib/api/urls";

const { mockDownloadUrl, mockHref } = vi.hoisted(() => ({
  mockDownloadUrl: vi.fn<typeof downloadUrl>(),
  mockHref: { value: "" },
}));

vi.mock(import("@lib/api/urls"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, downloadUrl: mockDownloadUrl };
});

function mockLocationHref(): void {
  Object.defineProperty(globalThis, "location", {
    value: {
      ...globalThis.location,
      get href() {
        return mockHref.value;
      },
      set href(value: string) {
        mockHref.value = value;
      },
    },
    configurable: true,
  });
}

describe("downloadButton trigger", () => {
  it("renders a download button with an accessible label", () => {
    expect.assertions(1);
    // Arrange
    mockDownloadUrl.mockReturnValue("/download");

    // Act + Assert
    render(<DownloadButton path="Birds" />);
    expect(screen.getByRole("button", { name: "Download folder" })).toBeInTheDocument();
  }, 1000);
});

describe("downloadButton download", () => {
  it("navigates to the download URL with all formats by default", async () => {
    expect.assertions(2);
    // Arrange
    mockDownloadUrl.mockReset();
    mockDownloadUrl.mockReturnValue("/api/content/download/Birds");
    mockLocationHref();

    // Act — open the dialog then click Download
    render(<DownloadButton path="Birds" />);
    fireEvent.click(screen.getByRole("button", { name: "Download folder" }));
    const downloadButton = await screen.findByRole("button", { name: "Download" });
    fireEvent.click(downloadButton);

    // Assert
    expect(mockDownloadUrl).toHaveBeenCalledWith("Birds", []);
    expect(mockHref.value).toBe("/api/content/download/Birds");
  }, 2000);
});

describe("downloadButton format selection", () => {
  it("passes the selected format to the download URL", async () => {
    expect.assertions(1);
    // Arrange
    mockDownloadUrl.mockReset();
    mockDownloadUrl.mockReturnValue("/api/content/download/Birds?formats=webp");
    mockLocationHref();

    // Act — select WebP then download
    render(<DownloadButton path="Birds" />);
    fireEvent.click(screen.getByRole("button", { name: "Download folder" }));
    fireEvent.click(await screen.findByLabelText("WEBP"));
    fireEvent.click(screen.getByRole("button", { name: "Download" }));

    // Assert
    expect(mockDownloadUrl).toHaveBeenCalledWith("Birds", ["webp"]);
  }, 2000);
});
