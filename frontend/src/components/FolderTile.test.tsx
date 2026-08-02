import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { type FolderEntry } from "@lib/api/generated/imageShare.schemas";
import FolderTile from "@components/FolderTile";
import { type randomFolderUrl } from "@lib/api/urls";

const { mockRandomFolderUrl } = vi.hoisted(() => ({
  mockRandomFolderUrl: vi.fn<typeof randomFolderUrl>(),
}));

vi.mock(import("@lib/api/urls"), async (importOriginal) => {
  const actual = (await importOriginal()) as Record<string, unknown>;
  return { ...actual, randomFolderUrl: mockRandomFolderUrl };
});

function noop(): void {}

function folderEntry(name: string, path: string): FolderEntry {
  return { name, path, type: "Folder" };
}

describe("folderTile cover image", () => {
  it("requests a recursive thumbnail cover from the folder", () => {
    expect.assertions(1);
    // Arrange
    mockRandomFolderUrl.mockReset();
    const entry = folderEntry("Holidays", "photos/holidays");

    // Act
    render(<FolderTile entry={entry} onNavigate={noop} />);

    // Assert
    expect(mockRandomFolderUrl).toHaveBeenCalledWith("photos/holidays", true, true);
  }, 1000);
});

describe("folderTile renders the folder name", () => {
  it("shows the folder name as an overlay", () => {
    expect.assertions(1);
    // Arrange + Act
    render(<FolderTile entry={folderEntry("Holidays", "holidays")} onNavigate={noop} />);

    // Assert
    expect(screen.getByText("Holidays")).toBeInTheDocument();
  }, 1000);
});

describe("folderTile navigate on click", () => {
  it("calls onNavigate with the folder path when the whole tile is clicked", () => {
    expect.assertions(1);
    // Arrange
    const onNavigate = vi.fn<(path: string) => void>();
    render(
      <FolderTile entry={folderEntry("Holidays", "photos/holidays")} onNavigate={onNavigate} />,
    );

    // Act
    fireEvent.click(screen.getByRole("button", { name: "Open folder Holidays" }));

    // Assert
    expect(onNavigate).toHaveBeenCalledWith("photos/holidays");
  }, 1000);
});

describe("folderTile has no download affordance", () => {
  it("does not render a download link on the tile", () => {
    expect.assertions(1);
    // Arrange + Act
    render(<FolderTile entry={folderEntry("Holidays", "holidays")} onNavigate={noop} />);

    // Assert
    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  }, 1000);
});
