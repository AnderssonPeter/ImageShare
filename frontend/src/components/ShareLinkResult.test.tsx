import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import ShareLinkResult from "@components/ShareLinkResult";

const TOKEN = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJhZG1pbiJ9.signature";

describe("shareLinkResult renders the generated token", () => {
  it("displays the JWT string when open", () => {
    expect.assertions(1);
    // Arrange + Act
    render(<ShareLinkResult token={TOKEN} open onOpenChange={vi.fn<(open: boolean) => void>()} />);
    // Assert
    expect(screen.getByText(TOKEN)).toBeInTheDocument();
  }, 1000);
});

describe("shareLinkResult close", () => {
  it("fires onOpenChange(false) when the close button is clicked", () => {
    expect.assertions(1);
    // Arrange
    const onOpenChange = vi.fn<(open: boolean) => void>();
    render(<ShareLinkResult token={TOKEN} open onOpenChange={onOpenChange} />);
    // Act
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    // Assert
    expect(onOpenChange.mock.calls[0]?.[0]).toBe(false);
  }, 1000);
});
