import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { type FolderEntry } from "@lib/api/generated";
import ImageTile from "@components/ImageTile";
import { type imageUrl } from "@lib/api/urls";

const { mockImageUrl } = vi.hoisted(() => ({
  mockImageUrl: vi.fn<typeof imageUrl>(),
}));

vi.mock(import("@lib/api/urls"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, imageUrl: mockImageUrl };
});

function noop(): void {}

function fileEntry(name: string, path: string): FolderEntry {
  return { name, path, type: "File" };
}

describe("imageTile thumbnail", () => {
  it("requests the thumbnail variant of the image", () => {
    expect.assertions(1);
    // Arrange
    mockImageUrl.mockReset();
    const entry = fileEntry("pic.jpg", "photos/pic.jpg");

    // Act
    render(<ImageTile entry={entry} onOpen={noop} />);

    // Assert
    expect(mockImageUrl).toHaveBeenCalledWith("photos/pic.jpg", true);
  }, 1000);
});

describe("imageTile open on click", () => {
  it("calls onOpen with the image path when clicked", () => {
    expect.assertions(1);
    // Arrange
    const onOpen = vi.fn<(path: string) => void>();
    render(<ImageTile entry={fileEntry("pic.jpg", "photos/pic.jpg")} onOpen={onOpen} />);

    // Act
    fireEvent.click(screen.getByRole("button", { name: "Open image pic.jpg" }));

    // Assert
    expect(onOpen).toHaveBeenCalledWith("photos/pic.jpg");
  }, 1000);
});
