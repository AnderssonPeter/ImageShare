import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import ImageDownloadButton from "@components/ImageDownloadButton";
import { type imageUrl } from "@lib/api/urls";

const {
  mockImageUrl,
  mockFetch,
  mockCreateObjectUrl,
  mockRevokeObjectUrl,
  mockToastSuccess,
  mockToastError,
} = vi.hoisted(() => ({
  mockImageUrl: vi.fn<typeof imageUrl>(),
  mockFetch: vi.fn<typeof globalThis.fetch>(),
  mockCreateObjectUrl: vi.fn<(blob: Blob) => string>(),
  mockRevokeObjectUrl: vi.fn<(url: string) => void>(),
  mockToastSuccess: vi.fn<(message: string) => void>(),
  mockToastError: vi.fn<(message: string) => void>(),
}));

vi.mock(import("@lib/api/urls"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, imageUrl: mockImageUrl };
});

vi.mock(import("sonner"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  const toast = actual.toast as Record<string, unknown>;
  return {
    ...actual,
    toast: { ...toast, success: mockToastSuccess, error: mockToastError } as never,
  };
});

interface AnchorClick {
  download: string;
}

function captureAnchorClicks(): AnchorClick[] {
  const clicks: AnchorClick[] = [];
  const originalCreate = document.createElement.bind(document);
  vi.spyOn(document, "createElement").mockImplementation((tagName: string) => {
    const element = originalCreate(tagName);
    if (tagName === "a") {
      const click = element.click.bind(element);
      element.click = () => {
        clicks.push({ download: element.download });
        click();
      };
    }
    return element;
  });
  return clicks;
}

function setupFetch(blob: Blob): void {
  mockFetch.mockReset();
  mockFetch.mockResolvedValue({
    ok: true,
    blob: () => Promise.resolve(blob),
  } as unknown as Response);
}

function setupGlobals(): void {
  mockImageUrl.mockReset();
  mockImageUrl.mockReturnValue("/api/content/image/p%2Fa.jpg");
  vi.stubGlobal("fetch", mockFetch);
  vi.stubGlobal("URL", {
    ...URL,
    createObjectURL: mockCreateObjectUrl,
    revokeObjectURL: mockRevokeObjectUrl,
  });
  mockCreateObjectUrl.mockReset();
  mockCreateObjectUrl.mockReturnValue("blob:object-url");
  mockRevokeObjectUrl.mockReset();
  mockToastSuccess.mockReset();
  mockToastError.mockReset();
}

describe("imageDownloadButton trigger", () => {
  it("renders a download button with an accessible label", () => {
    expect.assertions(1);
    // Arrange
    setupGlobals();

    // Act + Assert
    render(<ImageDownloadButton imagePath="p/a.jpg" imageName="a.jpg" />);
    expect(screen.getByRole("button", { name: "Download image" })).toBeInTheDocument();
  }, 1000);
});

describe("imageDownloadButton format menu", () => {
  it("lists JPG, AVIF, and WEBP options in that order when opened", async () => {
    expect.assertions(4);
    // Arrange
    setupGlobals();

    // Act
    render(<ImageDownloadButton imagePath="p/a.jpg" imageName="a.jpg" />);
    fireEvent.click(screen.getByRole("button", { name: "Download image" }));

    // Assert
    const items = await screen.findAllByRole("menuitem");
    expect(items).toHaveLength(3);
    expect(items[0]).toHaveTextContent(/JPG/u);
    expect(items[1]).toHaveTextContent(/AVIF/u);
    expect(items[2]).toHaveTextContent(/WEBP/u);
  }, 2000);
});

describe("imageDownloadButton download", () => {
  it("fetches with the matching Accept header and triggers a blob download", async () => {
    expect.hasAssertions();
    // Arrange
    setupGlobals();
    const blob = new Blob(["data"], { type: "image/webp" });
    setupFetch(blob);
    const clicks = captureAnchorClicks();

    // Act
    render(<ImageDownloadButton imagePath="p/a.jpg" imageName="a.jpg" />);
    fireEvent.click(screen.getByRole("button", { name: "Download image" }));
    fireEvent.click(await screen.findByRole("menuitem", { name: /WEBP/u }));

    // Assert
    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(1);
    });
    const [[url, init]] = mockFetch.mock.calls;
    expect(url).toBe("/api/content/image/p%2Fa.jpg");
    expect(init?.headers).toMatchObject({ Accept: "image/webp" });
    expect(clicks[0]?.download).toBe("a.webp");
    expect(mockToastSuccess).toHaveBeenCalledWith("Download started");
  }, 2000);

  it("toasts an error when the fetch fails", async () => {
    expect.hasAssertions();
    // Arrange
    setupGlobals();
    mockFetch.mockReset();
    mockFetch.mockResolvedValue({ ok: false, status: 403 } as unknown as Response);

    // Act
    render(<ImageDownloadButton imagePath="p/a.jpg" imageName="a.jpg" />);
    fireEvent.click(screen.getByRole("button", { name: "Download image" }));
    fireEvent.click(await screen.findByRole("menuitem", { name: /AVIF/u }));

    // Assert
    await waitFor(() => {
      expect(mockToastError).toHaveBeenCalledWith("Download failed");
    });
  }, 2000);
});
