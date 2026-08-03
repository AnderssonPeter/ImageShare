import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import ShareLinkResult from "@components/ShareLinkResult";

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.signature";
const ORIGIN = "https://imageshare.example";
const SHARE_URL = `${ORIGIN}/api/authentication/login/jwt/${TOKEN}`;

const { writeText } = vi.hoisted(() => ({ writeText: vi.fn<() => Promise<void>>() }));

function stubClipboard(): void {
  Object.defineProperty(globalThis.navigator, "clipboard", {
    value: { writeText },
    configurable: true,
  });
}

function stubLocation(): void {
  vi.stubGlobal("location", { origin: ORIGIN } as Location);
}

function renderResult(): void {
  render(<ShareLinkResult token={TOKEN} open onOpenChange={vi.fn<(open: boolean) => void>()} />);
}

describe("shareLinkResult renders the generated token and URL", () => {
  it("displays the shareable URL built from the token and origin", () => {
    expect.assertions(1);
    // Arrange
    stubLocation();
    // Act
    renderResult();
    // Assert
    expect(screen.getByText(SHARE_URL)).toBeInTheDocument();
  }, 1000);

  it("displays the raw JWT token", () => {
    expect.assertions(1);
    // Arrange + Act
    stubLocation();
    renderResult();
    // Assert
    expect(screen.getByText(TOKEN)).toBeInTheDocument();
  }, 1000);

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

  it("renders Copy link and Download QR action buttons", () => {
    expect.assertions(2);
    // Arrange
    stubLocation();
    // Act
    renderResult();
    // Assert
    expect(screen.getByRole("button", { name: "Copy link" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download QR" })).toBeInTheDocument();
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
    renderResult();
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Copy link" }));
    // Assert
    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith(SHARE_URL);
    });
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
