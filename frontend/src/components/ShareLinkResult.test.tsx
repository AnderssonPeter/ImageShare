import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import ShareLinkResult from "@components/ShareLinkResult";

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.signature";
const ORIGIN = "https://imageshare.example";
const SHARE_URL = `${ORIGIN}/api/authentication/login/jwt/${TOKEN}`;

const { writeText, mockToastSuccess, mockToastError, mockShare, mockCanShare, svgToPngFile } =
  vi.hoisted(() => ({
    writeText: vi.fn<() => Promise<void>>(),
    mockToastSuccess: vi.fn<(message: string) => void>(),
    mockToastError: vi.fn<(message: string) => void>(),
    mockShare: vi.fn<(data: ShareData) => Promise<void>>(),
    mockCanShare: vi.fn<(data?: ShareData) => boolean>(),
    svgToPngFile: vi.fn<() => Promise<File>>(),
  }));

function stubClipboard(): void {
  Object.defineProperty(globalThis.navigator, "clipboard", {
    value: { writeText },
    configurable: true,
  });
}

function stubWebShare(canShare: boolean): void {
  Object.defineProperty(globalThis.navigator, "canShare", {
    value: mockCanShare,
    configurable: true,
  });
  Object.defineProperty(globalThis.navigator, "share", {
    value: mockShare,
    configurable: true,
  });
  mockCanShare.mockReturnValue(canShare);
}

function stubLocation(): void {
  vi.stubGlobal("location", { origin: ORIGIN } as Location);
}

vi.mock(import("sonner"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  const toast = actual.toast as Record<string, unknown>;
  return {
    ...actual,
    toast: { ...toast, success: mockToastSuccess, error: mockToastError } as never,
  };
});

vi.mock(import("@lib/svgToPng"), () => ({
  svgElementToPngFile: svgToPngFile.mockResolvedValue(
    new File([""], "share-qr.png", { type: "image/png" }),
  ),
}));

function stubResolvedQrFile(): File {
  svgToPngFile.mockReset();
  const file = new File([""], "share-qr.png", { type: "image/png" });
  svgToPngFile.mockResolvedValue(file);
  return file;
}

function renderResult(returnUrl?: string): void {
  render(
    <ShareLinkResult
      token={TOKEN}
      returnUrl={returnUrl}
      open
      onOpenChange={vi.fn<(open: boolean) => void>()}
    />,
  );
}

describe("shareLinkResult renders the QR code", () => {
  it("renders a QR code encoding the share URL", () => {
    expect.assertions(1);
    // Arrange
    stubLocation();
    // Act
    renderResult();
    // Assert — qrcode.react renders an <svg>
    const qr = screen.getByRole("img", { hidden: true });
    expect(qr.tagName).toBe("svg");
  }, 1000);

  it("renders a Copy link action button", () => {
    expect.assertions(1);
    // Arrange
    stubLocation();
    // Act
    renderResult();
    // Assert
    expect(screen.getByRole("button", { name: "Copy link" })).toBeInTheDocument();
  }, 1000);
});

describe("shareLinkResult copy action", () => {
  it("copies the share URL to the clipboard", async () => {
    expect.hasAssertions();
    // Arrange
    stubLocation();
    stubClipboard();
    writeText.mockReset();
    writeText.mockResolvedValue();
    mockToastSuccess.mockReset();
    renderResult();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Copy link" }));
    // Assert
    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith(SHARE_URL);
    });
    expect(mockToastSuccess).toHaveBeenCalledWith("Link copied");
  }, 2000);

  it("shows a transient Copied label after copying", async () => {
    expect.hasAssertions();
    // Arrange
    stubLocation();
    stubClipboard();
    writeText.mockReset();
    writeText.mockResolvedValue();
    renderResult();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Copy link" }));
    // Assert
    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Copied" })).toBeInTheDocument();
    });
  }, 2000);
});

describe("shareLinkResult close", () => {
  it("fires onOpenChange(false) when the close button is clicked", () => {
    expect.assertions(1);
    // Arrange
    stubLocation();
    const onOpenChange = vi.fn<(open: boolean) => void>();
    render(<ShareLinkResult token={TOKEN} open onOpenChange={onOpenChange} />);
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    // Assert
    expect(onOpenChange.mock.calls[0]?.[0]).toBe(false);
  }, 1000);
});

describe("shareLinkResult return URL", () => {
  it("encodes the return URL into the copied share link", async () => {
    expect.hasAssertions();
    // Arrange
    stubLocation();
    stubClipboard();
    writeText.mockReset();
    writeText.mockResolvedValue();
    // Act
    renderResult("/browse/photos?image=cat.jpg");
    fireEvent.click(screen.getByRole("button", { name: "Copy link" }));
    // Assert
    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith(
        `${SHARE_URL}?returnUrl=${encodeURIComponent("/browse/photos?image=cat.jpg")}`,
      );
    });
  }, 2000);
});

describe("shareLinkResult share", () => {
  it("renders a Share action button", () => {
    expect.assertions(1);
    // Arrange
    stubLocation();
    // Act
    renderResult();
    // Assert
    expect(screen.getByRole("button", { name: "Share" })).toBeInTheDocument();
  }, 1000);
});

describe("shareLinkResult share via Web Share API", () => {
  it("shares the QR file and URL via the Web Share API when supported", async () => {
    expect.hasAssertions();
    // Arrange
    stubLocation();
    stubWebShare(true);
    const file = stubResolvedQrFile();
    mockToastSuccess.mockReset();
    renderResult();
    // Act — wait for the QR file to be rasterized (button enables), then share
    const sendButton = screen.getByRole("button", { name: "Share" });
    await waitFor(() => expect(sendButton).not.toBeDisabled());
    fireEvent.click(sendButton);
    await waitFor(() => mockShare.mock.calls.length > 0);
    // Assert
    expect(mockShare).toHaveBeenCalledWith({
      files: [file],
      title: "ImageShare link",
      text: SHARE_URL,
      url: SHARE_URL,
    });
    expect(mockToastSuccess).toHaveBeenCalledWith("Shared");
  }, 2000);
});

describe("shareLinkResult share falls back to mailto", () => {
  it("falls back to a mailto link when the Web Share API is unavailable", async () => {
    expect.hasAssertions();
    // Arrange
    stubLocation();
    stubWebShare(false);
    stubResolvedQrFile();
    renderResult();
    // Act — wait for the QR file to be rasterized (button enables), then share
    const sendButton = screen.getByRole("button", { name: "Share" });
    await waitFor(() => expect(sendButton).not.toBeDisabled());
    fireEvent.click(sendButton);
    await waitFor(() => svgToPngFile.mock.calls.length > 0);
    // Assert
    expect(globalThis.location.href).toBe(
      `mailto:?subject=ImageShare+link&body=${encodeURIComponent(SHARE_URL)}`,
    );
  }, 2000);
});
